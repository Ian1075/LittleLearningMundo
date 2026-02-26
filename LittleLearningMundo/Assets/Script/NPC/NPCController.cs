using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaIntegration.Models;
using Newtonsoft.Json.Linq;

/// <summary>
/// NPC 核心控制器：確保抵達後的介紹會正確顯示對話，並等待玩家確認。
/// </summary>
public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Talking, Navigating, Thinking }

    [Header("身分設定")]
    public NPCIdentity identity;
    public string npcName = "學長";

    [Header("功能設定")]
    public bool isGuide = true;
    public bool isStoryNPC = false; 
    public bool autoStartGreeting = false;

    [Header("當前狀態")]
    public NPCState currentState = NPCState.Idle;

    [Header("引用")]
    public NPCNavigator navigator;
    public NPCLocationSensor sensor;
    public NPCVisualManager visualManager;
    public NPCMemoryManager memoryManager;
    public ChatUIManager chatUI;
    public OllamaService ollamaService;
    public PlayerController playerController;

    private string _currentStreamText = "";
    private ToolCall _pendingToolCall = null;

    private void Start()
    {
        if (autoStartGreeting) Invoke(nameof(StartTalking), 0.5f);
    }

    public void StartTalking()
    {
        if (identity == null) return;
        bool wasNavigating = (currentState == NPCState.Navigating);
        if (wasNavigating) navigator.StopMoving();
        
        currentState = NPCState.Talking;
        if (playerController != null) playerController.SetState(PlayerController.PlayerState.Talking);

        BuildingZone currentZone = sensor != null ? sensor.GetCurrentZone() : null;

        // 抵達站點觸發
        if (isGuide && wasNavigating && currentZone != null)
        {
            HandleArrivalIntroduction(currentZone);
        }
        else
        {
            chatUI.ShowNPCResponse(identity.npcName, identity.defaultGreeting, EndInteraction, SwitchToInputMode);
        }
    }

    private void SwitchToInputMode() => chatUI.OpenPlayerInput(OnPlayerSubmit);

    private async void HandleArrivalIntroduction(BuildingZone zone)
    {
        bool isMainStory = (isStoryNPC && GameModeManager.Instance != null && GameModeManager.Instance.currentMode == GameModeManager.GameMode.MainStory);
        
        // 1. 立即啟動視覺演出 (切換鏡頭)
        if (isMainStory) StoryManager.Instance?.NotifyArrivalVisuals(zone);

        // 2. 準備 UI
        currentState = NPCState.Thinking;
        _currentStreamText = "";
        _pendingToolCall = null;
        chatUI.PrepareStreamingResponse(identity.npcName);

        StoryData.StoryStep storyStep = isMainStory ? StoryManager.Instance.GetCurrentStep() : null;

        // 邏輯 A：照稿唸 (不使用 AI)
        if (isMainStory && storyStep != null && !storyStep.useAISummary)
        {
            currentState = NPCState.Talking;
            chatUI.ShowNPCResponse(identity.npcName, storyStep.description, EndInteraction, () => {
                // 對話完畢，由玩家按 E 觸發下一站
                if (isMainStory) StoryManager.Instance?.OnStepArrival();
                else SwitchToInputMode();
            });
            return;
        }

        // 邏輯 B：AI 介紹
        string combinedKnowledge = zone.knowledgeBase;
        if (storyStep != null && !string.IsNullOrEmpty(storyStep.description))
            combinedKnowledge += $"\n[主線描述參考：{storyStep.description}]";

        string arrivalPrompt = string.Format(identity.arrivalEventPrompt, zone.displayName, combinedKnowledge);
        var history = memoryManager.PrepareMessages(arrivalPrompt, zone);
        var request = ollamaService.CreateRequest(history, false);

        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
            _currentStreamText += chunk;
            chatUI.UpdateStreamingText(_currentStreamText);
        }, null);

        memoryManager.SaveAssistantResponse(_currentStreamText);
        currentState = NPCState.Talking;
        
        // 關鍵：FinishStreamingResponse 會處理打字機結束後的「按 E 繼續」
        chatUI.FinishStreamingResponse(EndInteraction, () => {
            // 當玩家在對話框按完最後一個 E
            if (isMainStory) StoryManager.Instance?.OnStepArrival();
            else SwitchToInputMode();
        });
    }

    private async void OnPlayerSubmit(string playerInput)
    {
        currentState = NPCState.Thinking;
        _currentStreamText = "";
        _pendingToolCall = null;
        chatUI.PrepareStreamingResponse(identity.npcName);

        BuildingZone currentZone = sensor != null ? sensor.GetCurrentZone() : null;
        var history = memoryManager.PrepareMessages(playerInput, currentZone);
        var request = ollamaService.CreateRequest(history, isGuide);

        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
            _currentStreamText += chunk;
            chatUI.UpdateStreamingText(_currentStreamText);
        }, (tc) => { if (isGuide) _pendingToolCall = tc; });

        memoryManager.SaveAssistantResponse(_currentStreamText);
        currentState = NPCState.Talking;

        if (isGuide && _pendingToolCall != null) {
            string locId = ExtractId(_pendingToolCall.function.arguments);
            if (!string.IsNullOrEmpty(locId)) { HandleNavigation(locId); return; }
        }
        chatUI.FinishStreamingResponse(EndInteraction, SwitchToInputMode);
    }

    private void HandleNavigation(string locId)
    {
        string destName = (locId == "CSIE") ? "資工系館" : (locId == "OPHY" ? "物理系舊館" : locId);
        string reply = string.Format(identity.arrivalReplyTemplate, destName);
        chatUI.ShowNPCResponse(identity.npcName, reply, EndInteraction, () => ExecuteNavigation(locId));
    }

    private string ExtractId(JToken args)
    {
        if (args == null) return null;
        if (args is JObject obj && obj.TryGetValue("location_id", out JToken val)) return val.ToString();
        return args.ToString().Trim('\"');
    }

    public void ExecuteNavigation(string destinationID)
    {
        if (!isGuide) return;
        chatUI.CloseChat(); 
        WaypointNode targetNode = FindObjectsOfType<WaypointNode>().FirstOrDefault(n => n.locationID == destinationID);
        if (targetNode != null)
        {
            currentState = NPCState.Navigating;
            if (playerController != null) playerController.SetState(PlayerController.PlayerState.Idle);
            var path = WaypointPathfinder.FindPath(navigator.GetNearestNode(), targetNode);
            navigator.StartPathNavigation(path, StartTalking);
        }
        else StartTalking();
    }

    public void EndInteraction()
    {
        chatUI.CloseChat();
        currentState = NPCState.Idle;
        if (playerController != null) 
            playerController.SetState(PlayerController.PlayerState.Idle); 
    }

    public void SetHighlight(bool highlight) { if (visualManager != null) visualManager.SetHighlight(highlight); }
}
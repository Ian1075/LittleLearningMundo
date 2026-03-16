using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaIntegration.Models;
using Newtonsoft.Json.Linq;

/// <summary>
/// NPC 核心控制器：將多張圖片的特定描述整合進 AI 請求中。
/// 修復：修正了 Start 方法的語法錯誤以及遊戲模式判定的類型衝突。
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
        if (currentState == NPCState.Navigating) navigator.StopMoving();
        
        currentState = NPCState.Talking;
        if (playerController != null) playerController.SetState(PlayerController.PlayerState.Talking);

        BuildingZone currentZone = sensor != null ? sensor.GetCurrentZone() : null;

        if (isGuide && currentState != NPCState.Navigating && currentZone != null)
            HandleArrivalIntroduction(currentZone);
        else
            chatUI.ShowNPCResponse(identity.npcName, identity.defaultGreeting, EndInteraction, SwitchToInputMode);
    }

    private void SwitchToInputMode() => chatUI.OpenPlayerInput(OnPlayerSubmit);

    private async void HandleArrivalIntroduction(BuildingZone zone)
    {
        // 修正判定：檢查模式是否非 FreeMode
        bool isMainStory = isStoryNPC && GameModeManager.Instance != null && GameModeManager.Instance.currentMode != GameModeManager.GameMode.FreeMode;
        StoryData.StoryStep storyStep = isMainStory ? StoryManager.Instance.GetCurrentStep() : null;

        // 啟動視覺演出
        if (isMainStory) StoryManager.Instance?.NotifyArrivalVisuals(zone);

        // UI 準備
        currentState = NPCState.Thinking;
        _currentStreamText = ""; _pendingToolCall = null;
        chatUI.PrepareStreamingResponse(identity.npcName);

        // 整合知識庫
        string combinedKnowledge = zone.knowledgeBase;
        
        if (storyStep != null)
        {
            if (!string.IsNullOrEmpty(storyStep.description))
                combinedKnowledge += $"\n[此站劇本描述：{storyStep.description}]";
            
            // 重要：遍歷所有圖片描述並加入 Prompt
            if (storyStep.projections != null && storyStep.projections.Count > 0)
            {
                combinedKnowledge += "\n[現場展示的照片資訊如下，請在介紹時適時提及照片內容]：";
                for (int i = 0; i < storyStep.projections.Count; i++)
                {
                    var p = storyStep.projections[i];
                    if (p != null && !string.IsNullOrEmpty(p.imageDescription))
                    {
                        combinedKnowledge += $"\n照片 {i + 1} 描述：{p.imageDescription}";
                    }
                }
            }
        }

        // 非 AI 模式直接顯示
        if (isMainStory && storyStep != null && !storyStep.useAISummary)
        {
            chatUI.ShowNPCResponse(identity.npcName, storyStep.description, EndInteraction, () => StoryManager.Instance?.OnStepArrival());
            return;
        }

        string arrivalPrompt = string.Format(identity.arrivalEventPrompt, zone.displayName, combinedKnowledge);
        var history = memoryManager.PrepareMessages(arrivalPrompt, zone);
        var request = ollamaService.CreateRequest(history, false);

        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
            _currentStreamText += chunk;
            chatUI.UpdateStreamingText(_currentStreamText);
        }, null);

        memoryManager.SaveAssistantResponse(_currentStreamText);
        currentState = NPCState.Talking;
        
        chatUI.FinishStreamingResponse(EndInteraction, () => {
            if (isMainStory) StoryManager.Instance?.OnStepArrival();
            else SwitchToInputMode();
        });
    }

    private async void OnPlayerSubmit(string playerInput)
    {
        currentState = NPCState.Thinking; _currentStreamText = ""; _pendingToolCall = null;
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
            navigator.StartPathNavigation(WaypointPathfinder.FindPath(navigator.GetNearestNode(), targetNode), StartTalking);
        }
        else StartTalking();
    }

    public void EndInteraction()
    {
        chatUI.CloseChat(); currentState = NPCState.Idle;
        if (playerController != null) playerController.SetState(PlayerController.PlayerState.Idle); 
    }

    public void SetHighlight(bool highlight) { if (visualManager != null) visualManager.SetHighlight(highlight); }
}
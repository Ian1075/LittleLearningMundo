using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaIntegration.Models;
using Newtonsoft.Json.Linq;

/// <summary>
/// NPC 核心控制器：負責對話、導航與主線回報。
/// </summary>
public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Talking, Navigating, Thinking }

    [Header("身分與資源")]
    public NPCIdentity identity;
    public string npcName = "NPC";

    [Header("功能設定")]
    public bool isGuide = true;
    public bool isStoryNPC = false; // 是否為主線導覽員
    public bool autoStartGreeting = false;

    [Header("當前狀態")]
    public NPCState currentState = NPCState.Idle;

    [Header("引用組件")]
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

    /// <summary>
    /// 對話啟動入口
    /// </summary>
    public void StartTalking()
    {
        if (identity == null) { Debug.LogError("未指派 NPCIdentity！"); return; }

        bool wasNavigating = (currentState == NPCState.Navigating);
        if (wasNavigating) navigator.StopMoving();
        
        currentState = NPCState.Talking;
        if (playerController != null) playerController.SetState(PlayerController.PlayerState.Talking);

        BuildingZone currentZone = sensor != null ? sensor.GetCurrentZone() : null;

        // 判定：導航結束且人在區域內 -> 觸發介紹
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

    /// <summary>
    /// 到達目的地後的自動介紹
    /// </summary>
    private async void HandleArrivalIntroduction(BuildingZone zone)
    {
        currentState = NPCState.Thinking;
        _currentStreamText = "";
        _pendingToolCall = null;
        chatUI.PrepareStreamingResponse(identity.npcName);

        string arrivalPrompt = string.Format(identity.arrivalEventPrompt, zone.displayName, zone.knowledgeBase);
        var history = memoryManager.PrepareMessages(arrivalPrompt, zone);
        var request = ollamaService.CreateRequest(history, false);

        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
            _currentStreamText += chunk;
            chatUI.UpdateStreamingText(_currentStreamText);
        }, null);

        memoryManager.SaveAssistantResponse(_currentStreamText);
        currentState = NPCState.Talking;
        
        chatUI.FinishStreamingResponse(EndInteraction, () => {
            // 如果目前是在「主線模式」，抵達介紹完畢後要通知 StoryManager
            if (isStoryNPC && GameModeManager.Instance != null && GameModeManager.Instance.currentMode == GameModeManager.GameMode.MainStory)
            {
                StoryManager.Instance?.OnStepArrival();
            }
            else
            {
                SwitchToInputMode();
            }
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

        await ollamaService.apiClient.SendChatStreamAsync(request, 
            (chunk) => {
                _currentStreamText += chunk;
                chatUI.UpdateStreamingText(_currentStreamText);
            }, 
            (toolCall) => {
                if (isGuide) _pendingToolCall = toolCall;
            }
        );

        memoryManager.SaveAssistantResponse(_currentStreamText);
        currentState = NPCState.Talking;

        if (isGuide && _pendingToolCall != null && _pendingToolCall.function.name == "navigate_to_location")
        {
            string locId = ExtractId(_pendingToolCall.function.arguments);
            if (!string.IsNullOrEmpty(locId))
            {
                HandleNavigation(locId);
                return;
            }
        }
        
        chatUI.FinishStreamingResponse(EndInteraction, SwitchToInputMode);
    }

    private void HandleNavigation(string locId)
    {
        string destinationName = (locId == "CSIE") ? "資工系館" : (locId == "OPHY" ? "物理系舊館" : locId);
        string reply = string.Format(identity.arrivalReplyTemplate, destinationName);
        chatUI.ShowNPCResponse(identity.npcName, reply, EndInteraction, () => ExecuteNavigation(locId));
    }

    private string ExtractId(JToken arguments)
    {
        if (arguments == null) return null;
        if (arguments is JObject obj && obj.TryGetValue("location_id", out JToken val)) return val.ToString();
        return arguments.ToString().Trim('\"');
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
        if (playerController != null) playerController.SetState(PlayerController.PlayerState.Idle);
    }

    public void SetHighlight(bool highlight) { if (visualManager != null) visualManager.SetHighlight(highlight); }
}
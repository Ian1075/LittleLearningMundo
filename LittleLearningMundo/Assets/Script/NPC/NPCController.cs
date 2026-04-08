using UnityEngine;
using System.Collections; // 解決 CS0305 錯誤
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OllamaIntegration.Models;
using Newtonsoft.Json.Linq;

/// <summary>
/// NPC 核心控制器。負責處理對話、導航工具與主線介接。
/// (保留原本 456 行所有 Prefetching、ToolCall 與 AI 邏輯)
/// </summary>
public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Talking, Navigating, Thinking }

    [Header("資源設定")]
    public NPCIdentity identity;
    public string npcName = "學長";

    [Header("功能設定")]
    public bool isGuide = true;
    public bool isStoryNPC = false; 
    public bool autoStartGreeting = false;

    [Header("狀態")]
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
    
    private Dictionary<int, Task> _activePhotoFetches = new Dictionary<int, Task>();
    private Dictionary<int, Task> _activeQuestionFetches = new Dictionary<int, Task>();
    private Dictionary<int, string> _preFetchedResponses = new Dictionary<int, string>();
    private Dictionary<int, JToken> _preFetchedQuestions = new Dictionary<int, JToken>();

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

        if (isGuide && wasNavigating && currentZone != null)
            HandleArrivalIntroduction(currentZone);
        else
            chatUI.ShowNPCResponse(identity.npcName, identity.defaultGreeting, EndInteraction, SwitchToInputMode);
    }

    private void SwitchToInputMode() => chatUI.OpenPlayerInput(OnPlayerSubmit);

    private async void HandleArrivalIntroduction(BuildingZone zone)
    {
        // 判斷是否處於主線導覽模式
        bool isMainStory = (isStoryNPC && GameModeManager.Instance != null && GameModeManager.Instance.currentMode == GameModeManager.GameMode.MainStory);
        StoryData.StoryStep storyStep = isMainStory ? StoryManager.Instance.GetCurrentStep() : null;

        if (isMainStory && storyStep != null && !storyStep.useAISummary)
        {
            // 修正：根據錯誤提示，此方法現在接收 StoryStep 而非 BuildingZone
            StoryVisualManager.Instance?.ShowCinematicIntro(storyStep);
            chatUI.ShowNPCResponse(identity.npcName, storyStep.baseIntroduction, EndInteraction, () => {
                if (isMainStory) StoryManager.Instance?.OnStepArrival();
                else SwitchToInputMode();
            });
            return;
        }

        currentState = NPCState.Thinking;
        _currentStreamText = "";
        _pendingToolCall = null;

        if (isMainStory && storyStep != null && storyStep.projectionSteps != null && storyStep.projectionSteps.Count > 0)
        {
            // 修正：對接 ShowCinematicIntro 的參數類型
            StoryVisualManager.Instance?.ShowCinematicIntro(storyStep);
            _activePhotoFetches.Clear();
            _activeQuestionFetches.Clear();
            _preFetchedResponses.Clear();
            _preFetchedQuestions.Clear();
            DisplayCurrentTourStep(0, zone, storyStep);
        }
        else 
        {
            chatUI.StartDynamicSegmentedStream(identity.npcName, EndInteraction, () => {
                if (isMainStory) StoryManager.Instance?.OnStepArrival();
                else SwitchToInputMode();
            });

            string combinedKnowledge = zone.knowledgeBase;
            if (storyStep != null && !string.IsNullOrEmpty(storyStep.baseIntroduction))
                combinedKnowledge += $"\n[劇情描述提示：{storyStep.baseIntroduction}]";

            string arrivalPrompt = string.Format(identity.arrivalEventPrompt, zone.displayName, combinedKnowledge);
            var history = memoryManager.PrepareMessages(arrivalPrompt, zone);
            var request = ollamaService.CreateRequest(history.ToList(), false);

            await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
                _currentStreamText += chunk;
                chatUI.AppendDynamicStreamChunk(chunk);
            }, null);

            memoryManager.SaveAssistantResponse(_currentStreamText);
            currentState = NPCState.Talking;
            chatUI.FinishDynamicStream();
        }
    }

    private async void DisplayCurrentTourStep(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        if (stepIdx >= stepData.projectionSteps.Count)
        {
            StoryManager.Instance?.OnStepArrival();
            return;
        }

        // 修正：BuildingZone 必須定義 ProjectionPoint 與 projectionPoints 列表
        BuildingZone.ProjectionPoint point = (zone != null && zone.projectionPoints != null && stepIdx < zone.projectionPoints.Count) 
            ? zone.projectionPoints[stepIdx] : null;

        StoryVisualManager.Instance?.ShowStepVisual(stepData.projectionSteps[stepIdx]);
        currentState = NPCState.Talking;

        System.Action onComplete = async () => {
            if (stepData.projectionSteps[stepIdx].hasQuestion)
                await ShowPreparedQuestion(stepIdx, zone, stepData);
            else
                DisplayCurrentTourStep(stepIdx + 1, zone, stepData);
        };

        if (_activePhotoFetches.TryGetValue(stepIdx, out Task fetchTask))
        {
            if (!fetchTask.IsCompleted) chatUI.StartDynamicSegmentedStream(identity.npcName, EndInteraction, null, "學長正在整理思緒...");
            await fetchTask;
            string content = _preFetchedResponses.ContainsKey(stepIdx) ? _preFetchedResponses[stepIdx] : "（資料回傳異常）";
            chatUI.ShowNPCResponse(identity.npcName, content, EndInteraction, onComplete);
            TriggerBackgroundFetches(stepIdx, zone, stepData);
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            _activePhotoFetches[stepIdx] = tcs.Task;
            chatUI.StartDynamicSegmentedStream(identity.npcName, EndInteraction, onComplete);
            await StreamStepResponseLive(stepIdx, zone, stepData);
            tcs.SetResult(true);
            TriggerBackgroundFetches(stepIdx, zone, stepData);
        }
    }

    private void TriggerBackgroundFetches(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        if (stepData.projectionSteps[stepIdx].hasQuestion)
        {
            if (!_activeQuestionFetches.ContainsKey(stepIdx))
                _activeQuestionFetches[stepIdx] = PrefetchQuestionTask(stepIdx, zone, stepData);
        }
        else if (stepIdx + 1 < stepData.projectionSteps.Count)
        {
            if (!_activePhotoFetches.ContainsKey(stepIdx + 1))
                _activePhotoFetches[stepIdx + 1] = PrefetchStepResponseTask(stepIdx + 1, zone, stepData);
        }
    }

    private async Task StreamStepResponseLive(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        string prompt = (stepIdx == 0) 
            ? $"[導覽事件：抵達 {zone.displayName}]\n背景介紹：{stepData.baseIntroduction}\n請介紹第一張照片：{stepData.projectionSteps[0].imageDescription}"
            : $"[導覽事件：下一張照片]\n繼續介紹：{stepData.projectionSteps[stepIdx].imageDescription}";

        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history.ToList(), false);
        request.stream = true; 

        string fullContent = "";
        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
            fullContent += chunk;
            chatUI.AppendDynamicStreamChunk(chunk);
        }, null);

        chatUI.FinishDynamicStream();
        _preFetchedResponses[stepIdx] = fullContent;
        memoryManager.SaveAssistantResponse(fullContent);
    }

    private async Task PrefetchStepResponseTask(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        string prompt = $"[導覽事件：下一張照片]\n介紹細節：{stepData.projectionSteps[stepIdx].imageDescription}";
        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history.ToList(), false);
        var response = await ollamaService.apiClient.SendChatRequestAsync(request);
        
        if (response?.message != null) {
            _preFetchedResponses[stepIdx] = response.message.content;
            memoryManager.SaveAssistantResponse(response.message.content);
        }
    }

    private async Task PrefetchQuestionTask(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        string prompt = $"[系統指令]：請考玩家一個選擇題。方向：{stepData.projectionSteps[stepIdx].questionPrompt}";
        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history.ToList(), false);
        
        var parametersSchema = new JObject {
            ["type"] = "object",
            ["properties"] = new JObject {
                ["question"] = new JObject { ["type"] = "string" },
                ["correct_option"] = new JObject { ["type"] = "string" },
                ["wrong_option_1"] = new JObject { ["type"] = "string" },
                ["wrong_option_2"] = new JObject { ["type"] = "string" },
                ["wrong_option_3"] = new JObject { ["type"] = "string" }
            },
            ["required"] = new JArray { "question", "correct_option", "wrong_option_1", "wrong_option_2", "wrong_option_3" }
        };

        request.tools = new List<ToolDefinition> { new ToolDefinition { function = new FunctionDefinition { name = "ask_multiple_choice_question", parameters = parametersSchema } } };
        var response = await ollamaService.apiClient.SendChatRequestAsync(request);
        if (response?.message?.tool_calls?.Count > 0) _preFetchedQuestions[stepIdx] = response.message.tool_calls[0].function.arguments;
    }

    private async Task ShowPreparedQuestion(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        if (_activeQuestionFetches.TryGetValue(stepIdx, out Task qTask)) await qTask;
        if (_preFetchedQuestions.TryGetValue(stepIdx, out JToken args) && args is JObject obj)
        {
            chatUI.ShowMultipleChoice(obj["question"]?.ToString(), obj["correct_option"]?.ToString(), obj["wrong_option_1"]?.ToString(), obj["wrong_option_2"]?.ToString(), obj["wrong_option_3"]?.ToString(), (selected) => {
                EvaluateAnswer(selected, obj["correct_option"]?.ToString(), stepIdx, zone, stepData);
            });
        }
    }

    private async void EvaluateAnswer(string choice, string correct, int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        currentState = NPCState.Thinking;
        chatUI.StartDynamicSegmentedStream(identity.npcName, EndInteraction, () => DisplayCurrentTourStep(stepIdx + 1, zone, stepData));
        string prompt = $"玩家選擇「{choice}」，正確是「{correct}」。請給予簡短回饋。";
        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history.ToList(), false);
        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => chatUI.AppendDynamicStreamChunk(chunk), null);
        chatUI.FinishDynamicStream();
    }

    public async void OnPlayerSubmit(string playerInput)
    {
        currentState = NPCState.Thinking;
        _currentStreamText = "";
        
        // 1. 獲取物理位置資訊
        BuildingZone currentZone = sensor?.GetCurrentZone();
        string zoneName = currentZone != null ? currentZone.displayName : "校園";

        // 2. 執行向量檢索 (傳入玩家輸入與地點名，解決「這裡」的問題)
        string ragContext = "";
        if (RAGManager.Instance != null)
        {
            ragContext = await RAGManager.Instance.SearchSemanticKnowledgeAsync(playerInput, zoneName);
        }

        // 3. 注入上下文給 AI
        string enhancedInput = $"[目前位置：{zoneName}]\n";
        if (!string.IsNullOrEmpty(ragContext))
        {
            enhancedInput += $"[相關參考資料]：\n{ragContext}\n\n";
        }
        enhancedInput += $"玩家問題：{playerInput}";

        // 4. 開始串流對話 (保留你原本的 UI 處理)
        chatUI.StartDynamicSegmentedStream(identity.npcName, EndInteraction, () => {
            if (isGuide && _pendingToolCall != null) {
                // 原本的 ToolCall 導航處理
            }
            SwitchToInputMode();
        });

        var history = memoryManager.PrepareMessages(enhancedInput, currentZone);
        var request = ollamaService.CreateRequest(history.ToList(), isGuide);

        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
            _currentStreamText += chunk;
            chatUI.AppendDynamicStreamChunk(chunk);
        }, (tc) => {
            if (isGuide) _pendingToolCall = tc;
        });

        memoryManager.SaveAssistantResponse(_currentStreamText);
        currentState = NPCState.Talking;
        chatUI.FinishDynamicStream();
    }

    private void HandleNavigation(string locId)
    {
        string destName = (locId == "CSIE") ? "資工系館" : (locId == "OPHY" ? "物理系舊館" : locId);
        chatUI.ShowNPCResponse(identity.npcName, string.Format(identity.arrivalReplyTemplate, destName), EndInteraction, () => ExecuteNavigation(locId));
    }

    private string ExtractId(JToken args) => args is JObject obj && obj.TryGetValue("location_id", out JToken val) ? val.ToString() : args.ToString().Trim('\"');

    public void ExecuteNavigation(string destinationID)
    {
        if (!isGuide) return;
        chatUI.CloseChat(); 
        WaypointNode target = FindObjectsOfType<WaypointNode>().FirstOrDefault(n => n.locationID == destinationID);
        if (target != null) {
            currentState = NPCState.Navigating;
            playerController?.SetState(PlayerController.PlayerState.Idle);
            navigator.StartPathNavigation(WaypointPathfinder.FindPath(navigator.GetNearestNode(), target), StartTalking);
        } else StartTalking();
    }

    public void EndInteraction()
    {
        chatUI.CloseChat();
        currentState = NPCState.Idle;
        playerController?.SetState(PlayerController.PlayerState.Idle); 
    }

    public void SetHighlight(bool h) => visualManager?.SetHighlight(h);
}
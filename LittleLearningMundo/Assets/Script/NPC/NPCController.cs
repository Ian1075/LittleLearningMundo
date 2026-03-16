using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaIntegration.Models;
using Newtonsoft.Json.Linq;

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
    
    // ==========================================
    // =     非同步任務追蹤與資料緩存隊列       =
    // ==========================================
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
        bool isMainStory = (isStoryNPC && GameModeManager.Instance != null && GameModeManager.Instance.currentMode == GameModeManager.GameMode.MainStory);
        StoryData.StoryStep storyStep = isMainStory ? StoryManager.Instance.GetCurrentStep() : null;

        if (isMainStory && storyStep != null && !storyStep.useAISummary)
        {
            StoryVisualManager.Instance?.ShowCinematicIntro(zone);
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
            StoryVisualManager.Instance?.ShowCinematicIntro(zone);
            
            // 清理上一站的歷史任務與緩存
            _activePhotoFetches.Clear();
            _activeQuestionFetches.Clear();
            _preFetchedResponses.Clear();
            _preFetchedQuestions.Clear();
            
            DisplayCurrentTourStep(0, zone, storyStep);
        }
        else 
        {
            chatUI.PrepareStreamingResponse(identity.npcName);
            string combinedKnowledge = zone.knowledgeBase;
            if (storyStep != null && !string.IsNullOrEmpty(storyStep.baseIntroduction))
                combinedKnowledge += $"\n[劇情描述提示：{storyStep.baseIntroduction}]";

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
    }

    private async void DisplayCurrentTourStep(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        if (stepIdx >= stepData.projectionSteps.Count)
        {
            StoryManager.Instance?.OnStepArrival();
            return;
        }

        StoryVisualManager.Instance?.ShowStepVisual(stepData.projectionSteps[stepIdx]);
        currentState = NPCState.Talking;

        System.Action onComplete = async () => {
            if (stepData.projectionSteps[stepIdx].hasQuestion)
            {
                await ShowPreparedQuestion(stepIdx, zone, stepData);
            }
            else
            {
                DisplayCurrentTourStep(stepIdx + 1, zone, stepData);
            }
        };

        if (_activePhotoFetches.TryGetValue(stepIdx, out Task fetchTask))
        {
            if (!fetchTask.IsCompleted)
            {
                chatUI.PrepareStreamingResponse(identity.npcName);
                chatUI.UpdateStreamingText("學長正在整理思緒...");
            }
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
            {
                _activeQuestionFetches[stepIdx] = PrefetchQuestionTask(stepIdx, zone, stepData);
            }
        }
        else if (stepIdx + 1 < stepData.projectionSteps.Count)
        {
            if (!_activePhotoFetches.ContainsKey(stepIdx + 1))
            {
                _activePhotoFetches[stepIdx + 1] = PrefetchStepResponseTask(stepIdx + 1, zone, stepData);
            }
        }
    }
    
    private async Task StreamStepResponseLive(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        string prompt = "";
        if (stepIdx == 0) {
            prompt = $"[導覽事件：抵達 {zone.displayName}]\n背景介紹：{stepData.baseIntroduction}\n請進行開場，並生動地介紹第一張照片：{stepData.projectionSteps[0].imageDescription}\n【強制規定】：你現在正在進行定點解說，請專注於介紹照片，絕對不要使用尋路工具，也不要說「跟我來」或提議前往其他地點。";
        } else {
            prompt = $"[導覽事件：下一張照片]\n請順著剛才的對話，繼續為玩家介紹這張照片的細節：{stepData.projectionSteps[stepIdx].imageDescription}\n【強制規定】：你現在正在進行定點解說，請專注於介紹照片，絕對不要使用尋路工具，也不要說「跟我來」或提議前往其他地點。";
        }

        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history, false);
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
        string prompt = $"[導覽事件：下一張照片]\n請順著剛才的對話，繼續為玩家介紹這張照片的細節：{stepData.projectionSteps[stepIdx].imageDescription}\n【強制規定】：你現在正在進行定點解說，請專注於介紹照片，絕對不要使用尋路工具，也不要說「跟我來」或提議前往其他地點。";

        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history, false);
        request.stream = false; 

        var response = await ollamaService.apiClient.SendChatRequestAsync(request);
        
        if (response != null && response.message != null && !string.IsNullOrEmpty(response.message.content))
        {
            string cleanText = response.message.content.Replace("\n", "").Replace("\r", "").Replace("*", "");
            _preFetchedResponses[stepIdx] = cleanText;
            memoryManager.SaveAssistantResponse(cleanText);
        }
        else
        {
            _preFetchedResponses[stepIdx] = "（學長看著照片，陷入了沉思...）";
        }
    }

    // ==========================================
    // =           動態問答模組 (Q&A)             =
    // ==========================================

    private async Task PrefetchQuestionTask(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        string prompt = $"[系統強制指令]：請根據剛才介紹的照片內容，考玩家一個選擇題。\n出題方向提示：{stepData.projectionSteps[stepIdx].questionPrompt}\n【非常重要】：請務必呼叫 `ask_multiple_choice_question` 工具來出題，絕對不要輸出任何其他對話文字！";
        
        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history, false);
        request.stream = false; 

        if (request.tools == null) request.tools = new List<ToolDefinition>();
        
        ToolDefinition qnaTool = new ToolDefinition()
        {
            type = "function",
            function = new FunctionDefinition()
            {
                name = "ask_multiple_choice_question",
                description = "向玩家提出四選一的選擇題",
                parameters = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["question"] = new JObject { ["type"] = "string", ["description"] = "問題內容" },
                        ["correct_option"] = new JObject { ["type"] = "string", ["description"] = "正確選項" },
                        ["wrong_option_1"] = new JObject { ["type"] = "string", ["description"] = "錯誤選項1" },
                        ["wrong_option_2"] = new JObject { ["type"] = "string", ["description"] = "錯誤選項2" },
                        ["wrong_option_3"] = new JObject { ["type"] = "string", ["description"] = "錯誤選項3" }
                    },
                    ["required"] = new JArray { "question", "correct_option", "wrong_option_1", "wrong_option_2", "wrong_option_3" }
                }
            }
        };
        request.tools.Add(qnaTool);

        var response = await ollamaService.apiClient.SendChatRequestAsync(request);

        if (response != null && response.message != null && response.message.tool_calls != null && response.message.tool_calls.Count > 0)
        {
            var toolCall = response.message.tool_calls[0];
            if (toolCall.function.name == "ask_multiple_choice_question")
            {
                _preFetchedQuestions[stepIdx] = toolCall.function.arguments;
            }
            else _preFetchedQuestions[stepIdx] = null;
        }
        else _preFetchedQuestions[stepIdx] = null;
    }

    private async Task ShowPreparedQuestion(int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        if (_activeQuestionFetches.TryGetValue(stepIdx, out Task qTask))
        {
            if (!qTask.IsCompleted)
            {
                currentState = NPCState.Thinking;
                chatUI.PrepareStreamingResponse(identity.npcName);
                chatUI.UpdateStreamingText("學長正在準備考題...");
            }
            await qTask;
        }

        if (_preFetchedQuestions.TryGetValue(stepIdx, out JToken args) && args != null)
        {
            HandleQnAToolCall(args, stepIdx, zone, stepData);
        }
        else
        {
            Debug.LogWarning("[NPCController] 預載考題失敗，跳過 Q&A。");
            DisplayCurrentTourStep(stepIdx + 1, zone, stepData);
        }
    }

    private void HandleQnAToolCall(JToken args, int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        if (args is JObject obj)
        {
            string question = obj["question"]?.ToString();
            string correct = obj["correct_option"]?.ToString();
            string w1 = obj["wrong_option_1"]?.ToString();
            string w2 = obj["wrong_option_2"]?.ToString();
            string w3 = obj["wrong_option_3"]?.ToString();

            chatUI.ShowMultipleChoice(question, correct, w1, w2, w3, (selectedOption) => {
                EvaluateAnswer(selectedOption, correct, stepIdx, zone, stepData);
            });
        }
        else
        {
            DisplayCurrentTourStep(stepIdx + 1, zone, stepData);
        }
    }

    private async void EvaluateAnswer(string playerChoice, string correctOption, int stepIdx, BuildingZone zone, StoryData.StoryStep stepData)
    {
        currentState = NPCState.Thinking;
        _currentStreamText = "";
        
        // 使用 3 個參數相容舊版，並透過外部手動覆寫思考中的文字
        chatUI.StartDynamicSegmentedStream(identity.npcName, EndInteraction, () => {
            DisplayCurrentTourStep(stepIdx + 1, zone, stepData);
        });
        
        if (chatUI.responseTMP != null)
        {
            chatUI.responseTMP.text = "學長正在判定你的答案...";
        }

        string prompt = $"[系統指令]：剛剛的問題正確答案是「{correctOption}」。而玩家選擇了「{playerChoice}」。\n請扮演學長，給予玩家簡短、生動的回饋（30字以內），告訴他答對或答錯，並簡單解釋原因，然後結束這個話題。\n【強制規定】：只需針對題目給予評價，絕對不要說「好喔，跟我來吧」或提議前往任何地點。";

        var history = memoryManager.PrepareMessages(prompt, zone);
        var request = ollamaService.CreateRequest(history, false);
        request.stream = true; 
        
        string fullContent = "";
        await ollamaService.apiClient.SendChatStreamAsync(request, (chunk) => {
            fullContent += chunk;
            chatUI.AppendDynamicStreamChunk(chunk);
        }, null);

        memoryManager.SaveAssistantResponse(fullContent);
        currentState = NPCState.Talking;

        if (stepIdx + 1 < stepData.projectionSteps.Count && !_activePhotoFetches.ContainsKey(stepIdx + 1))
        {
            _activePhotoFetches[stepIdx + 1] = PrefetchStepResponseTask(stepIdx + 1, zone, stepData);
        }

        chatUI.FinishDynamicStream();
    }

    // ==========================================
    // =           自由對話與尋路                 =
    // ==========================================

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
        if (playerController != null) playerController.SetState(PlayerController.PlayerState.Idle); 
    }

    public void SetHighlight(bool highlight) { if (visualManager != null) visualManager.SetHighlight(highlight); }
}
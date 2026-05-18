using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 主線劇本執行器。負責處理主線任務的啟動、推進與狀態更新。
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [Header("狀態監控")]
    public bool isStoryRunning = false;
    public StoryData currentStory;
    public NPCController storyNPC;
    
    private int _currentStepIndex = 0;

    // 定義主線事件廣播
    public event Action<StoryData> OnStoryStarted;
    public event Action<StoryData> OnStoryEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// 由 PlayerInteraction 觸發，開始執行導覽
    /// </summary>
    public void StartStory(StoryData data, NPCController npc)
    {
        if (isStoryRunning || data == null || npc == null) return;

        currentStory = data;
        storyNPC = npc;
        isStoryRunning = true;
        _currentStepIndex = 0;
        
        OnStoryStarted?.Invoke(data);
        
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.MainStory);

        ExecuteCurrentStep();
    }

    private void ExecuteCurrentStep()
    {
        if (currentStory == null || _currentStepIndex >= currentStory.steps.Count)
        {
            FinishStory();
            return;
        }

        StoryData.StoryStep step = currentStory.steps[_currentStepIndex];
        if (storyNPC != null)
        {
            // 驅動 NPC 前往該站點
            storyNPC.ExecuteNavigation(step.locationID);
        }
    }

    public void OnStepArrival()
    {
        if (!isStoryRunning) return;
        
        if (StoryVisualManager.Instance != null)
        {
            // 傳入當前步驟，讓視覺管理器知道要淡出哪些 Quad
            StoryVisualManager.Instance.EndCinematic(GetCurrentStep());
        }
        
        _currentStepIndex++;
        ExecuteCurrentStep();
    }

    public StoryData.StoryStep GetCurrentStep()
    {
        if (currentStory == null || _currentStepIndex >= currentStory.steps.Count) return null;
        return currentStory.steps[_currentStepIndex];
    }

    private void FinishStory()
    {
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.MarkStoryComplete(currentStory);

        if (storyNPC != null)
        {
            storyNPC.chatUI.ShowNPCResponse(storyNPC.identity.npcName, currentStory.endStoryDialogue, EndStoryCleanup, EndStoryCleanup);
            storyNPC.isGuide = false;
            
            var account = AccountManager.Instance.CurrentPlayer;
            if (account != null && account.npcMemories != null)
            {
                var memory = account.npcMemories.Find(m => m.npcID == storyNPC.npcName);
                
                // 防呆：如果存檔中還沒有這個 NPC 的記憶欄位，自動為其初始化一個
                if (memory == null)
                {
                    memory = new NPCMemoryData { npcID = storyNPC.npcName, hasFinishedGuide = false };
                    account.npcMemories.Add(memory);
                }
                
                memory.hasFinishedGuide = true; // 標記為已結束
                AccountManager.Instance.SaveProgress(); // 立即存檔到 JSON
                Debug.Log($"[StoryManager] {storyNPC.npcName} 導覽模式已永久關閉並存檔。");
            }
        }
        else
        {
            EndStoryCleanup();
        }
    }

    private void EndStoryCleanup()
    {
        if (storyNPC != null) storyNPC.EndInteraction();
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.FreeMode);

        OnStoryEnded?.Invoke(currentStory);
        
        if (StoryVisualManager.Instance != null)
        {
            StoryVisualManager.Instance.EndCinematic(currentStory != null && _currentStepIndex < currentStory.steps.Count ? GetCurrentStep() : null);
        }
        
        isStoryRunning = false;
        currentStory = null;
        storyNPC = null;
    }
}
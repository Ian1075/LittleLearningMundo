using UnityEngine;
using System;
using System.Collections;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [Header("狀態監控")]
    public bool isStoryRunning = false;
    public StoryData currentStory;
    public NPCController storyNPC;
    
    private int _currentStepIndex = 0;

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
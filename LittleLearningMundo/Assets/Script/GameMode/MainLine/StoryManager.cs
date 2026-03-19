using UnityEngine;
using System.Collections.Generic;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [Header("當前劇本")]
    public StoryData currentStory;

    [Header("執行 NPC")]
    public NPCController storyNPC;
    
    private int _currentStepIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public StoryData.StoryStep GetCurrentStep()
    {
        if (currentStory == null || _currentStepIndex >= currentStory.steps.Count) return null;
        return currentStory.steps[_currentStepIndex];
    }

    public void StartStory()
    {
        if (currentStory == null) return;
        _currentStepIndex = 0;
        ExecuteStep();
    }

    private void ExecuteStep()
    {
        if (_currentStepIndex >= currentStory.steps.Count)
        {
            Debug.Log("<color=green>[主線全路線結束！解鎖筆記]</color>");
            
            // --- 關鍵新增：在這裡通知 ProgressManager 解鎖這條路線的筆記 ---
            if (ProgressManager.Instance != null)
            {
                ProgressManager.Instance.UnlockNoteForStory(currentStory);
            }

            if (storyNPC != null)
            {
                storyNPC.chatUI.ShowNPCResponse(
                    storyNPC.identity.npcName, 
                    currentStory.endStoryDialogue, 
                    CleanupAndSwitchMode, 
                    CleanupAndSwitchMode 
                );
            }
            else
            {
                CleanupAndSwitchMode();
            }
            return;
        }

        StoryData.StoryStep step = currentStory.steps[_currentStepIndex];
        if (storyNPC != null) storyNPC.ExecuteNavigation(step.locationID);
    }

    public void OnStepArrival()
    {
        var step = GetCurrentStep();
        if (StoryVisualManager.Instance != null && step != null)
        {
            StoryVisualManager.Instance.EndCinematic(step);
        }

        _currentStepIndex++;
        Invoke(nameof(ExecuteStep), 2.0f);
    }

    private void CleanupAndSwitchMode()
    {
        var step = GetCurrentStep();
        if (StoryVisualManager.Instance != null && step != null)
        {
            StoryVisualManager.Instance.EndCinematic(step);
        }

        if (storyNPC != null) storyNPC.EndInteraction();
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.FreeMode);
    }
}
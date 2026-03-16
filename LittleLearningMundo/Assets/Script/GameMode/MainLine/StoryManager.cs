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
            Debug.Log("<color=green>[主線結束]</color>");
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
        // 這一站介紹完畢，通知視覺管理器關閉所有該站的照片並恢復鏡頭
        var step = GetCurrentStep();
        if (StoryVisualManager.Instance != null && step != null)
        {
            StoryVisualManager.Instance.EndCinematic(step);
        }

        _currentStepIndex++;
        // 延遲一段時間後前往下一站
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
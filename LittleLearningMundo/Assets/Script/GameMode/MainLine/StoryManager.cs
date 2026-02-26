using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理主線導覽流程。確保在對話結束前不會錯誤關閉視覺效果。
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [Header("劇本設定")]
    public StoryData currentStory;
    public NPCController storyNPC;
    
    private int _currentStepIndex = 0;
    private BuildingZone _activeZone;

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
        if (currentStory == null || currentStory.steps.Count == 0) return;
        _currentStepIndex = 0;
        ExecuteStep();
    }

    private void ExecuteStep()
    {
        if (_currentStepIndex >= currentStory.steps.Count)
        {
            TriggerStoryCompletion();
            return;
        }

        StoryData.StoryStep step = currentStory.steps[_currentStepIndex];
        if (storyNPC != null) storyNPC.ExecuteNavigation(step.locationID);
    }

    /// <summary>
    /// 啟動抵達站點的視覺演出 (鏡頭與投影)
    /// </summary>
    public void NotifyArrivalVisuals(BuildingZone zone)
    {
        _activeZone = zone;
        var step = GetCurrentStep();
        if (step != null && StoryVisualManager.Instance != null)
        {
            StoryVisualManager.Instance.StartCinematic(zone, step.projectionImage);
        }
    }

    /// <summary>
    /// 當該站介紹完畢 (玩家點完對話)，由 NPCController 呼叫
    /// </summary>
    public void OnStepArrival()
    {
        // 1. 關閉視覺演出 (歸還相機)
        if (_activeZone != null && StoryVisualManager.Instance != null)
        {
            StoryVisualManager.Instance.EndCinematic(_activeZone);
        }

        // 2. 邁向下一站
        _currentStepIndex++;
        Invoke(nameof(ExecuteStep), 2.5f);
    }

    private void TriggerStoryCompletion()
    {
        if (storyNPC != null)
        {
            storyNPC.chatUI.ShowNPCResponse(
                storyNPC.identity.npcName, 
                currentStory.endStoryDialogue, 
                CleanupAndSwitchMode, 
                CleanupAndSwitchMode
            );
        }
    }

    private void CleanupAndSwitchMode()
    {
        if (storyNPC != null) storyNPC.EndInteraction();
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.FreeMode);
    }
}
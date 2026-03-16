using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理主線劇本。修正：在結束導覽時，確保攝影機恢復。
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
        if (currentStory == null) return;
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

    public void OnStepArrival()
    {
        // 這一站結束了，恢復攝影機
        if (StoryVisualManager.Instance != null)
        {
            StoryVisualManager.Instance.EndCinematic(_activeZone);
        }

        _currentStepIndex++;
        // 延遲一段時間後前往下一站
        Invoke(nameof(ExecuteStep), 2.0f);
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

    /// <summary>
    /// 徹底清理導覽狀態並切換回自由模式
    /// </summary>
    private void CleanupAndSwitchMode()
    {
        Debug.Log("[StoryManager] 導覽正式結束，執行清理。");
        
        // 1. 確保攝影機一定恢復
        if (StoryVisualManager.Instance != null)
        {
            StoryVisualManager.Instance.EndCinematic(_activeZone);
        }

        // 2. NPC 狀態清理
        if (storyNPC != null) storyNPC.EndInteraction();

        // 3. 切換模式
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.FreeMode);
    }

    public void NotifyArrivalVisuals(BuildingZone zone)
    {
        _activeZone = zone;
        var step = GetCurrentStep();
        if (step != null && StoryVisualManager.Instance != null)
        {
            StoryVisualManager.Instance.StartCinematic(zone, step.projections);
        }
    }
}
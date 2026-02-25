using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 負責讀取 StoryData 並控制 NPC 執行主線任務流程。
/// </summary>
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

    /// <summary>
    /// 開始執行主線導覽
    /// </summary>
    public void StartStory()
    {
        if (currentStory == null || currentStory.steps.Count == 0)
        {
            Debug.LogWarning("[StoryManager] 沒有設定 StoryData 或步驟為空！");
            GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.FreeMode);
            return;
        }

        _currentStepIndex = 0;
        Debug.Log($"<color=yellow>[主線啟動] {currentStory.storyTitle}</color>");
        ExecuteStep();
    }

    private void ExecuteStep()
    {
        if (_currentStepIndex >= currentStory.steps.Count)
        {
            Debug.Log("<color=green>[主線結束] 全部導覽已完成。</color>");
            GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.FreeMode);
            return;
        }

        StoryData.StoryStep step = currentStory.steps[_currentStepIndex];
        
        // 讓 NPC 導航去該步驟的地點
        if (storyNPC != null)
        {
            Debug.Log($"[主線] 前往下一站: {step.locationID}");
            storyNPC.ExecuteNavigation(step.locationID);
        }
    }

    /// <summary>
    /// 當 NPC 抵達目的地並播放完解說後觸發
    /// </summary>
    public void OnStepArrival()
    {
        _currentStepIndex++;
        
        // 抵達後停留一小段時間再走下一步
        float waitTime = 3.0f;
        Invoke(nameof(ExecuteStep), waitTime);
    }
}
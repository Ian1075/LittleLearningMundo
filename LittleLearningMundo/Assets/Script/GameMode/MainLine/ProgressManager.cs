using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 負責記錄玩家的導覽進度與解鎖的筆記。
/// </summary>
public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    [Header("已解鎖的筆記")]
    [Tooltip("注意：遊戲開始前請保持這裡為空 (Size = 0)，否則一開始就會直接顯示！")]
    public List<StoryData> completedStories = new List<StoryData>();

    // 定義一個事件，當有新筆記解鎖時廣播給 UI
    public event Action<StoryData> OnNoteUnlocked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 完成某條路線的最後一個地點時呼叫，解鎖該路線專屬筆記
    /// </summary>
    public void UnlockNoteForStory(StoryData story)
    {
        if (story == null) return;

        // 避免重複加入相同的筆記
        if (!completedStories.Contains(story))
        {
            completedStories.Add(story);
            Debug.Log($"<color=orange>[進度解鎖] 獲得新路線筆記：{story.noteTitle}</color>");
            
            // 通知 NotebookUIManager 更新畫面
            OnNoteUnlocked?.Invoke(story);
        }
    }
}
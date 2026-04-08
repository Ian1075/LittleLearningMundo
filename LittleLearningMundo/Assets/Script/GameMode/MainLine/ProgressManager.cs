using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 管理玩家的導覽進度，與 AccountManager 存檔連動。
/// </summary>
public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    [Header("場景中的劇本庫")]
    [Tooltip("請將場景中所有掛載 StoryData 的物件拖入此處")]
    public List<StoryData> allStoriesInScene = new List<StoryData>();

    [Header("進度設定")]
    public int totalRoutesCount = 3;

    [Header("當前玩家已解鎖清單")]
    public List<StoryData> completedStories = new List<StoryData>();

    public event Action<StoryData> OnNoteUnlocked;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 從 AccountManager 加載存檔進度
        if (AccountManager.Instance != null && AccountManager.Instance.CurrentPlayer != null)
        {
            LoadFromPlayer(AccountManager.Instance.CurrentPlayer);
        }
    }

    private void LoadFromPlayer(PlayerData data)
    {
        completedStories.Clear();
        if (data.unlockedStoryTitles == null) return;

        foreach (string title in data.unlockedStoryTitles)
        {
            StoryData s = allStoriesInScene.Find(x => x.storyTitle == title);
            if (s != null) 
            {
                completedStories.Add(s);
                OnNoteUnlocked?.Invoke(s); 
            }
        }
    }

    /// <summary>
    /// 檢查某個故事是否已經完成
    /// </summary>
    public bool IsStoryCompleted(string storyTitle)
    {
        return completedStories.Exists(s => s.storyTitle == storyTitle);
    }

    /// <summary>
    /// 尋找某位 NPC 負責且玩家尚未完成的故事
    /// </summary>
    public StoryData GetAvailableStoryForNPC(string npcName)
    {
        return allStoriesInScene.Find(s => s.responsibleNPCName == npcName && !IsStoryCompleted(s.storyTitle));
    }

    public void MarkStoryComplete(StoryData story)
    {
        if (story == null || completedStories.Contains(story)) return;

        completedStories.Add(story);
        OnNoteUnlocked?.Invoke(story);

        // 同步儲存至帳號
        if (AccountManager.Instance != null && AccountManager.Instance.CurrentPlayer != null)
        {
            if (!AccountManager.Instance.CurrentPlayer.unlockedStoryTitles.Contains(story.storyTitle))
            {
                AccountManager.Instance.CurrentPlayer.unlockedStoryTitles.Add(story.storyTitle);
                AccountManager.Instance.SaveProgress();
            }
        }
    }

    public float GetProgressPercentage() => totalRoutesCount > 0 ? (float)completedStories.Count / totalRoutesCount : 0f;
}
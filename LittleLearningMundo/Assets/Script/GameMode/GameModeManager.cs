using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理全域遊戲模式 (自由模式 vs 主線模式)
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    public enum GameMode { FreeMode, MainStory }

    [Header("當前狀態")]
    public GameMode currentMode = GameMode.FreeMode;

    [Header("NPC 分組管理")]
    public NPCController storyGuideNPC;
    public List<NPCController> freeRoamingNPCs = new List<NPCController>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ApplyModeSettings();
    }

    /// <summary>
    /// 切換遊戲模式 (統一使用 SetGameMode 解決報錯)
    /// </summary>
    public void SetGameMode(GameMode newMode)
    {
        currentMode = newMode;
        Debug.Log($"<color=cyan>[模式切換] 當前進入：{newMode}</color>");
        
        ApplyModeSettings();

        if (newMode == GameMode.MainStory)
        {
            // 啟動主線故事邏輯
            StoryManager.Instance?.StartStory();
        }
    }

    private void ApplyModeSettings()
    {
        bool isStory = (currentMode == GameMode.MainStory);

        if (storyGuideNPC != null) 
            storyGuideNPC.gameObject.SetActive(true);

        foreach (var npc in freeRoamingNPCs)
        {
            if (npc != null) npc.gameObject.SetActive(!isStory);
        }
    }
}
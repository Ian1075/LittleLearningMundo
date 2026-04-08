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
    /// 切換遊戲模式
    /// </summary>
    public void SetGameMode(GameMode newMode)
    {
        currentMode = newMode;
        Debug.Log($"<color=cyan>[模式切換] 當前進入：{newMode}</color>");
        
        ApplyModeSettings();

        if (newMode == GameMode.MainStory)
        {
            // 修正：呼叫 StartStory 時必須提供劇本資料與 NPC 引用
            if (StoryManager.Instance != null && storyGuideNPC != null)
            {
                // 從 ProgressManager 獲取目前 NPC 負責且玩家尚未完成的劇本
                StoryData data = ProgressManager.Instance.GetAvailableStoryForNPC(storyGuideNPC.gameObject.name);
                
                if (data != null)
                {
                    StoryManager.Instance.StartStory(data, storyGuideNPC);
                }
                else
                {
                    Debug.LogWarning("[GameMode] 找不到該 NPC 負責的可執行劇本，退回自由模式。");
                    SetGameMode(GameMode.FreeMode);
                }
            }
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
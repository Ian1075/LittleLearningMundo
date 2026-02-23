using UnityEngine;
using System.Collections.Generic;
using OllamaIntegration.Models;

/// <summary>
/// 專門管理對話紀錄與人設注入的模組。
/// </summary>
public class NPCMemoryManager : MonoBehaviour
{
    [Header("人設設定")]
    [TextArea(3, 5)] 
    public string npcPersonality = "你是一位就讀於成大資工系的學長。請用繁體中文回答。";

    [Header("對話記憶")]
    [SerializeField] private List<OllamaChatMessage> _chatHistory = new List<OllamaChatMessage>();

    /// <summary>
    /// 根據是否有對話紀錄與當前地點，生成第一句問候。
    /// </summary>
    public string GetInitialGreeting(BuildingZone zone)
    {
        if (_chatHistory.Count > 0) return "還有什麼想問的嗎？";
        
        string locInfo = zone != null ? $"我們現在在{zone.displayName}。" : "";
        return $"嗨！{locInfo} 有什麼我可以幫你的？";
    }

    /// <summary>
    /// 準備發送給 AI 的所有訊息，並注入動態地點知識。
    /// </summary>
    public List<OllamaChatMessage> PrepareMessages(string playerInput, BuildingZone zone)
    {
        string spatialContext = (zone != null) ? 
            $"\n[地點資訊：{zone.displayName}]\n[地點背景：{zone.knowledgeBase}]" : "";

        // 1. 處理系統指令 (確保第一筆資料永遠是最新的人設+環境資訊)
        if (_chatHistory.Count == 0)
        {
            _chatHistory.Add(new OllamaChatMessage { role = "system", content = npcPersonality + spatialContext });
        }
        else
        {
            _chatHistory[0].content = npcPersonality + spatialContext;
        }

        // 2. 加入玩家輸入
        _chatHistory.Add(new OllamaChatMessage { role = "user", content = playerInput });

        return _chatHistory;
    }

    /// <summary>
    /// 儲存 AI 的回覆到記憶中。
    /// </summary>
    public void SaveAssistantResponse(string content)
    {
        _chatHistory.Add(new OllamaChatMessage { role = "assistant", content = content });
    }

    /// <summary>
    /// 重置記憶。
    /// </summary>
    public void ClearMemory()
    {
        _chatHistory.Clear();
    }
}
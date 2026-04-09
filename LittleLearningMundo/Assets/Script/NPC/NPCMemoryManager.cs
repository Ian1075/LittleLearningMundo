using System.Collections.Generic;
using UnityEngine;
using OllamaIntegration.Models;

public class NPCMemoryManager : MonoBehaviour
{
    [Header("綁定設定")]
    public NPCController controller;

    [Header("記憶設定")]
    public int maxHistoryTurns = 5;

    private List<OllamaChatMessage> _conversationHistory = new List<OllamaChatMessage>();

    private void Start()
    {
        // 延遲一點點確保 AccountManager 或 NPCIdentity 已初始化
        Invoke(nameof(LoadHistoryFromAccount), 0.1f);
    }

    private void LoadHistoryFromAccount()
    {
        if (AccountManager.Instance != null && AccountManager.Instance.CurrentPlayer != null && controller != null && controller.identity != null)
        {
            _conversationHistory.Clear();
            
            // 【修改】根據當前 NPC 的 ID 讀取對應記憶
            string myID = controller.identity.npcName;
            var savedHistory = AccountManager.Instance.CurrentPlayer.GetMemoryForNPC(myID);

            foreach (var savedMsg in savedHistory)
            {
                _conversationHistory.Add(new OllamaChatMessage { 
                    role = savedMsg.role, 
                    content = savedMsg.content 
                });
            }
            Debug.Log($"<color=green>[Memory] NPC:{myID} 已從存檔載入 {_conversationHistory.Count} 條對話紀錄。</color>");
        }
    }

    private void SyncToAccount()
    {
        if (AccountManager.Instance != null && AccountManager.Instance.CurrentPlayer != null && controller != null && controller.identity != null)
        {
            // 【修改】定位到該 NPC 的記憶區塊並更新
            string myID = controller.npcName;
            var accountHistory = AccountManager.Instance.CurrentPlayer.GetMemoryForNPC(myID);
            
            accountHistory.Clear();
            foreach (var msg in _conversationHistory)
            {
                accountHistory.Add(new SavedChatMessage { 
                    role = msg.role, 
                    content = msg.content 
                });
            }
            // 每次對話完自動存檔
            AccountManager.Instance.SaveProgress();
        }
    }

    public void SaveAssistantResponse(string text)
    {
        if (string.IsNullOrEmpty(text)) return; // 避免存入空字串
        _conversationHistory.Add(new OllamaChatMessage { role = "assistant", content = text });
        TrimHistory();
        SyncToAccount();
    }

    public void SaveUserRequest(string text)
    {
        // 這裡的邏輯應該與 SaveAssistantResponse 類似
        // 只是角色要設為 "user"
        var currentAccount = AccountManager.Instance.CurrentPlayer;
        var memory = currentAccount.npcMemories.Find(m => m.npcID == controller.npcName);
                _conversationHistory.Add(new OllamaChatMessage { role = "user", content = text });
        TrimHistory();
        SyncToAccount();

    }

    public void AddSystemEvent(string eventText)
    {
        string prompt = $"[系統提示：導覽進行中]\n{eventText}\n(請順著剛才的對話繼續，不要重新打招呼)";
        _conversationHistory.Add(new OllamaChatMessage { role = "user", content = prompt });
        TrimHistory();
        SyncToAccount();
    }

    public void ClearHistory()
    {
        _conversationHistory.Clear();
        SyncToAccount();
    }

    private void TrimHistory()
    {
        int maxMessages = maxHistoryTurns * 2;
        if (_conversationHistory.Count > maxMessages)
        {
            _conversationHistory.RemoveRange(0, _conversationHistory.Count - maxMessages);
        }
    }

    public List<OllamaChatMessage> PrepareMessages(string currentInput, BuildingZone currentZone)
    {
        List<OllamaChatMessage> messages = new List<OllamaChatMessage>();
        if (controller == null || controller.identity == null) return messages;

        // 1. System Prompt (包含個性與當前環境)
        string systemContent = $"{controller.identity.coreRules}\n\n";
        if (currentZone != null)
            systemContent += $"[目前所在地點：{currentZone.displayName}]\n[該地知識：{currentZone.knowledgeBase}]";
        else
            systemContent += "[目前所在地點：校園步道 (移動中)]";

        messages.Add(new OllamaChatMessage { role = "system", content = systemContent });

        // 2. 歷史記憶
        messages.AddRange(_conversationHistory);

        // 3. 當前輸入
        if (!string.IsNullOrEmpty(currentInput))
        {
            var userMsg = new OllamaChatMessage { role = "user", content = currentInput };
            messages.Add(userMsg);
            
            if (!currentInput.StartsWith("[")) 
            {
                _conversationHistory.Add(userMsg);
                TrimHistory();
                SyncToAccount();
            }
        }
        return messages;
    }
}
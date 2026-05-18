using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

/// <summary>
/// 儲存單條聊天紀錄的資料結構
/// </summary>
[Serializable]
public class SavedChatMessage
{
    public string role;
    public string content;
}

/// <summary>
/// 存放單一 NPC 記憶（包含對話歷史與導覽狀態）的資料結構
/// </summary>
[Serializable]
public class NPCMemoryData
{
    public string npcID;
    public bool hasFinishedGuide; // 儲存是否已完成該 NPC 的導覽
    public List<SavedChatMessage> chatHistory = new List<SavedChatMessage>(); // 儲存該 NPC 的歷史聊天對話
}

/// <summary>
/// 完整玩家存檔資料結構
/// </summary>
[Serializable]
public class PlayerData
{
    public string playerName;
    public string password; 
    public List<string> unlockedStoryTitles = new List<string>(); 
    public List<NPCMemoryData> npcMemories = new List<NPCMemoryData>();

    /// <summary>
    /// 安全獲取或建立指定 NPC 的記憶資料結構，防止空指針異常
    /// </summary>
    public NPCMemoryData GetNPCMemoryData(string npcID)
    {
        if (npcMemories == null)
        {
            npcMemories = new List<NPCMemoryData>();
        }

        NPCMemoryData data = npcMemories.Find(m => m.npcID == npcID);
        if (data == null)
        {
            data = new NPCMemoryData { 
                npcID = npcID, 
                hasFinishedGuide = false, 
                chatHistory = new List<SavedChatMessage>() 
            };
            npcMemories.Add(data);
        }

        if (data.chatHistory == null)
        {
            data.chatHistory = new List<SavedChatMessage>();
        }

        return data;
    }

    /// <summary>
    /// 安全獲取指定 NPC 的對話紀錄列表（若無則自動初始化）
    /// </summary>
    public List<SavedChatMessage> GetMemoryForNPC(string npcID)
    {
        return GetNPCMemoryData(npcID).chatHistory;
    }
}

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }

    public PlayerData CurrentPlayer { get; private set; }
    public event Action<PlayerData> OnAccountLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 登入現有帳號
    /// </summary>
    public bool Login(string playerName, string password, out string errorMessage)
    {
        errorMessage = "";
        string savePath = GetSavePath(playerName);

        if (!File.Exists(savePath))
        {
            errorMessage = "找不到此帳號，請先註冊！";
            return false;
        }

        string json = File.ReadAllText(savePath);
        PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);

        if (loadedData.password == password)
        {
            // 載入時的防呆保護
            if (loadedData.npcMemories == null)
                loadedData.npcMemories = new List<NPCMemoryData>();

            CurrentPlayer = loadedData;
            OnAccountLoaded?.Invoke(CurrentPlayer);
            return true;
        }
        else
        {
            errorMessage = "密碼錯誤！";
            return false;
        }
    }

    /// <summary>
    /// 註冊新帳號
    /// </summary>
    public bool Register(string playerName, string password, out string errorMessage)
    {
        errorMessage = "";
        string savePath = GetSavePath(playerName);

        if (File.Exists(savePath))
        {
            errorMessage = "此名稱已被註冊！";
            return false;
        }

        // 建立新資料
        CurrentPlayer = new PlayerData { 
            playerName = playerName, 
            password = password,
            npcMemories = new List<NPCMemoryData>()
        };
        SaveProgress();
        OnAccountLoaded?.Invoke(CurrentPlayer);
        return true;
    }

    public void SaveProgress()
    {
        if (CurrentPlayer == null) return;
        string savePath = GetSavePath(CurrentPlayer.playerName);
        string json = JsonUtility.ToJson(CurrentPlayer, true); 
        File.WriteAllText(savePath, json);
    }

    private string GetSavePath(string playerName)
    {
        return Path.Combine(Application.persistentDataPath, $"{playerName}_save.json");
    }
}
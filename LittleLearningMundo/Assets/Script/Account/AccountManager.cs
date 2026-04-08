using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using OllamaIntegration.Models;

[Serializable]
public class SavedChatMessage
{
    public string role;
    public string content;
}

// 【新增】定義單個 NPC 的記憶區塊
[Serializable]
public class NPCMemory
{
    public string npcID;
    public List<SavedChatMessage> chatHistory = new List<SavedChatMessage>();
}

[Serializable]
public class PlayerData
{
    public string playerName;
    public string password; 
    public List<string> unlockedStoryTitles = new List<string>(); 
    
    // 【修改】從單一列表改為多個 NPC 的記憶列表
    public List<NPCMemory> npcMemories = new List<NPCMemory>();

    // 【新增】輔助工具：根據 ID 取得或創建記憶區塊
    public List<SavedChatMessage> GetMemoryForNPC(string id)
    {
        NPCMemory memory = npcMemories.Find(m => m.npcID == id);
        if (memory == null)
        {
            memory = new NPCMemory { npcID = id };
            npcMemories.Add(memory);
        }
        return memory.chatHistory;
    }
}

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }
    public PlayerData CurrentPlayer { get; private set; }
    public event Action<PlayerData> OnAccountLoaded;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public bool Login(string playerName, string password, out string errorMessage)
    {
        errorMessage = "";
        string savePath = GetSavePath(playerName);
        if (!File.Exists(savePath)) { errorMessage = "找不到此帳號，請先註冊！"; return false; }

        PlayerData data = JsonUtility.FromJson<PlayerData>(File.ReadAllText(savePath));
        if (data.password == password) { 
            CurrentPlayer = data; 
            OnAccountLoaded?.Invoke(data); 
            return true; 
        }
        errorMessage = "密碼錯誤！";
        return false;
    }

    public bool Register(string playerName, string password, out string errorMessage)
    {
        errorMessage = "";
        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(password)) {
            errorMessage = "名稱或密碼不能為空！";
            return false;
        }
        string savePath = GetSavePath(playerName);
        if (File.Exists(savePath)) { errorMessage = "此名稱已被註冊！"; return false; }

        CurrentPlayer = new PlayerData { playerName = playerName, password = password };
        SaveProgress();
        OnAccountLoaded?.Invoke(CurrentPlayer);
        return true;
    }

    public void SaveProgress()
    {
        if (CurrentPlayer == null) return;
        File.WriteAllText(GetSavePath(CurrentPlayer.playerName), JsonUtility.ToJson(CurrentPlayer, true));
    }

    private string GetSavePath(string name) => Path.Combine(Application.persistentDataPath, $"{name}_save.json");
}
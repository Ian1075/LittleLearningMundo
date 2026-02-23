using UnityEngine;
using System.Collections.Generic;
using OllamaIntegration.Models;

/// <summary>
/// NPC 核心大腦：負責協調所有子模組 (記憶、感應、視覺) 與 UI。
/// </summary>
public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Greeting, WaitingForInput, Thinking }

    [Header("狀態與身分")]
    public NPCState currentState = NPCState.Idle;
    public string npcName = "資工學長";

    [Header("模組引用")]
    public NPCMemoryManager memoryManager;     // 記憶模組
    public NPCLocationSensor locationSensor;   // 地點感應模組
    public NPCVisualManager visualManager;     // 視覺模組
    public OllamaService ollamaService;        // AI 服務
    public ChatUIManager chatUI;               // UI 控制器

    private void Start()
    {
        // 1. 預熱模型
        if (ollamaService != null && memoryManager != null) 
            ollamaService.PrewarmModel(memoryManager.npcPersonality);
        
        // 2. 初始化視覺
        if (visualManager != null) 
            visualManager.SetHighlight(false);
    }

    /// <summary>
    /// 開始對話 (由 PlayerInteraction 呼叫)
    /// </summary>
    public void StartGreeting()
    {
        currentState = NPCState.Greeting;

        // 獲取地點資訊
        BuildingZone zone = locationSensor != null ? locationSensor.GetCurrentZone() : null;
        
        // 獲取初始問候語
        string greeting = memoryManager != null ? memoryManager.GetInitialGreeting(zone) : "嘿！你好。";
        
        // 顯示回覆並註冊結束回呼
        chatUI.ShowNPCResponse(npcName, greeting, EndInteraction);
    }

    /// <summary>
    /// 切換到輸入模式
    /// </summary>
    public void SwitchToInputMode()
    {
        currentState = NPCState.WaitingForInput;
        chatUI.OpenPlayerInput(OnPlayerSubmit);
    }

    private async void OnPlayerSubmit(string playerInput)
    {
        currentState = NPCState.Thinking;

        BuildingZone zone = locationSensor != null ? locationSensor.GetCurrentZone() : null;
        
        // 準備 AI 訊息
        var history = memoryManager.PrepareMessages(playerInput, zone);

        // 向 AI 請求
        string aiResponse = await ollamaService.RequestChatAsync(history);
        
        // 儲存回覆
        memoryManager.SaveAssistantResponse(aiResponse);

        // 顯示結果
        chatUI.ShowNPCResponse(npcName, aiResponse, EndInteraction);
        currentState = NPCState.Greeting;
    }

    /// <summary>
    /// 結束對話
    /// </summary>
    public void EndInteraction()
    {
        chatUI.CloseChat();
        currentState = NPCState.Idle;

        // 解鎖玩家
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.SetState(PlayerController.PlayerState.Idle);
    }
}
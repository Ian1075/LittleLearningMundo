using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaIntegration.Models;

/// <summary>
/// NPC 邏輯大腦：處理對話記憶、模型預熱。
/// 視覺效果改為「切換材質」模式。
/// </summary>
public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Greeting, WaitingForInput, Thinking }

    [Header("狀態管理")]
    public NPCState currentState = NPCState.Idle;
    public string npcName = "資工學長";

    [Header("AI 人設")]
    [TextArea(3, 5)] 
    public string npcPersonality = "你是一位成大資工系的學員，語氣親切幽默。請用繁體中文回答。";

    [Header("對話記憶 (單次互動)")]
    private List<OllamaChatMessage> _chatHistory = new List<OllamaChatMessage>();

    [Header("系統引用")]
    public OllamaService ollamaService;
    public ChatUIManager chatUI; 

    [Header("視覺效果 (材質切換)")]
    [Tooltip("請將 NPC 的 Mesh Renderer 拖入此處")]
    public Renderer npcRenderer;
    [Tooltip("平時顯示的原始材質")]
    public Material originalMaterial;
    [Tooltip("選中時顯示的邊框高亮材質")]
    public Material highlightMaterial;

    private void Start()
    {
        // 1. 自動檢查 Renderer
        if (npcRenderer == null)
        {
            npcRenderer = GetComponentInChildren<Renderer>();
        }

        // 2. 自動備份原始材質 (如果在 Inspector 沒拉的話)
        if (npcRenderer != null && originalMaterial == null)
        {
            originalMaterial = npcRenderer.sharedMaterial;
        }

        // 3. 啟動時預熱 Ollama 模型
        if (ollamaService != null)
        {
            ollamaService.PrewarmModel(npcPersonality);
        }
        
        // 4. 初始狀態確保使用原始材質
        SetHighlight(false);
    }

    /// <summary>
    /// [公開方法] 透過切換材質來開啟或關閉 NPC 的邊框效果。
    /// </summary>
    /// <param name="highlight">是否切換至高亮材質</param>
    public void SetHighlight(bool highlight)
    {
        if (npcRenderer == null || originalMaterial == null || highlightMaterial == null) 
        {
            // 如果材質沒拉，給予警告但不要報錯中斷
            return;
        }

        // 執行材質切換
        npcRenderer.material = highlight ? highlightMaterial : originalMaterial;
    }

    /// <summary>
    /// 啟動互動 (通常由 PlayerInteraction 呼叫)
    /// </summary>
    public void StartGreeting()
    {
        currentState = NPCState.Greeting;
        string greeting = "嘿！找我有事嗎？";
        if (_chatHistory.Count > 0) greeting = "還有什麼想問的嗎？";
        chatUI.ShowNPCResponse(npcName, greeting);
    }

    /// <summary>
    /// 切換到玩家輸入模式
    /// </summary>
    public void SwitchToInputMode()
    {
        currentState = NPCState.WaitingForInput;
        chatUI.OpenPlayerInput(OnPlayerSubmit);
    }

    /// <summary>
    /// 處理 AI 對話邏輯
    /// </summary>
    private async void OnPlayerSubmit(string playerInput)
    {
        currentState = NPCState.Thinking;
        
        // 初始化記憶
        if (_chatHistory.Count == 0)
            _chatHistory.Add(new OllamaChatMessage { role = "system", content = npcPersonality });

        _chatHistory.Add(new OllamaChatMessage { role = "user", content = playerInput });
        
        // 發送請求給 Ollama
        string aiResponse = await ollamaService.RequestChatAsync(_chatHistory);
        
        _chatHistory.Add(new OllamaChatMessage { role = "assistant", content = aiResponse });

        chatUI.ShowNPCResponse(npcName, aiResponse);
        currentState = NPCState.Greeting; 
    }

    /// <summary>
    /// 結束對話，解鎖玩家
    /// </summary>
    public void EndInteraction()
    {
        chatUI.CloseChat();
        currentState = NPCState.Idle;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.SetState(PlayerController.PlayerState.Idle);
    }
}
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using OllamaIntegration.Models;

/// <summary>
/// 負責與本地 Ollama 伺服器進行通訊的核心服務。
/// 使用 Unity 內建的 JsonUtility 進行資料序列化。
/// </summary>
public class OllamaService : MonoBehaviour
{
    [Header("API 配置")]
    [Tooltip("本地 Ollama 的 Chat API 路徑")]
    public string apiUrl = "http://localhost:11434/api/chat";
    [Tooltip("使用的模型名稱，例如 llama3.1 或 mistral")]
    public string modelName = "llama3.1:8b";

    /// <summary>
    /// 模型預熱：在 Start 或特定時機呼叫，提前將模型加載至顯存 (VRAM)。
    /// </summary>
    /// <param name="systemPrompt">NPC 的初始人設指令</param>
    public async void PrewarmModel(string systemPrompt)
    {
        Debug.Log($"[Ollama] 開始預熱模型: {modelName}...");

        // 構造一個極簡的對話紀錄來觸發模型加載
        List<OllamaChatMessage> warmUpMessages = new List<OllamaChatMessage>
        {
            new OllamaChatMessage { role = "system", content = systemPrompt },
            new OllamaChatMessage { role = "user", content = "hi" }
        };

        // 發送請求但不處理回傳結果，僅為了讓模型 Loading
        await RequestChatAsync(warmUpMessages);
        
        Debug.Log("[Ollama] 模型預熱完成，準備就緒。");
    }

    /// <summary>
    /// 發送包含對話歷史紀錄的請求給 Ollama。
    /// </summary>
    /// <param name="chatHistory">完整的對話列表 (包含 system, user, assistant)</param>
    /// <returns>AI 回傳的文字內容</returns>
    public async Task<string> RequestChatAsync(List<OllamaChatMessage> chatHistory)
    {
        // 1. 準備請求資料 (根據你的 OllamaDataTypes.cs)
        ChatRequest payload = new ChatRequest
        {
            model = modelName,
            messages = chatHistory,
            stream = false // 關閉串流以配合 JsonUtility 解析
        };

        // 2. 序列化為 JSON 字串
        string jsonPayload = JsonUtility.ToJson(payload);

        // 3. 建立 Web Request
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. 發送並等待
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            // 5. 處理回傳結果
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    // 使用 JsonUtility 解析回傳資料
                    ChatResponse responseData = JsonUtility.FromJson<ChatResponse>(responseText);
                    
                    if (responseData != null && responseData.message != null)
                    {
                        return responseData.message.content;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Ollama] 解析回傳資料失敗: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[Ollama] 請求失敗: {request.error}\n請確認 Ollama 是否已啟動。");
            }
        }

        return "（通訊暫時中斷，請檢查連線狀態）";
    }
}
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions; 
using OllamaIntegration.Models;
using Newtonsoft.Json;

/// <summary>
/// 升級版：支援串流對話與向量提取 (Embeddings) 的 Ollama 客戶端。
/// </summary>
public class OllamaApiClient : MonoBehaviour
{
    [Header("網路配置")]
    [Tooltip("Ollama 伺服器基礎位址，例如 http://140.116.154.86:11434")]
    public string baseHostUrl = "http://140.116.154.86:11434";
    
    [Header("偵錯設定")]
    public bool logFullPayload = true;
    public bool logFullJsonResponse = true; 

    // --- 新增：Embedding 專用的資料結構 ---
    [Serializable]
    public class EmbeddingRequest { public string model; public string prompt; }
    [Serializable]
    public class EmbeddingResponse { public float[] embedding; }

    /// <summary>
    /// 新增功能：獲取文字的向量值 (Embedding)
    /// 用於方案 A 的向量資料庫搜尋。
    /// </summary>
    public async Task<float[]> GetEmbeddingAsync(string text, string model = "nomic-embed-text")
    {
        string url = $"{baseHostUrl}/api/embeddings";
        var payload = new EmbeddingRequest { model = model, prompt = text };
        string jsonPayload = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<EmbeddingResponse>(request.downloadHandler.text);
                return response.embedding;
            }
            else
            {
                Debug.LogError($"<color=red>[Ollama Embedding Error]</color> {request.error}");
                return null;
            }
        }
    }

    /// <summary>
    /// 發送串流對話請求並重組 JSON 輸出日誌。
    /// </summary>
    public async Task SendChatStreamAsync(OllamaChatRequest payload, Action<string> onChunkReceived, Action<ToolCall> onToolCallReceived)
    {
        string chatUrl = $"{baseHostUrl}/api/chat";
        payload.stream = true; 
        string jsonPayload = JsonConvert.SerializeObject(payload);

        if (logFullPayload) Debug.Log($"<color=yellow>[Ollama API Request]</color>\n{jsonPayload}");

        OllamaChatResponse fullResponse = new OllamaChatResponse();
        fullResponse.message = new OllamaMessageResponse { role = "assistant", content = "" };
        StringBuilder contentBuilder = new StringBuilder();

        using (UnityWebRequest request = new UnityWebRequest(chatUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            
            var handler = new OllamaStreamHandler(false); 
            handler.OnChunkProcessed += (chunkJson) => {
                try {
                    var chunk = JsonConvert.DeserializeObject<OllamaChatResponse>(chunkJson);
                    if (chunk != null)
                    {
                        if (chunk.message != null && !string.IsNullOrEmpty(chunk.message.content))
                        {
                            string clean = RemoveMarkdownAndExtraLines(chunk.message.content);
                            contentBuilder.Append(clean);
                            onChunkReceived?.Invoke(clean);
                        }

                        if (chunk.message?.tool_calls != null && chunk.message.tool_calls.Count > 0)
                        {
                            if (fullResponse.message.tool_calls == null) 
                                fullResponse.message.tool_calls = new List<ToolCall>();
                            
                            fullResponse.message.tool_calls.AddRange(chunk.message.tool_calls);
                            onToolCallReceived?.Invoke(chunk.message.tool_calls[0]);
                        }

                        if (chunk.done)
                        {
                            fullResponse.done = true;
                            fullResponse.total_duration = chunk.total_duration;
                        }
                    }
                } catch { }
            };

            request.downloadHandler = handler;
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                fullResponse.message.content = contentBuilder.ToString();
                if (logFullJsonResponse)
                {
                    string finalJson = JsonConvert.SerializeObject(fullResponse, Formatting.Indented);
                    Debug.Log($"<color=#00FF00>[Ollama Full JSON Response]</color>\n{finalJson}");
                }
            }
            else
            {
                Debug.LogError($"<color=red>[Ollama API Error]</color> {request.error}");
            }
        }
    }

    private string RemoveMarkdownAndExtraLines(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return Regex.Replace(text, @"[\*#_~`\[\]]", "");
    }

    public async Task<OllamaChatResponse> SendChatRequestAsync(OllamaChatRequest payload)
    {
        string chatUrl = $"{baseHostUrl}/api/chat";
        payload.stream = false;
        string jsonPayload = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(chatUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;
                if (logFullJsonResponse) Debug.Log($"<color=yellow>[Ollama API Response]</color>\n{rawResponse}");
                return JsonConvert.DeserializeObject<OllamaChatResponse>(rawResponse);
            }
        }
        return null;
    }
}

// ----------------------------------------------------------------------------
// OllamaStreamHandler 類別定義保持不變
// ----------------------------------------------------------------------------
public class OllamaStreamHandler : DownloadHandlerScript
{
    public event Action<string> OnChunkProcessed;
    private bool _shouldLog;

    public OllamaStreamHandler(bool shouldLog) : base()
    {
        _shouldLog = shouldLog;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0) return false;
        string text = Encoding.UTF8.GetString(data, 0, dataLength);
        string[] lines = text.Split('\n');

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            OnChunkProcessed?.Invoke(trimmed);
        }
        return true;
    }
}
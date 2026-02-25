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
/// 支援串流 (Streaming) 的 Ollama 通訊客戶端。
/// 包含完整的 OllamaStreamHandler 類別定義。
/// </summary>
public class OllamaApiClient : MonoBehaviour
{
    [Header("網路配置")]
    public string apiUrl = "http://140.116.154.86:11434/api/chat";
    
    [Header("偵錯設定")]
    public bool logFullPayload = true;
    public bool logFullJsonResponse = true; 

    /// <summary>
    /// 發送串流對話請求並重組 JSON 輸出日誌。
    /// </summary>
    public async Task SendChatStreamAsync(OllamaChatRequest payload, Action<string> onChunkReceived, Action<ToolCall> onToolCallReceived)
    {
        payload.stream = true; 
        string jsonPayload = JsonConvert.SerializeObject(payload);

        if (logFullPayload)
        {
            Debug.Log($"<color=yellow>[Ollama API Request]</color>\n{jsonPayload}");
        }

        OllamaChatResponse fullResponse = new OllamaChatResponse();
        fullResponse.message = new OllamaMessageResponse { role = "assistant", content = "" };
        StringBuilder contentBuilder = new StringBuilder();

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            
            // 關鍵：這裡建立了 OllamaStreamHandler，定義在檔案下方
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
        payload.stream = false;
        string jsonPayload = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
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
// 請確保以下類別定義有被複製到專案中
// ----------------------------------------------------------------------------

/// <summary>
/// 自定義下載處理器，用於解析 Ollama 傳回的連續 JSON 流。
/// </summary>
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

        // 將收到的 byte 轉換為字串
        string text = Encoding.UTF8.GetString(data, 0, dataLength);
        
        // Ollama 的串流格式是每行一個 JSON 物件，通常以 \n 分隔
        string[] lines = text.Split('\n');

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (_shouldLog)
            {
                Debug.Log($"<color=yellow>[Ollama Raw Chunk]</color> {trimmed}");
            }

            // 觸發事件進行 JSON 解析
            OnChunkProcessed?.Invoke(trimmed);
        }

        return true;
    }
}
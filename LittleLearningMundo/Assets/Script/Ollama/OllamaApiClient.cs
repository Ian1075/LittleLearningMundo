using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;
using System;
using OllamaIntegration.Models;

/// <summary>
/// 負責最底層的網路請求與重試機制
/// </summary>
public class OllamaApiClient : MonoBehaviour
{
    [Header("網路配置")]
    public string apiUrl = "http://localhost:11434/api/chat";
    public int maxRetries = 5;

    public async Task<ChatResponse> SendChatRequestAsync(ChatRequest payload)
    {
        string jsonPayload = JsonUtility.ToJson(payload);
        int currentRetry = 0;

        while (currentRetry < maxRetries)
        {
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
                    return JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                }
                
                Debug.LogWarning($"Ollama 請求失敗 (第 {currentRetry + 1} 次嘗試): {request.error}");
            }

            // 指數退避等待
            await Task.Delay((int)Mathf.Pow(2, currentRetry) * 1000);
            currentRetry++;
        }

        return null;
    }
}
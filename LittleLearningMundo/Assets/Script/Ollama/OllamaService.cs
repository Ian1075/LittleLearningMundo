using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaIntegration.Models;
using Newtonsoft.Json.Linq;

public class OllamaService : MonoBehaviour
{
    public OllamaApiClient apiClient;
    public string modelName = "mistral-small3.2:latest";
    public string keepAliveDuration = "30m";

    private void Start()
    {
        if (apiClient == null) apiClient = GetComponent<OllamaApiClient>();
        _ = WarmupModel();
    }

    public async Task WarmupModel()
    {
        var warmupMessage = new List<OllamaChatMessage> { new OllamaChatMessage { role = "user", content = "ping" } };
        var request = new OllamaChatRequest { model = modelName, messages = warmupMessage, stream = false };
        await apiClient.SendChatRequestAsync(request);
    }

    public OllamaChatRequest CreateRequest(List<OllamaChatMessage> history, bool includeTools = true)
    {
        OllamaChatRequest request = new OllamaChatRequest
        {
            model = modelName,
            messages = history,
            stream = true,
            keep_alive = keepAliveDuration
        };

        if (includeTools)
        {
            // 強化 Schema 描述
            var parametersSchema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["location_id"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "目標地點的唯一代碼",
                        ["enum"] = new JArray { "CSIE", "OPHY" } 
                    }
                },
                ["required"] = new JArray { "location_id" }
            };

            var navigateTool = new ToolDefinition
            {
                function = new FunctionDefinition
                {
                    name = "navigate_to_location",
                    // 強化描述：明確告知 AI 當玩家想去某地時「必須」使用
                    description = "當玩家表達想要前往、參觀或帶路去某個地點時，必須呼叫此工具。不要只用文字回答，必須先呼叫工具觸發移動。",
                    parameters = parametersSchema
                }
            };

            request.tools = new List<ToolDefinition> { navigateTool };
        }

        return request;
    }
}
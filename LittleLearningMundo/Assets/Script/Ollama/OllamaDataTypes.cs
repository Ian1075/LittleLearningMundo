using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OllamaIntegration.Models
{
    [Serializable]
    public class OllamaChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class OllamaChatRequest
    {
        public string model;
        public List<OllamaChatMessage> messages;
        public bool stream;
        public List<ToolDefinition> tools;
        
        // 加入 keep_alive 參數
        // "5m" (5分鐘), "1h" (1小時), "-1" (永久)
        [JsonProperty("keep_alive")]
        public string keep_alive; 
    }

    [Serializable]
    public class ToolDefinition
    {
        public string type = "function";
        public FunctionDefinition function;
    }

    [Serializable]
    public class FunctionDefinition
    {
        public string name;
        public string description;
        public JObject parameters; 
    }

    [Serializable]
    public class OllamaChatResponse
    {
        public OllamaMessageResponse message;
        public bool done;
        // 回傳中包含總耗時，可用於監控效能
        public long total_duration; 
    }

    [Serializable]
    public class OllamaMessageResponse
    {
        public string role;
        public string content;
        public List<ToolCall> tool_calls;
    }

    [Serializable]
    public class ToolCall
    {
        public FunctionCall function;
    }

    [Serializable]
    public class FunctionCall
    {
        public string name;
        public JToken arguments; 
    }
}
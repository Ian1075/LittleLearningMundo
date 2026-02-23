using System;
using System.Collections.Generic;

namespace OllamaIntegration.Models
{
    // JsonUtility 只能序列化帶有 [Serializable] 的類別
    [Serializable]
    public class OllamaChatMessage
    {
        public string role;    // "system", "user", "assistant"
        public string content;
    }

    [Serializable]
    public class ChatRequest
    {
        public string model;
        public List<OllamaChatMessage> messages;
        public bool stream;
    }

    [Serializable]
    public class ChatResponse
    {
        public OllamaChatMessage message;
        public string model;
        public string created_at;
        public bool done;
    }
}
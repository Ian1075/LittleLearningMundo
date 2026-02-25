using UnityEngine;
using System.Collections.Generic;
using OllamaIntegration.Models;

public class NPCMemoryManager : MonoBehaviour
{
    [Header("引用")]
    public NPCController controller; // 用於獲取 identity

    private List<OllamaChatMessage> _chatHistory = new List<OllamaChatMessage>();

    public List<OllamaChatMessage> PrepareMessages(string playerInput, BuildingZone zone)
    {
        if (controller == null || controller.identity == null)
        {
            Debug.LogError("NPCMemoryManager 找不到 NPCIdentity！");
            return _chatHistory;
        }

        string personality = controller.identity.systemInstruction;
        string spatialContext = (zone != null) ? 
            $"\n[目前所在地點：{zone.displayName}]\n[該地知識：{zone.knowledgeBase}]" : "";

        string fullSystemPrompt = personality + spatialContext;

        if (_chatHistory.Count == 0)
        {
            _chatHistory.Add(new OllamaChatMessage { role = "system", content = fullSystemPrompt });
        }
        else
        {
            _chatHistory[0].content = fullSystemPrompt;
        }

        _chatHistory.Add(new OllamaChatMessage { role = "user", content = playerInput });
        return _chatHistory;
    }

    public void SaveAssistantResponse(string content) => _chatHistory.Add(new OllamaChatMessage { role = "assistant", content = content });
    
    public void ClearMemory() => _chatHistory.Clear();
}
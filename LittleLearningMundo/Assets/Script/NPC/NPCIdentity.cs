using UnityEngine;

/// <summary>
/// 存放 NPC 的非程式邏輯資料，如人設、Prompt 模板與預設台詞。
/// </summary>
[CreateAssetMenu(fileName = "NewNPCIdentity", menuName = "NCKU/NPC Identity")]
public class NPCIdentity : ScriptableObject
{
    [Header("基本資訊")]
    public string npcName = "資工學長";

    [Header("AI 角色定義 (System Prompt)")]
    [TextArea(10, 20)]
    public string systemInstruction = @"你是一位成大資工系的學長。講話口語化，愛用「喔」、「啦」、「吧」。
根據目前身處的環境提供適當的對話。
請絕對禁止使用 Markdown 格式。";

    [Header("預設台詞")]
    [TextArea(3, 5)]
    public string defaultGreeting = "嘿！你好啊，我是這裡的導覽學長。想去哪裡看看嗎？我可以帶你過去喔。";
    
    [TextArea(3, 5)]
    public string arrivalReplyTemplate = "沒問題，學長這就帶你去 {0}，跟我來！";

    [Header("事件 Prompt 模板")]
    [TextArea(5, 10)]
    public string arrivalEventPrompt = "[系統事件：已到達目的地]\n地點名稱: {0}\n背景知識: {1}\n\n請根據以上資訊進行一段有趣的實地介紹。";
}
using UnityEngine;

/// <summary>
/// 定義單一條校園知識碎片
/// </summary>
[CreateAssetMenu(fileName = "NewKnowledgeFragment", menuName = "NCKU/Knowledge Fragment")]
public class KnowledgeFragment : ScriptableObject
{
    [Header("識別資訊")]
    public string locationID;    // 對應建築物的 ID (如: CSIE_Bldg)
    public string category;      // 類別 (如: 歷史, 功能, 傳說)

    [Header("內容")]
    [TextArea(5, 20)]
    public string content;       // 實際要餵給 AI 的知識內容

    [Header("權重")]
    [Range(1, 10)]
    public int priority = 5;     // 優先級，重要的知識優先被檢索
}
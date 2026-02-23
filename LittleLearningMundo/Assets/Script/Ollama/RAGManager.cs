using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 負責從知識庫中檢索相關資訊
/// </summary>
public class RAGManager : MonoBehaviour
{
    [Header("知識庫設定")]
    [Tooltip("將所有建立好的 KnowledgeFragment 拖入此處")]
    public List<KnowledgeFragment> allKnowledge;

    /// <summary>
    /// 根據地點 ID 檢索相關知識
    /// </summary>
    /// <param name="locationID">建築物 ID</param>
    /// <param name="limit">回傳幾條知識</param>
    /// <returns>組合好的知識字串</returns>
    public string GetContextByLocation(string locationID, int limit = 3)
    {
        // 1. 過濾出與該地點相關的知識
        // 2. 依照優先級排序
        // 3. 取出前 N 條
        var relevantFragments = allKnowledge
            .Where(f => f.locationID == locationID)
            .OrderByDescending(f => f.priority)
            .Take(limit)
            .Select(f => f.content)
            .ToList();

        if (relevantFragments.Count == 0)
            return "目前沒有關於此地點的特定背景資料。";

        // 4. 將多條知識組合成一個 Context 字串
        return "相關背景資料：\n" + string.Join("\n---\n", relevantFragments);
    }

    /// <summary>
    /// (進階) 根據關鍵字檢索知識 (簡單的關鍵字比對)
    /// </summary>
    public string SearchKnowledge(string query, int limit = 2)
    {
        var relevantFragments = allKnowledge
            .Where(f => query.Contains(f.locationID) || f.content.Contains(query))
            .OrderByDescending(f => f.priority)
            .Take(limit)
            .Select(f => f.content)
            .ToList();

        return string.Join("\n", relevantFragments);
    }
}
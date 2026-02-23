using UnityEngine;

/// <summary>
/// 掛載在建築物門口的 Trigger 物件上。
/// 用於精確定義「導覽觸發點」，確保 NPC 在正確的位置介紹該建築。
/// </summary>
[RequireComponent(typeof(Collider))]
public class BuildingZone : MonoBehaviour
{
    [Header("地點基本資訊")]
    public string locationID;      // 唯一識別碼 (如: CSIE_Entrance)
    public string displayName;     // 顯示名稱 (如: 資工系館正門)

    [Header("AI 導覽知識庫")]
    [TextArea(5, 15)]
    public string knowledgeBase;   // 專屬於這個視角的介紹資訊

    private void Awake()
    {
        // 確保 Collider 設為 Trigger，才不會擋住玩家走路
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // 方便在 Scene 視窗辨識這些區域
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
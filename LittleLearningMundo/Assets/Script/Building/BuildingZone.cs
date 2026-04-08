using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 掛載在建築物門口的 Trigger 物件上。
/// 用於定義導覽觸發點，以及該視角下的投影點與相機位置。
/// </summary>
[RequireComponent(typeof(Collider))]
public class BuildingZone : MonoBehaviour
{
    [System.Serializable]
    public class ProjectionPoint
    {
        public Transform quad;          // 顯示照片的牆面
        public Transform cameraNode;    // 觀看照片時的特寫相機位置
    }

    [Header("地點基本資訊")]
    public string locationID;      // 唯一識別碼 (如: CSIE_Entrance)
    public string displayName;     // 顯示名稱 (如: 資工系館正門)

    [Header("AI 導覽知識庫")]
    [TextArea(5, 15)]
    public string knowledgeBase;   // 專屬於這個視角的介紹資訊

    [Header("視覺演出點位")]
    [Tooltip("抵達此處時的大遠景相機位置")]
    public Transform cinematicCameraNode;

    [Tooltip("照片投影點序列，需與 StoryData 的步驟索引對應")]
    public List<ProjectionPoint> projectionPoints = new List<ProjectionPoint>();

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

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
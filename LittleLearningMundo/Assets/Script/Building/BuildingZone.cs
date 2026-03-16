using UnityEngine;

/// <summary>
/// 定義建築現場的基礎資訊。視覺演出細節已移至 StoryData。
/// </summary>
public class BuildingZone : MonoBehaviour
{
    [Header("地點資訊")]
    public string locationID;
    public string displayName;

    [Header("AI 知識庫")]
    [TextArea(5, 15)]
    public string knowledgeBase;

    [Header("視覺演出 (僅保留大遠景)")]
    [Tooltip("導覽開始時的大遠景視角")]
    public Transform cinematicCameraNode;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        // 繪製大遠景圖示 (粉紅色)
        if (cinematicCameraNode != null)
        {
            Gizmos.color = Color.magenta;
            DrawCameraGizmo(cinematicCameraNode.position, cinematicCameraNode.rotation, "大遠景");
        }
    }

    private void DrawCameraGizmo(Vector3 pos, Quaternion rot, string label)
    {
        Gizmos.DrawSphere(pos, 0.15f);
        Gizmos.DrawRay(pos, rot * Vector3.forward * 0.8f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 0.4f, label);
#endif
    }
}
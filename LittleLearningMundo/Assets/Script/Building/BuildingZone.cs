using UnityEngine;

public class BuildingZone : MonoBehaviour
{
    [Header("地點基本資訊")]
    public string locationID;
    public string displayName;

    [Header("AI 導覽知識庫")]
    [TextArea(5, 15)]
    public string knowledgeBase;

    [Header("視覺演出錨點 (主線使用)")]
    [Tooltip("鏡頭切換時的目標位置與角度")]
    public Transform cinematicCameraNode;
    
    [Tooltip("照片投射的 Quad 物件位置")]
    public Transform projectionPlane;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // 預設隱藏投影面
        if (projectionPlane != null) projectionPlane.gameObject.SetActive(false);
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

        // 畫出相機節點的朝向
        if (cinematicCameraNode != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(cinematicCameraNode.position, 0.5f);
            Gizmos.DrawLine(cinematicCameraNode.position, cinematicCameraNode.position + cinematicCameraNode.forward * 2f);
        }
    }
}
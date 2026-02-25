using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 代表路徑網路中的一個節點。
/// </summary>
public class WaypointNode : MonoBehaviour
{
    [Header("地點識別 (僅目的地需要填寫)")]
    public string locationID; 

    [Header("鄰近連接節點")]
    public List<WaypointNode> neighbors = new List<WaypointNode>();

    // A* 演算法變數
    [HideInInspector] public float gScore;
    [HideInInspector] public float fScore;
    [HideInInspector] public WaypointNode parent;

    private void OnDrawGizmos()
    {
        // 節點本體
        Gizmos.color = string.IsNullOrEmpty(locationID) ? Color.blue : Color.red;
        Gizmos.DrawSphere(transform.position, 0.4f);

        if (neighbors == null) return;

        // 畫出連線
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.color = Color.cyan;
                DrawArrow(transform.position, neighbor.transform.position);
            }
        }
    }

    // 輔助繪製箭頭，辨識單向或雙向路徑
    private void DrawArrow(Vector3 pos, Vector3 target)
    {
        Gizmos.DrawLine(pos, target);
        Vector3 direction = (target - pos).normalized;
        if (direction == Vector3.zero) return;
        
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
        Gizmos.DrawRay(target, right * 0.5f);
        Gizmos.DrawRay(target, left * 0.5f);
    }
}
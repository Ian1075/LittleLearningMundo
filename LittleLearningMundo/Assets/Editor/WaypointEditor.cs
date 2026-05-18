using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 為 WaypointNode 提供可視化編輯功能。
/// 增加了「全場景雙向連線」與「全場景 Y 軸對齊」功能。
/// </summary>
[CustomEditor(typeof(WaypointNode))]
[CanEditMultipleObjects]
public class WaypointEditor : Editor
{
    private WaypointNode _node;
    private static float _targetY = 0f; // 靜態變數以便在切換選取時保留數值

    private void OnEnable()
    {
        _node = (WaypointNode)target;
        if (_node != null) _targetY = _node.transform.position.y;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("局部編輯工具 (針對選中節點)", EditorStyles.boldLabel);

        if (GUILayout.Button("與選中的其他節點建立雙向連線"))
        {
            ConnectSelectedNodes(true);
        }

        if (GUILayout.Button("清除選中節點的所有連線"))
        {
            Undo.RecordObject(_node, "Clear Neighbors");
            _node.neighbors.Clear();
            EditorUtility.SetDirty(_node);
        }

        GUILayout.Space(15);
        GUILayout.Label("全域路徑工具 (針對整個場景)", EditorStyles.boldLabel);
        
        // --- 雙向連線工具 ---
        GUI.color = Color.cyan;
        if (GUILayout.Button("將場景內「所有」路徑轉為雙向"))
        {
            MakeAllNodesBidirectional();
        }
        GUI.color = Color.white;

        GUILayout.Space(5);

        // --- Y 軸對齊工具 ---
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Y 軸高度統一工具", EditorStyles.miniBoldLabel);
        _targetY = EditorGUILayout.FloatField("目標 Y 高度", _targetY);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("抓取當前高度"))
        {
            _targetY = _node.transform.position.y;
        }
        
        GUI.color = new Color(1f, 0.8f, 0.4f); // 橘黃色提醒
        if (GUILayout.Button("統一全場景節點高度"))
        {
            AlignAllNodesY(_targetY);
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("操作提示：\n1. 點擊側鍵 (Mouse 3) 生成節點。\n2. 「統一全場景節點高度」會強制修改場景中所有 WaypointNode 的 Y 軸座標。", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 2)
        {
            GenerateNewNode(e.mousePosition);
            e.Use();
        }
    }

    /// <summary>
    /// 將場景中所有 WaypointNode 的高度設為指定數值
    /// </summary>
    private void AlignAllNodesY(float y)
    {
        WaypointNode[] allNodes = GameObject.FindObjectsOfType<WaypointNode>();
        if (allNodes.Length == 0) return;

        if (!EditorUtility.DisplayDialog("確認對齊高度", $"是否要將場景內所有 {allNodes.Length} 個節點的高度都設為 {y}？", "確定", "取消"))
            return;

        foreach (var node in allNodes)
        {
            Undo.RecordObject(node.transform, "Align Waypoint Y");
            Vector3 pos = node.transform.position;
            pos.y = y;
            node.transform.position = pos;
            EditorUtility.SetDirty(node);
        }

        Debug.Log($"<color=orange>[路徑編輯器] 已將全場景 {allNodes.Length} 個節點對齊至 Y = {y}。</color>");
    }

    private void MakeAllNodesBidirectional()
    {
        WaypointNode[] allNodes = GameObject.FindObjectsOfType<WaypointNode>();
        int updatedCount = 0;

        foreach (var nodeA in allNodes)
        {
            if (nodeA.neighbors == null) continue;
            List<WaypointNode> currentNeighbors = new List<WaypointNode>(nodeA.neighbors);

            foreach (var nodeB in currentNeighbors)
            {
                if (nodeB == null) continue;
                if (!nodeB.neighbors.Contains(nodeA))
                {
                    Undo.RecordObject(nodeB, "Make Bidirectional");
                    nodeB.neighbors.Add(nodeA);
                    EditorUtility.SetDirty(nodeB);
                    updatedCount++;
                }
            }
        }
        Debug.Log($"<color=cyan>[路徑編輯器] 全場景雙向化完成！補齊了 {updatedCount} 條反向路徑。</color>");
    }

    private void GenerateNewNode(Vector2 mousePos)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject newNodeObj = new GameObject("WaypointNode_" + System.DateTime.Now.Ticks % 10000);
            newNodeObj.transform.position = hit.point + Vector3.up * 0.1f;
            newNodeObj.transform.SetParent(_node.transform.parent);

            WaypointNode newNode = newNodeObj.AddComponent<WaypointNode>();
            
            Undo.RegisterCreatedObjectUndo(newNodeObj, "Create Waypoint");
            Undo.RecordObject(_node, "Connect New Node");

            if (!_node.neighbors.Contains(newNode))
                _node.neighbors.Add(newNode);

            newNode.neighbors.Add(_node);

            EditorUtility.SetDirty(_node);
            EditorUtility.SetDirty(newNode);
            
            Selection.activeGameObject = newNodeObj;
            Debug.Log($"<color=green>[路徑編輯器] 成功生成節點並建立雙向連線！</color>");
        }
    }

    private void ConnectSelectedNodes(bool bidirectional)
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length < 2) return;

        foreach (var sourceObj in selected)
        {
            WaypointNode source = sourceObj.GetComponent<WaypointNode>();
            if (source == null) continue;

            foreach (var targetObj in selected)
            {
                if (sourceObj == targetObj) continue;
                WaypointNode targetNode = targetObj.GetComponent<WaypointNode>();
                if (targetNode == null) continue;

                Undo.RecordObject(source, "Connect Nodes");
                if (!source.neighbors.Contains(targetNode))
                {
                    source.neighbors.Add(targetNode);
                    EditorUtility.SetDirty(source);
                }

                if (bidirectional)
                {
                    Undo.RecordObject(targetNode, "Connect Nodes");
                    if (!targetNode.neighbors.Contains(source))
                    {
                        targetNode.neighbors.Add(source);
                        EditorUtility.SetDirty(targetNode);
                    }
                }
            }
        }
    }
}
using UnityEngine;
using UnityEditor;

/// <summary>
/// 為 WaypointNode 提供可視化編輯功能。
/// 強化了射線偵測與錯誤回饋，解決因缺少 Collider 導致無法生成的問。
/// </summary>
[CustomEditor(typeof(WaypointNode))]
[CanEditMultipleObjects]
public class WaypointEditor : Editor
{
    private WaypointNode _node;

    private void OnEnable()
    {
        _node = (WaypointNode)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("快速編輯工具", EditorStyles.boldLabel);

        if (GUILayout.Button("與選中的其他節點建立雙向連線"))
        {
            ConnectSelectedNodes(true);
        }

        if (GUILayout.Button("清除所有鄰近連線"))
        {
            Undo.RecordObject(_node, "Clear Neighbors");
            _node.neighbors.Clear();
            EditorUtility.SetDirty(_node);
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("操作提示：\n1. 點擊側鍵 (Mouse 3) 生成節點。\n2. 若無反應，請確保道路物件有掛載 Mesh Collider。", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        int targetButton = 3; // 側後退鍵

        // 1. 強制讓側鍵不會觸發 Unity 預設行為
        if (e.button == targetButton)
        {
            if (e.type == EventType.Layout || e.type == EventType.Repaint)
            {
                HandleUtility.AddDefaultControl(controlID);
            }
        }

        // 2. 獲取滑鼠位置的射線
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);

        // 3. 視覺輔助與警告
        if (hitSomething)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(hit.point, hit.normal, 0.4f);
            Handles.Label(hit.point + Vector3.up * 0.5f, "點擊側鍵生成節點");
        }
        else
        {
            // 如果沒打中東西，在滑鼠位置顯示警告
            Handles.color = Color.red;
            Vector3 mouseInWorld = ray.GetPoint(10f); // 假設距離 10 米
            Handles.Label(mouseInWorld, "<color=red>警告：此處沒有 Collider！\n請幫道路物件添加 Mesh Collider</color>");
        }

        // 4. 處理生成邏輯
        if (e.button == targetButton && e.type == EventType.MouseDown)
        {
            if (hitSomething)
            {
                CreateNewNode(hit.point);
                e.Use();
            }
            else
            {
                Debug.LogWarning("[路徑編輯器] 生成失敗：滑鼠位置底下沒有任何帶有 Collider 的物件。請檢查道路是否有 Mesh Collider。");
            }
        }

        // 強制重繪以保持預覽圓圈流暢
        if (e.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }

    private void CreateNewNode(Vector3 spawnPos)
    {
        GameObject newNodeObj = new GameObject("Waypoint_" + System.DateTime.Now.ToString("mm_ss_fff"));
        newNodeObj.transform.position = spawnPos;
        
        if (_node.transform.parent != null)
            newNodeObj.transform.parent = _node.transform.parent;
        
        WaypointNode newNode = newNodeObj.AddComponent<WaypointNode>();
        
        Undo.RegisterCreatedObjectUndo(newNodeObj, "Create Waypoint");
        Undo.RecordObject(_node, "Connect Waypoint");
        Undo.RecordObject(newNode, "Connect Waypoint");
        
        if (!_node.neighbors.Contains(newNode)) _node.neighbors.Add(newNode);
        if (!newNode.neighbors.Contains(_node)) newNode.neighbors.Add(_node);

        EditorUtility.SetDirty(_node);
        EditorUtility.SetDirty(newNode);
        
        Selection.activeGameObject = newNodeObj;
        Debug.Log($"<color=green>[路徑編輯器] 成功生成節點並連線！</color>");
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
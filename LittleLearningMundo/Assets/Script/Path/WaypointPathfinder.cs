using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 靜態工具類，負責在節點網路中計算最短路徑。
/// </summary>
public static class WaypointPathfinder
{
    public static List<WaypointNode> FindPath(WaypointNode start, WaypointNode target)
    {
        if (start == null || target == null) return null;

        List<WaypointNode> openSet = new List<WaypointNode> { start };
        HashSet<WaypointNode> closedSet = new HashSet<WaypointNode>();

        // 初始化所有節點評分 (在場景中尋找所有節點)
        WaypointNode[] allNodes = GameObject.FindObjectsOfType<WaypointNode>();
        foreach (var n in allNodes)
        {
            n.gScore = float.MaxValue;
            n.fScore = float.MaxValue;
            n.parent = null;
        }

        start.gScore = 0;
        start.fScore = Vector3.Distance(start.transform.position, target.transform.position);

        while (openSet.Count > 0)
        {
            // 取得 openSet 中 fScore 最低的節點
            WaypointNode current = openSet.OrderBy(n => n.fScore).First();

            if (current == target) return ReconstructPath(target);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor)) continue;

                float tentativeGScore = current.gScore + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (tentativeGScore < neighbor.gScore)
                {
                    neighbor.parent = current;
                    neighbor.gScore = tentativeGScore;
                    neighbor.fScore = neighbor.gScore + Vector3.Distance(neighbor.transform.position, target.transform.position);

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }
        return null; // 找不到路徑
    }

    private static List<WaypointNode> ReconstructPath(WaypointNode target)
    {
        List<WaypointNode> path = new List<WaypointNode>();
        WaypointNode current = target;
        while (current != null)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }
}
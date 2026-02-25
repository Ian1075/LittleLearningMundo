using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class NPCNavigator : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 3.5f;
    public float rotateSpeed = 8f;
    public float stopDistance = 0.3f;

    [Header("跟隨設定")]
    public float waitDistance = 5f; // 若離玩家超過此距離，則停下等待
    public Transform playerTransform;

    private List<WaypointNode> _path = new List<WaypointNode>();
    private int _targetIndex = 0;
    private bool _isMoving = false;
    private Action _onArrival;

    private void Update()
    {
        if (!_isMoving || _path == null || _targetIndex >= _path.Count) return;

        // 檢查與玩家的距離
        if (playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer > waitDistance)
            {
                // 停下等待玩家
                return; 
            }
        }

        MoveToNextNode();
    }

    public void StartPathNavigation(List<WaypointNode> path, Action onArrival)
    {
        if (path == null || path.Count == 0) return;
        _path = path;
        _targetIndex = 0;
        _onArrival = onArrival;
        _isMoving = true;

        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void MoveToNextNode()
    {
        Vector3 targetPos = _path[_targetIndex].transform.position;
        targetPos.y = transform.position.y; // 保持 NPC 貼地

        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotateSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < stopDistance)
        {
            _targetIndex++;
            if (_targetIndex >= _path.Count)
            {
                _isMoving = false;
                _onArrival?.Invoke();
            }
        }
    }

    public void StopMoving() => _isMoving = false;

    public WaypointNode GetNearestNode()
    {
        var nodes = FindObjectsOfType<WaypointNode>();
        return nodes.OrderBy(n => Vector3.Distance(transform.position, n.transform.position)).FirstOrDefault();
    }
}
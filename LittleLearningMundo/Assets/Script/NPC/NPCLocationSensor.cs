using UnityEngine;

/// <summary>
/// 負責透過 Trigger 事件偵測 NPC 目前所在的 BuildingZone。
/// </summary>
[RequireComponent(typeof(Collider))]
public class NPCLocationSensor : MonoBehaviour
{
    [Header("當前狀態")]
    [SerializeField] private BuildingZone _currentZone;

    /// <summary>
    /// 取得目前所在地點，若不在任何區域則回傳 null
    /// </summary>
    public BuildingZone GetCurrentZone() => _currentZone;

    private void OnTriggerEnter(Collider other)
    {
        // 檢查進入的是否為導覽區域
        BuildingZone zone = other.GetComponent<BuildingZone>();
        if (zone != null)
        {
            _currentZone = zone;
            Debug.Log($"[感應器] 進入區域：{_currentZone.displayName}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 離開區域時清空紀錄
        BuildingZone zone = other.GetComponent<BuildingZone>();
        if (zone != null && _currentZone == zone)
        {
            Debug.Log($"[感應器] 離開區域：{_currentZone.displayName}");
            _currentZone = null;
        }
    }
}
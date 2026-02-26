using UnityEngine;

/// <summary>
/// 負責跟隨玩家的相機腳本。
/// 支援設定固定角度 (Rotation) 與位置偏移 (Offset)。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟隨目標")]
    public Transform player;

    [Header("偏移設定")]
    [Tooltip("相機相對於玩家的座標偏移。建議：Y=15(高), Z=-12(後)")]
    public Vector3 offset = new Vector3(0, 15, -12);

    [Tooltip("相機的固定視角角度。建議：X=50 (稍微向下俯瞰)")]
    public Vector3 rotation = new Vector3(50, 0, 0);

    [Header("平滑動態 (提升帶入感)")]
    [Tooltip("是否開啟平滑跟隨，讓鏡頭更有電影感")]
    public bool smoothFollow = true;
    [Range(1f, 20f)] public float smoothSpeed = 5f;

    private void Start()
    {
        // 遊戲開始時，如果沒指派玩家，自動抓取標籤為 Player 的物件
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 初始化位置與角度，防止遊戲啟動瞬間「瞬移」造成視覺不適
        if (player != null)
        {
            transform.position = player.position + offset;
            transform.rotation = Quaternion.Euler(rotation);
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 1. 計算目標位置
        Vector3 targetPosition = player.position + offset;

        // 2. 執行位置跟隨
        if (smoothFollow)
        {
            // 使用 Lerp 讓鏡頭平滑滑動到目標點
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
        else
        {
            transform.position = targetPosition;
        }

        // 3. 執行角度設定
        // 在跟隨模式下，強制保持我們設定的黃金角度
        transform.rotation = Quaternion.Euler(rotation);
    }
}
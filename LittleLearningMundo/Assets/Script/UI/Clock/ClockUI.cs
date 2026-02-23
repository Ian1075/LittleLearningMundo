using UnityEngine;
using TMPro;

/// <summary>
/// 負責將 TimeManager 的時間顯示在 Unity UI 上的腳本
/// </summary>
public class ClockUI : MonoBehaviour
{
    [Header("UI 組件")]
    [Tooltip("用於顯示時間的 TextMeshPro 文本組件")]
    public TextMeshProUGUI timeText;

    private void Start()
    {
        if (timeText == null)
        {
            timeText = GetComponent<TextMeshProUGUI>();
        }

        // 1. 初始化顯示 (避免等待第一分鐘變動)
        if (TimeManager.Instance != null)
        {
            UpdateTimeDisplay(TimeManager.Instance.GetHour(), TimeManager.Instance.GetMinute());

            // 2. 訂閱時間變化事件
            TimeManager.Instance.OnTimeChanged += UpdateTimeDisplay;
        }
        else
        {
            Debug.LogError("ClockUI: 找不到 TimeManager 實例！");
        }
    }

    private void OnDestroy()
    {
        // 當此 UI 被銷毀時，取消訂閱事件以防止記憶體洩漏
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged -= UpdateTimeDisplay;
        }
    }

    /// <summary>
    /// 更新 UI 文字的 callback 方法
    /// </summary>
    /// <param name="hour">小時</param>
    /// <param name="minute">分鐘</param>
    private void UpdateTimeDisplay(int hour, int minute)
    {
        if (timeText != null)
        {
            // 格式化為 HH:mm
            timeText.text = string.Format("{0:00}:{1:00}", hour, minute);
        }
    }
}
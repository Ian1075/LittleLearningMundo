using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("時間比例設定")]
    [Tooltip("現實 1 秒等於遊戲中幾分鐘")]
    public float realSecondToGameMinutes = 1f; 

    [Header("當前時間狀態")]
    [SerializeField] private int currentHour = 0;
    [SerializeField] private int currentMinute = 0;

    private float gameTimeInSeconds = 0f; 
    private int lastBroadcastMinute = -1;

    // 廣播事件：傳送小時與分鐘
    public event Action<int, int> OnTimeChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化秒數，確保與 Inspector 設定的小時分鐘同步
            SyncSecondsFromHoursMinutes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateTime();
    }

    private void UpdateTime()
    {
        // 計算這一幀增加的遊戲秒數
        float addedGameSeconds = Time.deltaTime * realSecondToGameMinutes * 60f;
        gameTimeInSeconds += addedGameSeconds;

        // 處理跨越午夜 (24小時循環)
        if (gameTimeInSeconds >= 86400f) 
        {
            gameTimeInSeconds -= 86400f;
        }

        // 更新當前的小時與分鐘 (用於 Inspector 顯示與廣播)
        currentHour = Mathf.FloorToInt(gameTimeInSeconds / 3600f);
        currentMinute = Mathf.FloorToInt((gameTimeInSeconds % 3600f) / 60f);

        // 只有在分鐘真的變動時才廣播
        if (currentMinute != lastBroadcastMinute)
        {
            lastBroadcastMinute = currentMinute;
            OnTimeChanged?.Invoke(currentHour, currentMinute);
        }
    }

    // --- 公用方法 ---

    public int GetHour() => currentHour;
    public int GetMinute() => currentMinute;

    public string GetFormattedTime()
    {
        return string.Format("{0:00}:{1:00}", currentHour, currentMinute);
    }

    // 提供手動設定時間的方法，並同步秒數
    public void SetTime(int hour, int minute)
    {
        currentHour = Mathf.Clamp(hour, 0, 23);
        currentMinute = Mathf.Clamp(minute, 0, 59);
        SyncSecondsFromHoursMinutes();
    }

    private void SyncSecondsFromHoursMinutes()
    {
        gameTimeInSeconds = (currentHour * 3600f) + (currentMinute * 60f);
        lastBroadcastMinute = currentMinute;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            SyncSecondsFromHoursMinutes();
        }
    }
}
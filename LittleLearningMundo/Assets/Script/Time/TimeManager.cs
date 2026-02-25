using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("時間比例設定")]
    public float realSecondToGameMinutes = 1f; 

    [SerializeField] private int currentHour = 10;
    [SerializeField] private int currentMinute = 0;

    private float gameTimeInSeconds = 0f; 
    private int lastBroadcastMinute = -1;

    public event Action<int, int> OnTimeChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SyncSecondsFromHoursMinutes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        float addedGameSeconds = Time.deltaTime * realSecondToGameMinutes * 60f;
        gameTimeInSeconds += addedGameSeconds;

        if (gameTimeInSeconds >= 86400f) gameTimeInSeconds -= 86400f;

        currentHour = Mathf.FloorToInt(gameTimeInSeconds / 3600f);
        currentMinute = Mathf.FloorToInt((gameTimeInSeconds % 3600f) / 60f);

        if (currentMinute != lastBroadcastMinute)
        {
            lastBroadcastMinute = currentMinute;
            OnTimeChanged?.Invoke(currentHour, currentMinute);
        }
    }

    // --- 新增公用方法解決 ClockUI 報錯 ---
    public int GetHour() => currentHour;
    public int GetMinute() => currentMinute;

    public string GetFormattedTime() => $"{currentHour:00}:{currentMinute:00}";

    private void SyncSecondsFromHoursMinutes()
    {
        gameTimeInSeconds = (currentHour * 3600f) + (currentMinute * 60f);
        lastBroadcastMinute = currentMinute;
    }
}
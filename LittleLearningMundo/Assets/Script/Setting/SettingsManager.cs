using UnityEngine;

/// <summary>
/// 負責儲存與讀取玩家的遊戲設定 (如 API 網址、模型名稱)，並自動套用給系統。
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("預設設定 (初次遊玩時使用)")]
    public string defaultApiUrl = "http://localhost:11434/api/chat";
    public string defaultModelName = "llama3.1:8b";

    // 目前的實際設定值
    public string CurrentApiUrl { get; private set; }
    public string CurrentModelName { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 遊戲一開始，就把讀取到的設定強行塞給 Ollama 系統
        ApplyToServices();
    }

    /// <summary>
    /// 從本機儲存空間讀取設定
    /// </summary>
    public void LoadSettings()
    {
        CurrentApiUrl = PlayerPrefs.GetString("OllamaApiUrl", defaultApiUrl);
        CurrentModelName = PlayerPrefs.GetString("OllamaModelName", defaultModelName);
    }

    /// <summary>
    /// 儲存設定並立即生效
    /// </summary>
    public void SaveSettings(string url, string model)
    {
        CurrentApiUrl = url;
        CurrentModelName = model;

        PlayerPrefs.SetString("OllamaApiUrl", url);
        PlayerPrefs.SetString("OllamaModelName", model);
        PlayerPrefs.Save(); // 寫入硬碟

        ApplyToServices();
    }

    /// <summary>
    /// 將設定值同步到實際運作的腳本上
    /// </summary>
    private void ApplyToServices()
    {
        OllamaApiClient client = FindObjectOfType<OllamaApiClient>();
        if (client != null) client.baseHostUrl = CurrentApiUrl;

        OllamaService service = FindObjectOfType<OllamaService>();
        if (service != null) service.modelName = CurrentModelName;
    }
}
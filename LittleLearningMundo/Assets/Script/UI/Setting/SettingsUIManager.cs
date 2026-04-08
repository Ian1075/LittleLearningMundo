using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 負責控制設定面板的開關與 UI 互動。
/// </summary>
public class SettingsUIManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; } = false;

    [Header("面板開關")]
    public GameObject settingsPanel;
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("UI 綁定 (如果這裡是 None 就抓不到字！)")]
    public TMP_InputField urlInputField;
    public TMP_InputField modelInputField;
    public Button saveButton;
    public Button closeButton;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (saveButton != null) saveButton.onClick.AddListener(OnSaveButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        // 【新增】綁定輸入框結束編輯 (點擊其他地方離開) 時的事件
        if (urlInputField != null) urlInputField.onEndEdit.AddListener(OnUrlEndEdit);
        if (modelInputField != null) modelInputField.onEndEdit.AddListener(OnModelEndEdit);

        UpdateUIFromSettings();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (settingsPanel.activeSelf) ClosePanel();
            else OpenPanel();
        }
    }

    public void OpenPanel()
    {
        IsPaused = true;
        Time.timeScale = 0f; 

        UpdateUIFromSettings(); 
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        IsPaused = false;
        Time.timeScale = 1f; 

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void UpdateUIFromSettings()
    {
        if (SettingsManager.Instance == null) return;

        string urlToDisplay = SettingsManager.Instance.CurrentApiUrl;
        string modelToDisplay = SettingsManager.Instance.CurrentModelName;

        if (string.IsNullOrEmpty(urlToDisplay) || urlToDisplay == "\u200B") urlToDisplay = SettingsManager.Instance.defaultApiUrl;
        if (string.IsNullOrEmpty(modelToDisplay) || modelToDisplay == "\u200B") modelToDisplay = SettingsManager.Instance.defaultModelName;

        if (urlInputField != null) urlInputField.text = urlToDisplay;
        if (modelInputField != null) modelInputField.text = modelToDisplay;
    }

    // 【新增】當離開 URL 輸入框時立刻檢查
    private void OnUrlEndEdit(string text)
    {
        if (SettingsManager.Instance == null || urlInputField == null) return;
        string cleanText = text.Replace("\u200B", "").Trim();
        if (string.IsNullOrEmpty(cleanText))
        {
            urlInputField.text = SettingsManager.Instance.defaultApiUrl;
        }
    }

    // 【新增】當離開 Model 輸入框時立刻檢查
    private void OnModelEndEdit(string text)
    {
        if (SettingsManager.Instance == null || modelInputField == null) return;
        string cleanText = text.Replace("\u200B", "").Trim();
        if (string.IsNullOrEmpty(cleanText))
        {
            modelInputField.text = SettingsManager.Instance.defaultModelName;
        }
    }

    private void OnSaveButtonClicked()
    {
        if (SettingsManager.Instance == null) return;

        // 【終極防呆】如果你忘記綁定 UI，直接在 Console 噴出紅色錯誤警告你！
        if (urlInputField == null || modelInputField == null)
        {
            Debug.LogError("<color=red><b>[嚴重錯誤]</b></color> 你的 SettingsUIManager 沒有綁定 InputField！請檢查 Inspector，把輸入框拖進去！");
            return;
        }

        string oldUrl = SettingsManager.Instance.CurrentApiUrl;
        string oldModel = SettingsManager.Instance.CurrentModelName;

        // 讀取玩家輸入的文字 (清除隱形字元與空白)
        string url = urlInputField.text.Replace("\u200B", "").Trim();
        string model = modelInputField.text.Replace("\u200B", "").Trim();

        // 防呆機制：如果是空的，自動使用預設值
        if (string.IsNullOrEmpty(url)) url = SettingsManager.Instance.defaultApiUrl;
        if (string.IsNullOrEmpty(model)) model = SettingsManager.Instance.defaultModelName;

        urlInputField.text = url;
        modelInputField.text = model;

        Debug.Log($"<color=yellow>[設定變更]</color>\nURL: {oldUrl} ➔ {url}\nModel: {oldModel} ➔ {model}");

        // 儲存設定
        SettingsManager.Instance.SaveSettings(url, model);
        
        ClosePanel();
    }
}
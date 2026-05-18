using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 負責控制設定面板的開關、UI 互動與安全離開遊戲功能。
/// </summary>
public class SettingsUIManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; } = false;

    [Header("面板開關")]
    public GameObject settingsPanel;
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("UI 綁定 (請務必將對應 UI 物件拖入 Inspector)")]
    public TMP_InputField urlInputField;
    public TMP_InputField modelInputField;
    public Button saveButton;
    public Button closeButton;
    public Button exitButton; // 【新增】離開遊戲（安全存檔退出）按鈕

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (saveButton != null) saveButton.onClick.AddListener(OnSaveButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        
        // 【新增】綁定離開按鈕事件
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);

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

    private void OnUrlEndEdit(string text)
    {
        if (SettingsManager.Instance == null || urlInputField == null) return;
        string cleanText = text.Replace("\u200B", "").Trim();
        if (string.IsNullOrEmpty(cleanText))
        {
            urlInputField.text = SettingsManager.Instance.defaultApiUrl;
        }
    }

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

        if (urlInputField == null || modelInputField == null)
        {
            Debug.LogError("<color=red><b>[嚴重錯誤]</b></color> 你的 SettingsUIManager 沒有綁定 InputField！請檢查 Inspector，把輸入框拖進去！");
            return;
        }

        string oldUrl = SettingsManager.Instance.CurrentApiUrl;
        string oldModel = SettingsManager.Instance.CurrentModelName;

        string url = urlInputField.text.Replace("\u200B", "").Trim();
        string model = modelInputField.text.Replace("\u200B", "").Trim();

        if (string.IsNullOrEmpty(url)) url = SettingsManager.Instance.defaultApiUrl;
        if (string.IsNullOrEmpty(model)) model = SettingsManager.Instance.defaultModelName;

        urlInputField.text = url;
        modelInputField.text = model;

        Debug.Log($"<color=yellow>[設定變更]</color>\nURL: {oldUrl} ➔ {url}\nModel: {oldModel} ➔ {model}");

        SettingsManager.Instance.SaveSettings(url, model);
        
        ClosePanel();
    }

    /// <summary>
    /// 【新增】點擊離開按鈕時觸發，先自動存檔再安全退出
    /// </summary>
    private void OnExitButtonClicked()
    {
        Debug.Log("<color=red>[系統] 偵測到離開請求，正在執行安全存檔...</color>");

        // 1. 自動存檔（防止玩家忘記存檔直接退出，對話紀錄或筆記不見）
        if (AccountManager.Instance != null && AccountManager.Instance.CurrentPlayer != null)
        {
            AccountManager.Instance.SaveProgress();
            Debug.Log("<color=green>[系統] 玩家進度成功儲存到本機 JSON 檔案中！</color>");
        }

        // 2. 恢復時間流速（防止退出後影響 Editor 狀態或生命週期）
        Time.timeScale = 1f;

        // 3. 執行退出平台判定
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
        // 在 WebGL 網頁版，由於無法強制關閉瀏覽器視窗，我們讓玩家「退回登入介面」代表離開
        Debug.LogWarning("[系統] WebGL 網頁版不支援調用 Application.Quit()。改為登出並退回登入畫面。");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        #else
        Application.Quit(); // PC 執行檔 (.exe) 直接關閉程式
        #endif
    }
}
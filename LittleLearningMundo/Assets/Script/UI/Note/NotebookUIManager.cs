using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotebookUIManager : MonoBehaviour
{
    [Header("面板開關")]
    public GameObject notebookPanel;
    public KeyCode toggleKey = KeyCode.Tab; // 按 Tab 鍵開關筆記本

    [Header("狀態檢查引用")]
    [Tooltip("用於檢查是否正在對話中，以禁止開啟筆記本")]
    public ChatUIManager chatUI; // 【新增】引入對話介面來判斷狀態

    [Header("【頂部】總進度條設定")]
    public Slider progressBar;               // 【新增】進度條 UI
    public TextMeshProUGUI progressText;     // 【新增】進度文字 (例: 1/3)

    [Header("【左側】標題清單設定")]
    public Transform leftContentParent; 
    public GameObject noteTabPrefab;    

    [Header("【右側】內容顯示設定")]
    public GameObject rightContentPanel; 
    public TextMeshProUGUI rightTitleText;   
    public TextMeshProUGUI rightContentText; 

    private void Start()
    {
        if (notebookPanel != null) notebookPanel.SetActive(false);
        if (rightContentPanel != null) rightContentPanel.SetActive(false); 

        // 訂閱解鎖事件
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnNoteUnlocked += HandleNewNote;
            
            // 載入已有的筆記
            foreach (var story in ProgressManager.Instance.completedStories)
            {
                AddNewNoteTab(story);
            }
            // 【新增】初始化時更新一次進度條
            UpdateProgressUI(); 
        }
    }

    private void OnDestroy()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnNoteUnlocked -= HandleNewNote;
        }
    }

    private void Update()
    {
        // 1. 【最高權限攔截】如果設定選單開著，完全禁止操作筆記本
        if (SettingsUIManager.IsPaused) return;

        if (Input.GetKeyDown(toggleKey) && notebookPanel != null)
        {
            // 2. 【狀態規則】如果是準備「打開」筆記本，先檢查是否在對話中
            if (!notebookPanel.activeSelf)
            {
                if (chatUI != null && chatUI.background != null && chatUI.background.activeSelf)
                {
                    Debug.Log("正在對話中，無法開啟筆記本！");
                    return; // 攔截打開動作
                }
            }

            // 執行開關
            notebookPanel.SetActive(!notebookPanel.activeSelf);
        }
    }

    private void HandleNewNote(StoryData newStory)
    {
        AddNewNoteTab(newStory);
        // 【新增】解鎖新筆記時，連動更新進度條
        UpdateProgressUI(); 
    }

    private void AddNewNoteTab(StoryData newStory)
    {
        if (noteTabPrefab == null || leftContentParent == null) return;

        GameObject newTabObj = Instantiate(noteTabPrefab, leftContentParent);
        NoteItemUI noteUI = newTabObj.GetComponent<NoteItemUI>();
        if (noteUI != null)
        {
            noteUI.Setup(newStory, this);
        }
    }

    public void ShowNoteContent(StoryData storyData)
    {
        if (storyData == null) return;

        if (rightContentPanel != null) rightContentPanel.SetActive(true);

        if (rightTitleText != null) rightTitleText.text = storyData.noteTitle;
        if (rightContentText != null) rightContentText.text = storyData.noteContent;
    }

    /// <summary>
    /// 【新增】更新進度條視覺
    /// </summary>
    private void UpdateProgressUI()
    {
        if (ProgressManager.Instance == null) return;

        int current = ProgressManager.Instance.completedStories.Count;
        int total = ProgressManager.Instance.totalRoutesCount;

        if (progressBar != null)
        {
            // 將數值轉為 0.0 ~ 1.0 之間
            progressBar.value = ProgressManager.Instance.GetProgressPercentage();
        }

        if (progressText != null)
        {
            progressText.text = $"導覽收集進度：{current} / {total}";
        }
    }

    public void CloseNotebook()
    {
        if (notebookPanel != null) notebookPanel.SetActive(false);
    }
}
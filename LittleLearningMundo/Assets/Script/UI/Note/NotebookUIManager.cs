using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 負責管理「左側標題清單」與「右側內容顯示」的雙欄式筆記本總管。
/// </summary>
public class NotebookUIManager : MonoBehaviour
{
    [Header("面板開關")]
    public GameObject notebookPanel;
    public KeyCode toggleKey = KeyCode.Tab; // 按 Tab 鍵開關筆記本

    [Header("【左側】標題清單設定")]
    public Transform leftContentParent; // 左側 Scroll View 裡的 Content
    public GameObject noteTabPrefab;    // 左側生成的按鈕 Prefab (需掛載 NoteItemUI)

    [Header("【右側】內容顯示設定")]
    public GameObject rightContentPanel; // 右側整個內容區塊 (未選擇時可隱藏)
    public TextMeshProUGUI rightTitleText;   // 右側上方的標題
    public TextMeshProUGUI rightContentText; // 右側中間的內文 (可能放在右側的 Scroll View 裡)

    private void Start()
    {
        // 確保剛開始是關閉的
        if (notebookPanel != null) notebookPanel.SetActive(false);
        if (rightContentPanel != null) rightContentPanel.SetActive(false); // 一開始右邊是空的

        // 訂閱解鎖事件
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnNoteUnlocked += AddNewNoteTab;
            
            // 如果一開始就有已完成的進度，先載入出來
            foreach (var story in ProgressManager.Instance.completedStories)
            {
                AddNewNoteTab(story);
            }
        }
    }

    private void OnDestroy()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnNoteUnlocked -= AddNewNoteTab;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) && notebookPanel != null)
        {
            notebookPanel.SetActive(!notebookPanel.activeSelf);
        }
    }

    /// <summary>
    /// 生成一個新的「標題按鈕」到左側清單中
    /// </summary>
    private void AddNewNoteTab(StoryData newStory)
    {
        if (noteTabPrefab == null || leftContentParent == null) return;

        // 實例化 Prefab 到左側 Content 底下
        GameObject newTabObj = Instantiate(noteTabPrefab, leftContentParent);
        
        // 抓取 NoteItemUI 腳本並塞入資料與自身的 Reference
        NoteItemUI noteUI = newTabObj.GetComponent<NoteItemUI>();
        if (noteUI != null)
        {
            noteUI.Setup(newStory, this);
        }
    }

    /// <summary>
    /// 提供給 NoteItemUI (左側按鈕) 點擊時呼叫，用來更新右側畫面
    /// </summary>
    public void ShowNoteContent(StoryData storyData)
    {
        if (storyData == null) return;

        // 打開右側面板
        if (rightContentPanel != null) rightContentPanel.SetActive(true);

        // 填入資料
        if (rightTitleText != null) rightTitleText.text = storyData.noteTitle;
        if (rightContentText != null) rightContentText.text = storyData.noteContent;
    }

    public void CloseNotebook()
    {
        if (notebookPanel != null) notebookPanel.SetActive(false);
    }
}
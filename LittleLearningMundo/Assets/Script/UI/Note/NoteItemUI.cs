using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 掛載在左側清單的「單個筆記標題按鈕 Prefab」上的腳本
/// </summary>
public class NoteItemUI : MonoBehaviour
{
    [Header("UI 綁定")]
    public TextMeshProUGUI titleText; // 顯示在左側的標題
    public Button tabButton;          // 這個項目本身的按鈕組件

    private StoryData _myStoryData;
    private NotebookUIManager _manager;

    /// <summary>
    /// 初始化按鈕資料與點擊事件
    /// </summary>
    public void Setup(StoryData data, NotebookUIManager manager)
    {
        _myStoryData = data;
        _manager = manager;

        if (titleText != null) titleText.text = data.noteTitle;

        if (tabButton == null) tabButton = GetComponent<Button>();
        
        // 綁定點擊事件
        if (tabButton != null)
        {
            tabButton.onClick.RemoveAllListeners();
            tabButton.onClick.AddListener(OnTabClicked);
        }
    }

    /// <summary>
    /// 當玩家點擊左側的這個標題時
    /// </summary>
    private void OnTabClicked()
    {
        if (_manager != null && _myStoryData != null)
        {
            // 通知管理器：把我的內容顯示到右邊去！
            _manager.ShowNoteContent(_myStoryData);
        }
    }
}
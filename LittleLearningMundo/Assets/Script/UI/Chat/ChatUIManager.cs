using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// 負責對話介面顯示與輸入邏輯，支援自動文本分段、打字機特效與鍵盤操作
/// </summary>
public class ChatUIManager : MonoBehaviour
{
    [Header("UI 元件引用")]
    public GameObject background;
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI responseTMP;
    public GameObject inputArea; 
    public TMP_InputField inputField;
    public Button enterBtn;
    public GameObject nextIcon; // 可選：提示玩家「點擊繼續」的小圖示

    [Header("打字機特效設定")]
    public int scrambleCount = 3;
    public float scrambleSpeed = 0.015f;
    public float charDelay = 0.02f;

    [Header("分段設定")]
    [Tooltip("如果句子長度超過此數值且沒有標點符號，將強制切分")]
    public int maxCharsPerSegment = 50;

    private string _glitchChars = "!@#$%^&*()_+-=[]{}|;':,.<>/?0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    // 狀態管理
    private Action<string> _onTextSubmitted;
    private Action _onEscPressed;
    private List<string> _pendingSegments = new List<string>();
    private int _currentSegmentIndex = 0;
    private bool _isTyping = false;
    private string _currentFullText; // 當前分段的完整內容
    private bool _isMultiSegmentFlow = false;

    private void Awake()
    {
        if (enterBtn != null) 
            enterBtn.onClick.AddListener(HandleSubmit);
        
        if (nextIcon) nextIcon.SetActive(false);
        CloseChat(); 
    }

    private void Update()
    {
        if (background == null || !background.activeSelf) return;

        // 1. 全域 Esc 監聽
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _onEscPressed?.Invoke();
            CloseChat();
            return;
        }

        // 2. 處理分段點擊繼續 (滑鼠左鍵或 Enter/Space)
        if (_isMultiSegmentFlow && !IsInputFieldActive())
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (_isTyping)
                {
                    // 如果正在打字，則瞬間顯示該段全文
                    CompleteCurrentSegment();
                }
                else
                {
                    // 如果打字完成，顯示下一段
                    DisplayNextSegment();
                }
            }
        }
        else if (IsInputFieldActive())
        {
            // 3. 輸入框開啟時的 Enter 提交
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HandleSubmit();
            }
        }
    }

    public void CloseChat()
    {
        StopAllCoroutines();
        _isTyping = false;
        _isMultiSegmentFlow = false;
        
        if (background) background.SetActive(false);
        if (nameTMP) nameTMP.gameObject.SetActive(false);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        
        _onTextSubmitted = null;
        _onEscPressed = null;
    }

    /// <summary>
    /// 核心方法：顯示 NPC 回應，會自動進行分段處理
    /// </summary>
    public void ShowNPCResponse(string npcName, string content, Action onCloseCallback = null)
    {
        background.SetActive(true);
        nameTMP.gameObject.SetActive(true);
        responseTMP.gameObject.SetActive(true);
        inputArea.SetActive(false);
        enterBtn.gameObject.SetActive(false);
        
        nameTMP.text = npcName;
        _onEscPressed = onCloseCallback;

        // 進行文本分段
        _pendingSegments = SplitText(content);
        _currentSegmentIndex = 0;
        _isMultiSegmentFlow = true;

        DisplayNextSegment();
    }

    private void DisplayNextSegment()
    {
        if (_currentSegmentIndex < _pendingSegments.Count)
        {
            string segment = _pendingSegments[_currentSegmentIndex];
            _currentSegmentIndex++;
            
            if (nextIcon) nextIcon.SetActive(false);
            
            StopAllCoroutines();
            StartCoroutine(TypeWithGlitch(segment));
        }
        else
        {
            // 所有分段顯示完畢
            _isMultiSegmentFlow = false;
            if (nextIcon) nextIcon.SetActive(false);
            
            // 觸發 NPCController 切換到輸入模式 (若 NPCController 邏輯上有此需求)
            // 這裡不自動開啟 OpenPlayerInput，交由 NPCController 狀態機決定何時呼叫
        }
    }

    private void CompleteCurrentSegment()
    {
        StopAllCoroutines();
        _isTyping = false;
        responseTMP.text = _currentFullText;
        if (nextIcon) nextIcon.SetActive(true);
    }

    public void OpenPlayerInput(Action<string> onCallback)
    {
        _onTextSubmitted = onCallback;
        
        responseTMP.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        
        inputArea.SetActive(true);
        enterBtn.gameObject.SetActive(true);
        
        inputField.text = "";
        inputField.ActivateInputField(); 
    }

    public void HandleSubmit()
    {
        if (string.IsNullOrEmpty(inputField.text) || !IsInputFieldActive()) return;
        
        string text = inputField.text;
        _onTextSubmitted?.Invoke(text);
        
        inputArea.SetActive(false);
        enterBtn.gameObject.SetActive(false);
        responseTMP.gameObject.SetActive(true);
        responseTMP.text = "解碼訊息中...";
    }

    public bool IsInputFieldActive() => inputArea != null && inputArea.activeSelf;

    /// <summary>
    /// 使用正則表達式根據標點符號切分文本，確保句子不會過長
    /// </summary>
    private List<string> SplitText(string text)
    {
        List<string> segments = new List<string>();
        // 根據常見的中英文標點與換行進行切分
        string pattern = @"(?<=[。！？\n；])"; 
        string[] rawParts = Regex.Split(text, pattern);

        foreach (var part in rawParts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // 如果單句還是太長，進行強制二段切分 (避免單句溢出)
            if (trimmed.Length > maxCharsPerSegment)
            {
                for (int i = 0; i < trimmed.Length; i += maxCharsPerSegment)
                {
                    int length = Math.Min(maxCharsPerSegment, trimmed.Length - i);
                    segments.Add(trimmed.Substring(i, length));
                }
            }
            else
            {
                segments.Add(trimmed);
            }
        }
        return segments;
    }

    private IEnumerator TypeWithGlitch(string targetText)
    {
        _isTyping = true;
        _currentFullText = targetText;
        responseTMP.text = "";
        string currentText = "";

        for (int i = 0; i < targetText.Length; i++)
        {
            char finalChar = targetText[i];

            if (char.IsWhiteSpace(finalChar))
            {
                currentText += finalChar;
                responseTMP.text = currentText;
                continue;
            }

            for (int j = 0; j < scrambleCount; j++)
            {
                responseTMP.text = currentText + _glitchChars[UnityEngine.Random.Range(0, _glitchChars.Length)];
                yield return new WaitForSeconds(scrambleSpeed);
            }

            currentText += finalChar;
            responseTMP.text = currentText;

            yield return new WaitForSeconds(charDelay);
        }

        _isTyping = false;
        if (nextIcon) nextIcon.SetActive(true);
    }
}
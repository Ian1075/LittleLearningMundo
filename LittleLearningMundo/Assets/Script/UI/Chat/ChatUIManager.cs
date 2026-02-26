using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// 處理對話 UI。確保在不同對話狀態下，UI 元件的 Active 狀態切換正確。
/// </summary>
public class ChatUIManager : MonoBehaviour
{
    [Header("UI 元件")]
    public GameObject background;
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI responseTMP;
    public GameObject inputArea; 
    public TMP_InputField inputField;
    public Button enterBtn;
    public GameObject nextIcon;

    [Header("打字機特效")]
    public int scrambleCount = 3;
    public float scrambleSpeed = 0.015f;
    public float charDelay = 0.02f;
    public int maxCharsPerSegment = 80;

    private string _glitchChars = "!@#$%^&*()_+-=[]{}|;':,.<>/?0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    // 狀態管理回呼
    private Action _onEscPressed;
    private Action _onFinishedAll; 
    private Action<string> _onInputSubmitted; 
    
    private bool _isTyping = false;
    private bool _isMultiSegmentFlow = false;
    private bool _isStreamingActive = false;
    private string _fullStreamBuffer = ""; 
    private string _currentSegmentShowing = "";
    private int _streamCharPtr = 0;

    private void Awake()
    {
        if (enterBtn != null) 
            enterBtn.onClick.AddListener(HandleSubmit);
            
        CloseChat(); 
    }

    private void Update()
    {
        // 隨時檢查 Canvas 攝影機，防止因為視角切換導致 UI 渲染在錯誤的相機平面
        UpdateCanvasCamera();

        if (background == null || !background.activeSelf) return;

        // 1. Esc 關閉
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _onEscPressed?.Invoke();
            CloseChat();
            return;
        }

        // 2. 處理 E 鍵或點擊 (NPC 說話模式下)
        if (_isMultiSegmentFlow && !IsInputFieldActive())
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                if (_isTyping)
                {
                    StopAllCoroutines();
                    CompleteCurrentSegmentVisual();
                }
                else if (nextIcon != null && nextIcon.activeSelf)
                {
                    nextIcon.SetActive(false);
                    responseTMP.text = "";
                    _currentSegmentShowing = "";
                    
                    if (_isStreamingActive || _streamCharPtr < _fullStreamBuffer.Length)
                        StartCoroutine(StreamingTypewriterCore());
                    else
                    {
                        _isMultiSegmentFlow = false;
                        _onFinishedAll?.Invoke();
                    }
                }
            }
        }
        else if (IsInputFieldActive())
        {
            // 3. 輸入模式：按 Enter 送出
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HandleSubmit();
            }
        }
    }

    /// <summary>
    /// 確保 UI 始終對齊目前的主相機
    /// </summary>
    private void UpdateCanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (canvas.worldCamera != Camera.main)
            {
                canvas.worldCamera = Camera.main;
            }
        }
    }

    /// <summary>
    /// 關閉所有 UI 元件
    /// </summary>
    public void CloseChat()
    {
        StopAllCoroutines();
        _isTyping = _isMultiSegmentFlow = _isStreamingActive = false;
        
        if (background) background.SetActive(false);
        if (nameTMP) nameTMP.gameObject.SetActive(false);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (inputArea) inputArea.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
    }

    /// <summary>
    /// 準備開始串流顯示 NPC 回話
    /// </summary>
    public void PrepareStreamingResponse(string npcName)
    {
        _fullStreamBuffer = ""; _streamCharPtr = 0; _currentSegmentShowing = "";
        _isStreamingActive = _isMultiSegmentFlow = true;

        // 【狀態設定：說話模式】
        if (background) background.SetActive(true);
        if (nameTMP) { 
            nameTMP.gameObject.SetActive(true); 
            nameTMP.text = npcName; 
        }
        if (responseTMP) { 
            responseTMP.gameObject.SetActive(true); 
            responseTMP.text = "..."; 
        }
        
        // 確保輸入相關隱藏
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(StreamingTypewriterCore());
    }

    public void UpdateStreamingText(string fullText)
    {
        _fullStreamBuffer = Regex.Replace(fullText, @"[\r\n]+", "").TrimStart();
    }

    private IEnumerator StreamingTypewriterCore()
    {
        _isTyping = true;
        int currentSegmentCount = 0;
        while (_isStreamingActive || _streamCharPtr < _fullStreamBuffer.Length)
        {
            if (_streamCharPtr < _fullStreamBuffer.Length)
            {
                char c = _fullStreamBuffer[_streamCharPtr];
                bool isSegmentBreak = "。！？；".Contains(c.ToString());
                if ((isSegmentBreak || currentSegmentCount >= maxCharsPerSegment) && _streamCharPtr < _fullStreamBuffer.Length - 1)
                {
                    yield return StartCoroutine(TypeSingleCharWithGlitch(c));
                    _streamCharPtr++; break; 
                }
                yield return StartCoroutine(TypeSingleCharWithGlitch(c));
                _streamCharPtr++; currentSegmentCount++;
            }
            else yield return null;
        }
        _isTyping = false;
        
        // 打字結束，顯示繼續圖示
        if (nextIcon) nextIcon.SetActive(true);
    }

    private IEnumerator TypeSingleCharWithGlitch(char c)
    {
        if (char.IsWhiteSpace(c)) { 
            _currentSegmentShowing += c; 
            responseTMP.text = _currentSegmentShowing; 
            yield break; 
        }
        for (int j = 0; j < scrambleCount; j++)
        {
            responseTMP.text = _currentSegmentShowing + _glitchChars[UnityEngine.Random.Range(0, _glitchChars.Length)];
            yield return new WaitForSeconds(scrambleSpeed);
        }
        _currentSegmentShowing += c;
        responseTMP.text = _currentSegmentShowing;
        yield return new WaitForSeconds(charDelay);
    }

    private void CompleteCurrentSegmentVisual()
    {
        _isTyping = false;
        while (_streamCharPtr < _fullStreamBuffer.Length)
        {
            char c = _fullStreamBuffer[_streamCharPtr];
            _currentSegmentShowing += c; _streamCharPtr++;
            if ("。！？；".Contains(c.ToString()) || _currentSegmentShowing.Length >= maxCharsPerSegment) break;
        }
        responseTMP.text = _currentSegmentShowing;
        if (nextIcon) nextIcon.SetActive(true);
    }

    public void FinishStreamingResponse(Action onEsc, Action onAllFinished)
    {
        _onEscPressed = onEsc; _onFinishedAll = onAllFinished;
        _isStreamingActive = false; 
    }

    /// <summary>
    /// 開啟玩家輸入框
    /// </summary>
    public void OpenPlayerInput(Action<string> onCallback)
    {
        _isMultiSegmentFlow = false;
        _onInputSubmitted = onCallback;

        // 【狀態設定：輸入模式】
        if (background) background.SetActive(true);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        
        if (inputArea) inputArea.SetActive(true);
        if (enterBtn) enterBtn.gameObject.SetActive(true);
        
        if (inputField != null)
        {
            inputField.text = ""; 
            inputField.ActivateInputField();
        }
    }

    public void HandleSubmit()
    {
        if (!IsInputFieldActive() || string.IsNullOrEmpty(inputField.text)) return;
        
        string text = inputField.text;

        // 送出後回到等待顯示模式
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        
        if (responseTMP)
        {
            responseTMP.gameObject.SetActive(true);
            responseTMP.text = "...";
        }
        
        _onInputSubmitted?.Invoke(text);
    }

    public bool IsInputFieldActive() => inputArea != null && inputArea.activeSelf;

    public void ShowNPCResponse(string npcName, string content, Action onEsc, Action onAllFinished)
    {
        PrepareStreamingResponse(npcName);
        UpdateStreamingText(content);
        FinishStreamingResponse(onEsc, onAllFinished);
    }
}
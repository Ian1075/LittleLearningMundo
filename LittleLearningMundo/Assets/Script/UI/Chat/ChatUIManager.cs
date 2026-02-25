using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

/// <summary>
/// 負責對話介面顯示、分段與輸入。
/// 已優化：解決回覆開頭出現空行的問題，並優化分段與打字機連動。
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
    public GameObject nextIcon;

    [Header("打字機特效設定")]
    public int scrambleCount = 2;
    public float scrambleSpeed = 0.01f;
    public float charDelay = 0.02f;

    [Header("分段設定")]
    public int maxCharsPerSegment = 80;

    private string _glitchChars = "!@#$%^&*()_+-=[]{}|;':,.<>/?0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    private Action _onEscPressed;
    private Action _onFinishedAll; 
    private Action<string> _onInputSubmitted; 
    
    private bool _isTyping = false;
    private bool _isMultiSegmentFlow = false;
    private bool _isStreamingActive = false;

    private string _fullStreamBuffer = ""; 
    private int _streamCharPtr = 0;       
    private string _currentSegmentShowing = ""; 

    private void Awake()
    {
        if (enterBtn != null) 
            enterBtn.onClick.AddListener(HandleSubmit);
            
        CloseChat(); 
    }

    private void Update()
    {
        if (background == null || !background.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _onEscPressed?.Invoke();
            CloseChat();
            return;
        }

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
                    {
                        StartCoroutine(StreamingTypewriterCore());
                    }
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
        _isStreamingActive = false;
        _fullStreamBuffer = "";
        _streamCharPtr = 0;
        
        if (background) background.SetActive(false);
        if (nameTMP) nameTMP.gameObject.SetActive(false);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
    }

    // ==================== 串流回覆 (Streaming) ====================

    public void PrepareStreamingResponse(string npcName)
    {
        _fullStreamBuffer = "";
        _streamCharPtr = 0;
        _currentSegmentShowing = "";
        _isStreamingActive = true;
        _isMultiSegmentFlow = true; 

        background.SetActive(true);
        if (nameTMP) { nameTMP.gameObject.SetActive(true); nameTMP.text = npcName; }
        responseTMP.gameObject.SetActive(true);
        responseTMP.text = "";
        
        inputArea.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(StreamingTypewriterCore());
    }

    public void UpdateStreamingText(string fullText)
    {
        // 核心修正：移除全文開頭的所有換行符號與空白，確保 NPC 不會從第二行才開始說話
        _fullStreamBuffer = fullText.TrimStart('\r', '\n', ' ');
    }

    public void FinishStreamingResponse(Action onEsc, Action onAllFinished)
    {
        _onEscPressed = onEsc;
        _onFinishedAll = onAllFinished;
        _isStreamingActive = false; 
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
                
                // 檢查是否需要分段
                bool isPunctuation = "。！？\n；".Contains(c.ToString());
                bool isOverLength = currentSegmentCount >= maxCharsPerSegment;

                if ((isPunctuation || isOverLength) && _streamCharPtr < _fullStreamBuffer.Length - 1)
                {
                    yield return StartCoroutine(TypeSingleCharWithGlitch(c));
                    _streamCharPtr++;
                    break; 
                }

                yield return StartCoroutine(TypeSingleCharWithGlitch(c));
                _streamCharPtr++;
                currentSegmentCount++;
            }
            else
            {
                yield return null;
            }
        }

        _isTyping = false;
        if (nextIcon) nextIcon.SetActive(true);
    }

    private IEnumerator TypeSingleCharWithGlitch(char c)
    {
        if (char.IsWhiteSpace(c))
        {
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
            _currentSegmentShowing += c;
            _streamCharPtr++;
            if ("。！？\n；".Contains(c.ToString()) || _currentSegmentShowing.Length % maxCharsPerSegment == 0)
                break;
        }
        responseTMP.text = _currentSegmentShowing;
        if (nextIcon) nextIcon.SetActive(true);
    }

    // ==================== 基礎輸入邏輯 ====================

    public void OpenPlayerInput(Action<string> onCallback)
    {
        _isMultiSegmentFlow = false;
        _onInputSubmitted = onCallback;
        responseTMP.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        inputArea.SetActive(true);
        if (enterBtn) enterBtn.gameObject.SetActive(true);
        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void HandleSubmit()
    {
        if (!IsInputFieldActive() || string.IsNullOrEmpty(inputField.text)) return;
        string text = inputField.text;
        inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        responseTMP.gameObject.SetActive(true);
        responseTMP.text = "傳輸訊息中...";
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
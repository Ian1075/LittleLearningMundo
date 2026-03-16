using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ChatUIManager : MonoBehaviour
{
    [Header("一般對話 UI 元件")]
    public GameObject background;
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI responseTMP;
    public GameObject inputArea; 
    public TMP_InputField inputField;
    public Button enterBtn;
    public GameObject nextIcon;

    [Header("選擇題 UI 元件 (Q&A)")]
    public GameObject choicePanel;
    public TextMeshProUGUI questionTMP;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceButtonTMPs;

    [Header("打字機特效")]
    public int scrambleCount = 3;
    public float scrambleSpeed = 0.015f;
    public float charDelay = 0.02f;
    public int maxCharsPerSegment = 80;

    private string _glitchChars = "!@#$%^&*()_+-=[]{}|;':,.<>/?0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    private Action _onEscPressed;
    private Action _onFinishedAll; 
    private Action<string> _onInputSubmitted; 
    
    private bool _isTyping = false;
    private string _currentSegmentShowing = "";

    // --- 連續串流模式 (用於自由對話) ---
    private bool _isContinuousStreaming = false;
    private string _continuousStreamBuffer = ""; 
    private int _streamCharPtr = 0;

    // --- 動態分段串流模式 (用於導覽解說) ---
    private Queue<string> _dynamicQueue = new Queue<string>();
    private bool _isDynamicStreaming = false;
    private bool _isDynamicStreamEnded = false;
    private string _dynamicBuffer = "";
    private bool _isWaitingForDynamicSegment = false;

    private void Awake()
    {
        if (enterBtn != null) enterBtn.onClick.AddListener(HandleSubmit);
        CloseChat(); 
    }

    private void Update()
    {
        UpdateCanvasCamera();

        if (background == null || !background.activeSelf) return;

        if (choicePanel != null && choicePanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _onEscPressed?.Invoke();
            CloseChat();
            return;
        }

        if (!IsInputFieldActive())
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                if (_isDynamicStreaming)
                {
                    if (_isTyping) { StopAllCoroutines(); CompleteDynamicSegmentVisual(); }
                    else if (nextIcon != null && nextIcon.activeSelf) { PlayNextDynamicSegment(); }
                }
                else if (_isContinuousStreaming)
                {
                    if (_isTyping) { StopAllCoroutines(); CompleteContinuousSegmentVisual(); }
                    else if (nextIcon != null && nextIcon.activeSelf)
                    {
                        nextIcon.SetActive(false);
                        responseTMP.text = "";
                        _currentSegmentShowing = "";
                        
                        if (_streamCharPtr < _continuousStreamBuffer.Length)
                            StartCoroutine(ContinuousTypewriterCore());
                        else
                        {
                            _isContinuousStreaming = false;
                            _onFinishedAll?.Invoke();
                        }
                    }
                }
            }
        }
        else 
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                HandleSubmit();
        }
    }

    private void UpdateCanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (canvas.worldCamera != Camera.main) canvas.worldCamera = Camera.main;
        }
    }

    public void CloseChat()
    {
        StopAllCoroutines();
        _isTyping = _isContinuousStreaming = _isDynamicStreaming = false;
        
        if (background) background.SetActive(false);
        if (nameTMP) nameTMP.gameObject.SetActive(false);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (inputArea) inputArea.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        if (choicePanel) choicePanel.SetActive(false);
    }

    // ==========================================
    // =     動態分段串流 (Dynamic Segment)       =
    // ==========================================

    /// <summary>
    /// 初始化動態分段隊列，準備接收即時拆分的句子。
    /// </summary>
    public void StartDynamicSegmentedStream(string npcName, Action onEsc, Action onAllFinished)
    {
        background.SetActive(true);
        if (nameTMP) { nameTMP.gameObject.SetActive(true); nameTMP.text = npcName; }
        responseTMP.gameObject.SetActive(true);
        inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        if (choicePanel) choicePanel.SetActive(false);

        _onEscPressed = onEsc;
        _onFinishedAll = onAllFinished;

        _dynamicQueue.Clear();
        _dynamicBuffer = "";
        _isDynamicStreaming = true;
        _isDynamicStreamEnded = false;
        _isWaitingForDynamicSegment = true;
        _isTyping = false;

        responseTMP.text = "學長正在整理思緒...";
    }

    /// <summary>
    /// 即時接收 AI 回傳的字串，一湊滿整句就推入隊列播放。
    /// </summary>
    public void AppendDynamicStreamChunk(string chunk)
    {
        if (!_isDynamicStreaming) return;

        _dynamicBuffer += chunk;
        
        // 尋找句號、驚嘆號等標點符號作為分割點
        string pattern = @"(?<=[。！？；])";
        string[] parts = Regex.Split(_dynamicBuffer, pattern);

        _dynamicBuffer = ""; // 清空緩衝，準備裝載剩餘或未完成的字元
        for (int i = 0; i < parts.Length; i++)
        {
            if (i == parts.Length - 1)
            {
                // 最後一段可能還沒遇到標點符號，放回緩衝區等待
                _dynamicBuffer = parts[i];
                
                // 防呆機制：如果 AI 忘記加標點符號，字數太長就強制截斷成一句
                if (_dynamicBuffer.Length > maxCharsPerSegment)
                {
                    _dynamicQueue.Enqueue(_dynamicBuffer);
                    _dynamicBuffer = "";
                }
            }
            else
            {
                string trimmed = parts[i].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    _dynamicQueue.Enqueue(trimmed);
                }
            }
        }

        // 如果目前 UI 正在空等，立刻播放剛解析出來的句子
        if (_isWaitingForDynamicSegment && _dynamicQueue.Count > 0)
        {
            PlayNextDynamicSegment();
        }
    }

    /// <summary>
    /// AI 串流完全結束時呼叫，強迫輸出緩衝區最後的零星文字。
    /// </summary>
    public void FinishDynamicStream()
    {
        if (!_isDynamicStreaming) return;

        string trimmed = _dynamicBuffer.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            _dynamicQueue.Enqueue(trimmed);
        }
        _dynamicBuffer = "";
        _isDynamicStreamEnded = true;

        if (_isWaitingForDynamicSegment)
        {
            PlayNextDynamicSegment();
        }
    }

    private void PlayNextDynamicSegment()
    {
        _isWaitingForDynamicSegment = false;

        if (_dynamicQueue.Count > 0)
        {
            string segment = _dynamicQueue.Dequeue();
            if (nextIcon) nextIcon.SetActive(false);
            StopAllCoroutines();
            _currentSegmentShowing = segment;
            StartCoroutine(TypeWithGlitch(segment));
        }
        else
        {
            if (_isDynamicStreamEnded)
            {
                // 所有內容播放完畢
                _isDynamicStreaming = false;
                if (nextIcon) nextIcon.SetActive(false);
                _onFinishedAll?.Invoke();
            }
            else
            {
                // 字句還在生成中，稍微等一下
                _isWaitingForDynamicSegment = true;
                responseTMP.text = "（學長思考中...）";
                if (nextIcon) nextIcon.SetActive(false);
            }
        }
    }

    private void CompleteDynamicSegmentVisual()
    {
        _isTyping = false;
        responseTMP.text = _currentSegmentShowing;
        if (nextIcon) nextIcon.SetActive(true);
    }

    private IEnumerator TypeWithGlitch(string targetText)
    {
        _isTyping = true;
        responseTMP.text = "";
        string currentText = "";

        for (int i = 0; i < targetText.Length; i++)
        {
            char finalChar = targetText[i];
            if (char.IsWhiteSpace(finalChar)) {
                currentText += finalChar;
                responseTMP.text = currentText;
                continue;
            }
            for (int j = 0; j < scrambleCount; j++) {
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

    /// <summary>
    /// 一般非串流的對話，也統一利用這個強大的隊列系統一次性載入
    /// </summary>
    public void ShowNPCResponse(string npcName, string content, Action onEsc, Action onAllFinished)
    {
        StartDynamicSegmentedStream(npcName, onEsc, onAllFinished);
        AppendDynamicStreamChunk(content);
        FinishDynamicStream();
    }


    // ==========================================
    // =        連續串流模式 (保留給自由對話)        =
    // ==========================================

    public void PrepareStreamingResponse(string npcName)
    {
        _continuousStreamBuffer = ""; _streamCharPtr = 0; _currentSegmentShowing = "";
        _isContinuousStreaming = true;

        if (background) background.SetActive(true);
        if (nameTMP) { nameTMP.gameObject.SetActive(true); nameTMP.text = npcName; }
        if (responseTMP) { responseTMP.gameObject.SetActive(true); responseTMP.text = "..."; }
        
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        if (choicePanel) choicePanel.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(ContinuousTypewriterCore());
    }

    public void UpdateStreamingText(string fullText)
    {
        _continuousStreamBuffer = Regex.Replace(fullText, @"[\r\n]+", "").TrimStart();
    }

    public void FinishStreamingResponse(Action onEsc, Action onAllFinished)
    {
        _onEscPressed = onEsc; _onFinishedAll = onAllFinished;
        _isContinuousStreaming = false; 
    }

    private IEnumerator ContinuousTypewriterCore()
    {
        _isTyping = true;
        int currentSegmentCount = 0;
        while (_isContinuousStreaming || _streamCharPtr < _continuousStreamBuffer.Length)
        {
            if (_streamCharPtr < _continuousStreamBuffer.Length)
            {
                char c = _continuousStreamBuffer[_streamCharPtr];
                bool isSegmentBreak = "。！？；".Contains(c.ToString());
                if ((isSegmentBreak || currentSegmentCount >= maxCharsPerSegment) && _streamCharPtr < _continuousStreamBuffer.Length - 1)
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
        if (nextIcon) nextIcon.SetActive(true);
    }

    private void CompleteContinuousSegmentVisual()
    {
        _isTyping = false;
        while (_streamCharPtr < _continuousStreamBuffer.Length)
        {
            char c = _continuousStreamBuffer[_streamCharPtr];
            _currentSegmentShowing += c; _streamCharPtr++;
            if ("。！？；".Contains(c.ToString()) || _currentSegmentShowing.Length >= maxCharsPerSegment) break;
        }
        responseTMP.text = _currentSegmentShowing;
        if (nextIcon) nextIcon.SetActive(true);
    }


    // ==========================================
    // =           共用 UI 控制與輸入             =
    // ==========================================

    public void ShowMultipleChoice(string question, string correct, string w1, string w2, string w3, Action<string> onSelected)
    {
        if (background) background.SetActive(true);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);

        if (choicePanel) choicePanel.SetActive(true);
        if (questionTMP) questionTMP.text = question;

        List<string> options = new List<string> { correct, w1, w2, w3 };
        for (int i = 0; i < options.Count; i++) {
            string temp = options[i];
            int randomIndex = UnityEngine.Random.Range(i, options.Count);
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }

        for (int i = 0; i < 4; i++) {
            if (i >= choiceButtons.Length || i >= choiceButtonTMPs.Length) break;
            choiceButtonTMPs[i].text = options[i];
            string selectedOption = options[i];
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => {
                if (choicePanel) choicePanel.SetActive(false);
                onSelected?.Invoke(selectedOption);
            });
        }
    }

    public void OpenPlayerInput(Action<string> onCallback)
    {
        _onInputSubmitted = onCallback;
        if (background) background.SetActive(true);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);
        if (inputArea) inputArea.SetActive(true);
        if (enterBtn) enterBtn.gameObject.SetActive(true);
        if (inputField != null) { inputField.text = ""; inputField.ActivateInputField(); }
    }

    public void HandleSubmit()
    {
        if (!IsInputFieldActive() || string.IsNullOrEmpty(inputField.text)) return;
        string text = inputField.text;
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
        if (responseTMP) { responseTMP.gameObject.SetActive(true); responseTMP.text = "..."; }
        _onInputSubmitted?.Invoke(text);
    }

    public bool IsInputFieldActive() => inputArea != null && inputArea.activeSelf;

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
}
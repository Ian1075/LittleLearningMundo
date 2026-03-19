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

        // 1. 全域 Esc 偵測：關閉對話 (主線模式下禁止取消，防止中斷導覽流程)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isMainStory = GameModeManager.Instance != null && GameModeManager.Instance.currentMode == GameModeManager.GameMode.MainStory;
            if (!isMainStory)
            {
                _onEscPressed?.Invoke();
                CloseChat();
                return;
            }
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
        _isTyping = _isDynamicStreaming = false;
        
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

    public void StartDynamicSegmentedStream(string npcName, Action onEsc, Action onAllFinished, string waitingText = "學長正在整理思緒...")
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

        responseTMP.text = waitingText;
    }

    public void AppendDynamicStreamChunk(string chunk)
    {
        if (!_isDynamicStreaming) return;

        _dynamicBuffer += chunk;
        string pattern = @"(?<=[。！？；])";
        string[] parts = Regex.Split(_dynamicBuffer, pattern);

        _dynamicBuffer = ""; 
        for (int i = 0; i < parts.Length; i++)
        {
            if (i == parts.Length - 1)
            {
                _dynamicBuffer = parts[i];
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

        if (_isWaitingForDynamicSegment && _dynamicQueue.Count > 0)
        {
            PlayNextDynamicSegment();
        }
    }

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
                _isDynamicStreaming = false;
                if (nextIcon) nextIcon.SetActive(false);
                _onFinishedAll?.Invoke();
            }
            else
            {
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

    public void ShowNPCResponse(string npcName, string content, Action onEsc, Action onAllFinished)
    {
        StartDynamicSegmentedStream(npcName, onEsc, onAllFinished);
        AppendDynamicStreamChunk(content);
        FinishDynamicStream();
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
}
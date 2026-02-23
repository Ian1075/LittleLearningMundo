using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 唯一負責對話介面顯示與輸入邏輯的模組，支援鍵盤 Enter 送出
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

    [Header("打字機特效設定")]
    public int scrambleCount = 3;
    public float scrambleSpeed = 0.015f;
    public float charDelay = 0.02f;

    private string _glitchChars = "!@#$%^&*()_+-=[]{}|;':,.<>/?0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private Action<string> _onTextSubmitted;

    private void Awake()
    {
        if (enterBtn != null) 
            enterBtn.onClick.AddListener(HandleSubmit);
        
        CloseChat(); 
    }

    private void Update()
    {
        // 當輸入框開啟時，偵測鍵盤快捷鍵
        if (IsInputFieldActive())
        {
            // 按下 Enter 送出 (包含主鍵盤與小鍵盤)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HandleSubmit();
            }

            // 按下 Esc 關閉對話
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseChat();
            }
        }
    }

    public void CloseChat()
    {
        if (background) background.SetActive(false);
        if (nameTMP) nameTMP.gameObject.SetActive(false);
        if (responseTMP) responseTMP.gameObject.SetActive(false);
        if (inputArea) inputArea.SetActive(false);
        if (enterBtn) enterBtn.gameObject.SetActive(false);
    }

    public void ShowNPCResponse(string npcName, string content)
    {
        CloseChat();
        background.SetActive(true);
        nameTMP.gameObject.SetActive(true);
        responseTMP.gameObject.SetActive(true);
        
        nameTMP.text = npcName;
        
        StopAllCoroutines();
        StartCoroutine(TypeWithGlitch(content));
    }

    public void OpenPlayerInput(Action<string> onCallback)
    {
        _onTextSubmitted = onCallback;
        
        responseTMP.gameObject.SetActive(false);
        inputArea.SetActive(true);
        enterBtn.gameObject.SetActive(true);
        
        inputField.text = "";
        inputField.ActivateInputField(); // 自動聚焦，方便直接打字
    }

    public void HandleSubmit()
    {
        // 檢查是否為空，且確保只有在輸入框開啟時才處理
        if (string.IsNullOrEmpty(inputField.text) || !IsInputFieldActive()) return;
        
        string text = inputField.text;
        _onTextSubmitted?.Invoke(text);
        
        inputArea.SetActive(false);
        enterBtn.gameObject.SetActive(false);
        responseTMP.gameObject.SetActive(true);
        responseTMP.text = "解碼訊息中...";
    }

    public bool IsInputFieldActive() => inputArea != null && inputArea.activeSelf;

    private IEnumerator TypeWithGlitch(string targetText)
    {
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
    }
}
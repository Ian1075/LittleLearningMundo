using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; 

public class LoginUIManager : MonoBehaviour
{
    [Header("場景設定")]
    public string nextSceneName = "MainScene";

    [Header("介面板塊")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("登入元件")]
    public TMP_InputField loginNameInput;
    public TMP_InputField loginPassInput;
    public Button loginBtn;
    public Button toRegBtn;

    [Header("註冊元件")]
    public TMP_InputField regNameInput;
    public TMP_InputField regPassInput;
    public TMP_InputField regConfirmInput;
    public Button regSubmitBtn;
    public Button toLogBtn;

    public TextMeshProUGUI warningText; 

    private void Start()
    {
        Time.timeScale = 1f; 
        ShowLogin();
        loginBtn.onClick.AddListener(OnLogin);
        regSubmitBtn.onClick.AddListener(OnRegister);
        toRegBtn.onClick.AddListener(ShowRegister);
        toLogBtn.onClick.AddListener(ShowLogin);

        if (loginPassInput) loginPassInput.contentType = TMP_InputField.ContentType.Password;
        if (regPassInput) regPassInput.contentType = TMP_InputField.ContentType.Password;
        if (regConfirmInput) regConfirmInput.contentType = TMP_InputField.ContentType.Password;
    }

    public void ShowLogin() { loginPanel.SetActive(true); registerPanel.SetActive(false); warningText.gameObject.SetActive(false); }
    public void ShowRegister() { loginPanel.SetActive(false); registerPanel.SetActive(true); warningText.gameObject.SetActive(false); }

    private void OnLogin()
    {
        if (AccountManager.Instance.Login(loginNameInput.text.Trim(), loginPassInput.text, out string err))
            SceneManager.LoadScene(nextSceneName);
        else ShowWarning(err);
    }

    private void OnRegister()
    {
        string n = regNameInput.text.Trim();
        string p = regPassInput.text;
        if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(p)) { ShowWarning("不能為空！"); return; }
        if (p != regConfirmInput.text) { ShowWarning("密碼不一致！"); return; }

        if (AccountManager.Instance.Register(n, p, out string err))
            SceneManager.LoadScene(nextSceneName);
        else ShowWarning(err);
    }

    private void ShowWarning(string m) { warningText.text = m; warningText.gameObject.SetActive(true); }
}
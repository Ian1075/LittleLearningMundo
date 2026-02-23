using UnityEngine;

/// <summary>
/// 負責切換場景 3D 渲染狀態的工具
/// 可以在不停止物理與邏輯的情況下，關閉 3D 視覺呈現以節省效能
/// </summary>
public class SceneRenderingManager : MonoBehaviour
{
    [Header("設定")]
    public Camera mainCamera;
    public KeyCode toggleKey = KeyCode.F1; // 按下 F1 切換渲染

    [Header("狀態")]
    [SerializeField] private bool isRenderingEnabled = true;

    // 儲存原始的 Culling Mask，以便恢復
    private int _originalMask;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            _originalMask = mainCamera.cullingMask;
        }
    }

    private void Update()
    {
        // 偵測快捷鍵切換渲染狀態
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleSceneRendering();
        }
    }

    /// <summary>
    /// 切換場景渲染開關
    /// </summary>
    public void ToggleSceneRendering()
    {
        isRenderingEnabled = !isRenderingEnabled;
        ApplyRenderingState();
    }

    private void ApplyRenderingState()
    {
        if (mainCamera == null) return;

        if (isRenderingEnabled)
        {
            // 恢復原始渲染圖層
            mainCamera.cullingMask = _originalMask;
            Debug.Log("[System] 已開啟 3D 場景渲染");
        }
        else
        {
            // 只保留 UI 層 (假設 UI 在第 5 層 "UI")，其餘全部不渲染
            // 0 代表 Nothing，但我們通常想保留 UI 顯示
            int uiLayerMask = 1 << LayerMask.NameToLayer("UI");
            mainCamera.cullingMask = uiLayerMask;
            
            // 如果你連 UI 也不想要，就設為 0
            // mainCamera.cullingMask = 0;

            Debug.Log("[System] 已關閉 3D 場景渲染 (物理與 AI 仍持續運作)");
        }
    }

    /// <summary>
    /// 外部方法：強制設定渲染狀態
    /// </summary>
    public void SetRendering(bool enabled)
    {
        isRenderingEnabled = enabled;
        ApplyRenderingState();
    }
}
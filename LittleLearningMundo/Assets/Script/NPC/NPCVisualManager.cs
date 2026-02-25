using UnityEngine;

/// <summary>
/// 負責處理 NPC 的外觀回饋。
/// 確保 NPC 預設使用原始材質。
/// </summary>
public class NPCVisualManager : MonoBehaviour
{
    [Header("材質組件")]
    public Renderer npcRenderer;
    public Material originalMaterial;
    public Material highlightMaterial;

    private void Start()
    {
        // 1. 自動抓取 Renderer
        if (npcRenderer == null) npcRenderer = GetComponentInChildren<Renderer>();
        
        // 2. 紀錄原始材質 (如果 Inspector 沒拉的話)
        if (npcRenderer != null && originalMaterial == null) 
            originalMaterial = npcRenderer.sharedMaterial;

        // 3. 關鍵：遊戲開始時強制設為原始材質，確保「預設不亮」
        ApplyMaterial(originalMaterial);
    }

    /// <summary>
    /// 切換高亮狀態
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (npcRenderer == null || originalMaterial == null || highlightMaterial == null) return;
        
        ApplyMaterial(highlight ? highlightMaterial : originalMaterial);
    }

    private void ApplyMaterial(Material mat)
    {
        if (npcRenderer != null && mat != null)
        {
            npcRenderer.material = mat;
        }
    }
}
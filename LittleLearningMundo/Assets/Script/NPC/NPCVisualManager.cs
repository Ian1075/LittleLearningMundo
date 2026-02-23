using UnityEngine;

/// <summary>
/// 負責處理 NPC 的外觀回饋 (材質切換)。
/// </summary>
public class NPCVisualManager : MonoBehaviour
{
    [Header("組件")]
    public Renderer npcRenderer;
    public Material originalMaterial;
    public Material highlightMaterial;

    private void Awake()
    {
        if (npcRenderer == null) npcRenderer = GetComponentInChildren<Renderer>();
        if (npcRenderer != null && originalMaterial == null) originalMaterial = npcRenderer.sharedMaterial;
    }

    /// <summary>
    /// 切換高亮狀態
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (npcRenderer == null || originalMaterial == null || highlightMaterial == null) return;
        npcRenderer.material = highlight ? highlightMaterial : originalMaterial;
    }
}
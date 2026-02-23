using UnityEngine;
using System.Linq;

/// <summary>
/// 處理 3D 空間偵測與互動觸發，狀態邏輯交由 PlayerController 管理
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("互動設定")]
    public float interactionRange = 5f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask npcLayer;

    [Header("引用組件")]
    public PlayerController playerController;
    public ChatUIManager chatUI;

    private NPCController _currentNearestNpc;

    private void Start()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // 檢查 UI 是否正在輸入，或者玩家是否正在對話狀態
        if (chatUI != null && chatUI.IsInputFieldActive()) return;

        FindAndHighlightNearestNPC();

        if (Input.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }
    }

    private void FindAndHighlightNearestNPC()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, npcLayer);
        
        NPCController nearest = hitColliders
            .Select(c => c.GetComponent<NPCController>())
            .Where(n => n != null)
            .OrderBy(n => Vector3.Distance(transform.position, n.transform.position))
            .FirstOrDefault();

        if (nearest != _currentNearestNpc)
        {
            if (_currentNearestNpc != null) _currentNearestNpc.visualManager.SetHighlight(false);
            _currentNearestNpc = nearest;
            if (_currentNearestNpc != null) _currentNearestNpc.visualManager.SetHighlight(true);
        }
    }

    private void HandleInteraction()
    {
        if (_currentNearestNpc == null) return;

        switch (_currentNearestNpc.currentState)
        {
            case NPCController.NPCState.Idle:
                // 1. 通知 Controller 切換到對話狀態（鎖定移動）
                playerController.SetState(PlayerController.PlayerState.Talking);
                // 2. 讓 NPC 開始打招呼
                _currentNearestNpc.StartGreeting();
                break;

            case NPCController.NPCState.Greeting:
                // 如果已經在對話中，再按一次 E 切換到輸入模式
                _currentNearestNpc.SwitchToInputMode();
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
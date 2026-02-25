using UnityEngine;
using System.Linq;

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

    private void Update()
    {
        if (chatUI != null && chatUI.IsInputFieldActive()) 
        {
            ClearCurrentHighlight();
            return;
        }

        UpdateNearestNPC();

        if (Input.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }
    }

    private void UpdateNearestNPC()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, npcLayer);
        
        NPCController nearest = hitColliders
            .Select(c => c.GetComponentInParent<NPCController>())
            .Where(n => n != null)
            .OrderBy(n => Vector3.Distance(transform.position, n.transform.position))
            .FirstOrDefault();

        if (nearest != _currentNearestNpc)
        {
            ClearCurrentHighlight();
            _currentNearestNpc = nearest;
            if (_currentNearestNpc != null) _currentNearestNpc.SetHighlight(true);
        }
    }

    private void ClearCurrentHighlight()
    {
        if (_currentNearestNpc != null) 
        {
            _currentNearestNpc.SetHighlight(false);
            _currentNearestNpc = null;
        }
    }

    private void HandleInteraction()
    {
        if (_currentNearestNpc == null) return;

        if (_currentNearestNpc.currentState == NPCController.NPCState.Idle)
        {
            // 模式切換判定
            if (_currentNearestNpc.isStoryNPC && 
                GameModeManager.Instance != null && 
                GameModeManager.Instance.currentMode == GameModeManager.GameMode.FreeMode)
            {
                Debug.Log($"[Interaction] 啟動主線模式：{_currentNearestNpc.npcName}");
                GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.MainStory);
                return;
            }

            if (playerController != null) 
                playerController.SetState(PlayerController.PlayerState.Talking);
                
            _currentNearestNpc.StartTalking();
        }
    }
}
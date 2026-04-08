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

        if (StoryManager.Instance != null && StoryManager.Instance.isStoryRunning) return;

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
            string npcName = _currentNearestNpc.gameObject.name;
            
            // 檢查該 NPC 是否有未完成的主線
            StoryData availableStory = ProgressManager.Instance.GetAvailableStoryForNPC(npcName);

            if (availableStory != null)
            {
                // 開啟主線導覽
                if (playerController != null) playerController.SetState(PlayerController.PlayerState.Talking);
                StoryManager.Instance.StartStory(availableStory, _currentNearestNpc);
            }
            else
            {
                // 日常聊天
                _currentNearestNpc.StartTalking();
            }
        }
    }
}
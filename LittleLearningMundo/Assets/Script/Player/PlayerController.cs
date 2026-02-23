using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 10f;

    public enum PlayerState { Idle, Talking }
    private PlayerState currentState = PlayerState.Idle;

    public void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void SetState(PlayerState state)
    {
        currentState = state;
    }

    void Update()
    {
        if (currentState == PlayerState.Talking) return;

        // 1. 取得輸入 (WASD / 方向鍵)
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // 2. 建立移動向量 (只在 X 和 Z 軸)
        Vector3 move = new Vector3(x, 0, z).normalized;
        
        // 3. 執行移動
        if (move.magnitude >= 0.1f)
        {
            controller.Move(move * speed * Time.deltaTime);
            
            // 讓玩家模型轉向移動方向 (這樣才知道他在往哪走)
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }
    }
}
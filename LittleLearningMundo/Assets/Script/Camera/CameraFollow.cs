using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 20, 0); // 高度

    void LateUpdate()
    {
        transform.position = player.position + offset;
    }
}
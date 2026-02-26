using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 處理主線劇本中的視角切換與投影圖片顯示。
/// 支援 [ExecuteAlways] 以實現編輯器內的即時預覽。
/// </summary>
[ExecuteAlways]
public class StoryVisualManager : MonoBehaviour
{
    public static StoryVisualManager Instance { get; private set; }

    [Header("引用")]
    public Camera mainCamera;
    public CameraFollow cameraFollow; 

    [Header("動態設定")]
    public float transitionSpeed = 3f;

    [Header("即時預覽工具 (編輯器專用)")]
    public bool livePreview = false;
    public BuildingZone previewZone;

    private bool _isCinematicMode = false;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 編輯器模式下的即時預覽邏輯
        if (!Application.isPlaying)
        {
            if (livePreview && previewZone != null && previewZone.cinematicCameraNode != null && mainCamera != null)
            {
                // 強制主攝影機與導覽節點同步
                mainCamera.transform.position = previewZone.cinematicCameraNode.position;
                mainCamera.transform.rotation = previewZone.cinematicCameraNode.rotation;
            }
            return;
        }

        // 遊戲執行中的邏輯處理 (如有需要)
    }

    /// <summary>
    /// 開啟特定地點的演出效果
    /// </summary>
    public void StartCinematic(BuildingZone zone, Sprite image)
    {
        if (zone == null || !Application.isPlaying) return;
        _isCinematicMode = true;

        if (cameraFollow != null) cameraFollow.enabled = false;

        if (zone.cinematicCameraNode != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCamera(zone.cinematicCameraNode.position, zone.cinematicCameraNode.rotation));
        }

        if (zone.projectionPlane != null && image != null)
        {
            zone.projectionPlane.gameObject.SetActive(true);
            SpriteRenderer sr = zone.projectionPlane.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = image;
                Color c = sr.color;
                c.a = 0;
                sr.color = c;
                StartCoroutine(FadeProjection(zone.projectionPlane, true));
            }
        }
    }

    /// <summary>
    /// 結束演出，歸還相機控制權
    /// </summary>
    public void EndCinematic(BuildingZone zone)
    {
        if (!_isCinematicMode || !Application.isPlaying) return;
        _isCinematicMode = false;

        if (zone != null && zone.projectionPlane != null)
        {
            StartCoroutine(FadeProjection(zone.projectionPlane, false));
        }

        if (cameraFollow != null) cameraFollow.enabled = true;
    }

    private IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot)
    {
        float t = 0;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;
    }

    private IEnumerator FadeProjection(Transform plane, bool fadeIn)
    {
        SpriteRenderer sr = plane.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float t = 0;
        float duration = 1f;
        Color col = sr.color;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            col.a = fadeIn ? t : (1f - t);
            sr.color = col;
            yield return null;
        }

        if (!fadeIn) plane.gameObject.SetActive(false);
    }

    // 當在 Inspector 中勾選 livePreview 時，確保即時反應
    private void OnValidate()
    {
        if (!Application.isPlaying && livePreview && previewZone != null && previewZone.cinematicCameraNode != null && mainCamera != null)
        {
            mainCamera.transform.position = previewZone.cinematicCameraNode.position;
            mainCamera.transform.rotation = previewZone.cinematicCameraNode.rotation;
        }
    }
}
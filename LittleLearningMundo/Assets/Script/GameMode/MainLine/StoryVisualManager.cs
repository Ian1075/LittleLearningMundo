using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 處理導覽演出。修正：確保在演出結束時，攝影機能平滑回歸玩家跟隨模式。
/// </summary>
[ExecuteAlways]
public class StoryVisualManager : MonoBehaviour
{
    public static StoryVisualManager Instance { get; private set; }

    [Header("基礎引用")]
    public Camera mainCamera;
    public CameraFollow cameraFollow; 

    [Header("演出參數")]
    public float transitionSpeed = 3.0f;
    public float fadeDuration = 1.0f;
    public float focusWaitTime = 5.0f;

    [Header("即時預覽工具")]
    public bool livePreview = false;
    public StoryData previewStoryData; 
    public int previewStepIndex = 0;
    public int previewProjectionIndex = -1; 

    private bool _isCinematicMode = false;
    private Coroutine _cinematicRoutine;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            livePreview = false;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying && livePreview && mainCamera != null && previewStoryData != null)
        {
            UpdateEditorPreview();
        }
    }

    private void UpdateEditorPreview()
    {
        if (previewStepIndex >= 0 && previewStepIndex < previewStoryData.steps.Count)
        {
            var step = previewStoryData.steps[previewStepIndex];
            if (previewProjectionIndex == -1)
            {
                BuildingZone bZone = FindBuildingZone(step.locationID);
                if (bZone != null && bZone.cinematicCameraNode != null)
                    ApplyView(bZone.cinematicCameraNode.position, bZone.cinematicCameraNode.rotation);
            }
            else if (previewProjectionIndex >= 0 && previewProjectionIndex < step.projections.Count)
            {
                var proj = step.projections[previewProjectionIndex];
                if (proj.cameraNode != null)
                    ApplyView(proj.cameraNode.position, proj.cameraNode.rotation);
            }
        }
    }

    private BuildingZone FindBuildingZone(string id)
    {
        BuildingZone[] zones = Object.FindObjectsOfType<BuildingZone>();
        foreach (var z in zones) if (z.locationID == id) return z;
        return null;
    }

    private void ApplyView(Vector3 pos, Quaternion rot)
    {
        mainCamera.transform.position = pos;
        mainCamera.transform.rotation = rot;
    }

    public void StartCinematic(BuildingZone zone, List<StoryData.ProjectionData> projections)
    {
        if (zone == null || !Application.isPlaying) return;
        
        _isCinematicMode = true;
        
        // 1. 禁用玩家跟隨
        if (cameraFollow != null) cameraFollow.enabled = false;

        // 2. 開始運鏡序列
        if (_cinematicRoutine != null) StopCoroutine(_cinematicRoutine);
        _cinematicRoutine = StartCoroutine(CinematicSequence(zone, projections));
    }

    private IEnumerator CinematicSequence(BuildingZone zone, List<StoryData.ProjectionData> projections)
    {
        // 先飛向大遠景
        if (zone.cinematicCameraNode != null)
        {
            yield return StartCoroutine(MoveCamera(zone.cinematicCameraNode.position, zone.cinematicCameraNode.rotation));
            yield return new WaitForSeconds(0.5f);
        }

        foreach (var proj in projections)
        {
            if (!_isCinematicMode) yield break;

            if (proj.quad != null && proj.image != null)
            {
                proj.quad.gameObject.SetActive(true);
                Renderer r = proj.quad.GetComponent<Renderer>();
                if (r is SpriteRenderer sr) sr.sprite = proj.image;
                else if (r is MeshRenderer mr) mr.material.mainTexture = proj.image.texture;
                
                SetAlpha(r, 0);
                StartCoroutine(FadeRenderer(r, true));
            }

            if (proj.cameraNode != null)
            {
                yield return StartCoroutine(MoveCamera(proj.cameraNode.position, proj.cameraNode.rotation));
            }

            yield return new WaitForSeconds(focusWaitTime);
        }
    }

    /// <summary>
    /// 結束演出並恢復一般攝影機
    /// </summary>
    public void EndCinematic(BuildingZone zone)
    {
        if (!Application.isPlaying) return;
        
        _isCinematicMode = false;
        
        if (_cinematicRoutine != null) StopCoroutine(_cinematicRoutine);

        // 隱藏該地點的所有照片
        if (zone != null)
        {
            // 這裡可以透過 StoryManager 傳入目前的 projections 來關閉
            // 或是簡單地讓 Zone 自己處理
        }

        // 恢復跟隨腳本
        if (cameraFollow != null)
        {
            cameraFollow.enabled = true;
            Debug.Log("[VisualManager] 攝影機跟隨已恢復。");
        }
    }

    private IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot)
    {
        float t = 0;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        while (t < 1.0f)
        {
            t += Time.deltaTime * transitionSpeed;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            yield return null;
        }
    }

    private IEnumerator FadeRenderer(Renderer r, bool fadeIn)
    {
        if (r == null) yield break;
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / fadeDuration;
            float alpha = fadeIn ? t : (1.0f - t);
            SetAlpha(r, alpha);
            yield return null;
        }
        if (!fadeIn) r.gameObject.SetActive(false);
    }

    private void SetAlpha(Renderer r, float alpha)
    {
        if (r is SpriteRenderer sr) { Color c = sr.color; c.a = alpha; sr.color = c; }
        else if (r is MeshRenderer mr && mr.material.HasProperty("_Color")) { Color c = mr.material.color; c.a = alpha; mr.material.color = c; }
    }
}
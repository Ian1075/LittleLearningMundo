using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 處理電影感演出。
/// 支援由 NPCController 逐步觸發對應的特寫運鏡。
/// </summary>
[ExecuteAlways]
public class StoryVisualManager : MonoBehaviour
{
    public static StoryVisualManager Instance { get; private set; }

    [Header("基礎引用")]
    public Camera mainCamera;
    public CameraFollow cameraFollow; 

    [Header("演出參數")]
    [Range(1f, 10f)] public float transitionSpeed = 3.5f;
    public float fadeDuration = 1.0f;

    [Header("編輯器預覽工具")]
    public bool livePreview = false;
    public StoryData previewStoryData; 
    public int previewStepIndex = 0;
    public int previewProjectionIndex = -1; 

    private bool _isCinematicMode = false;

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
        // 編輯器即時同步預覽邏輯
        if (!Application.isPlaying && livePreview && mainCamera != null && previewStoryData != null)
        {
            if (previewStepIndex >= 0 && previewStepIndex < previewStoryData.steps.Count)
            {
                var step = previewStoryData.steps[previewStepIndex];
                
                if (previewProjectionIndex == -1)
                {
                    BuildingZone bZone = FindBuildingZone(step.locationID);
                    if (bZone != null && bZone.cinematicCameraNode != null)
                        ApplyViewInstant(bZone.cinematicCameraNode.position, bZone.cinematicCameraNode.rotation);
                }
                else if (previewProjectionIndex >= 0 && previewProjectionIndex < step.projectionSteps.Count)
                {
                    var proj = step.projectionSteps[previewProjectionIndex];
                    if (proj.cameraNode != null)
                        ApplyViewInstant(proj.cameraNode.position, proj.cameraNode.rotation);
                }
            }
        }
    }

    private void ApplyViewInstant(Vector3 pos, Quaternion rot)
    {
        mainCamera.transform.position = pos;
        mainCamera.transform.rotation = rot;
    }

    private BuildingZone FindBuildingZone(string id)
    {
        BuildingZone[] zones = Object.FindObjectsOfType<BuildingZone>();
        foreach (var z in zones) if (z.locationID == id) return z;
        return null;
    }

    /// <summary>
    /// 第一步：進入建築物大遠景
    /// </summary>
    public void ShowCinematicIntro(BuildingZone zone)
    {
        if (zone == null || !Application.isPlaying) return;
        _isCinematicMode = true;
        
        if (cameraFollow != null) cameraFollow.enabled = false;

        if (zone.cinematicCameraNode != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCamera(zone.cinematicCameraNode.position, zone.cinematicCameraNode.rotation));
        }
    }

    /// <summary>
    /// 第二步：切換到指定的照片特寫位
    /// </summary>
    public void ShowStepVisual(StoryData.ProjectionStep step)
    {
        if (step == null || !_isCinematicMode || !Application.isPlaying) return;

        // 1. 照片淡入
        if (step.quad != null && step.image != null)
        {
            step.quad.gameObject.SetActive(true);
            Renderer r = step.quad.GetComponent<Renderer>();
            if (r is SpriteRenderer sr) { sr.sprite = step.image; StartCoroutine(FadeRenderer(sr, true)); }
            else if (r is MeshRenderer mr) { mr.material.mainTexture = step.image.texture; StartCoroutine(FadeRenderer(mr, true)); }
        }

        // 2. 攝影機運鏡到該步驟的 Empty Object
        if (step.cameraNode != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCamera(step.cameraNode.position, step.cameraNode.rotation));
        }
    }

    /// <summary>
    /// 結束演出，淡出所有照片並恢復玩家相機
    /// </summary>
    public void EndCinematic(StoryData.StoryStep currentStep)
    {
        if (!_isCinematicMode || !Application.isPlaying) return;
        _isCinematicMode = false;
        
        StopAllCoroutines();

        // 淡出並隱藏該地點的所有照片
        if (currentStep != null)
        {
            foreach (var proj in currentStep.projectionSteps)
            {
                if (proj.quad != null)
                {
                    Renderer r = proj.quad.GetComponent<Renderer>();
                    if (r != null) StartCoroutine(FadeRenderer(r, false));
                }
            }
        }

        if (cameraFollow != null) cameraFollow.enabled = true;
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
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;
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
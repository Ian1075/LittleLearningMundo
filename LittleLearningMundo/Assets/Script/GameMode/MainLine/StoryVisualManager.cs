using UnityEngine;
using System.Collections;

public class StoryVisualManager : MonoBehaviour
{
    public static StoryVisualManager Instance { get; private set; }

    [Header("基礎引用")]
    public Camera mainCamera;
    public CameraFollow cameraFollow; 

    [Header("演出參數")]
    [Range(1f, 10f)] public float transitionSpeed = 3.5f;
    public float fadeDuration = 1.0f;

    private bool _isCinematicMode = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 第一步：進入建築物大遠景
    /// </summary>
    public void ShowCinematicIntro(StoryData.StoryStep step)
    {
        if (step == null || !Application.isPlaying) return;
        _isCinematicMode = true;
        
        if (cameraFollow != null) cameraFollow.enabled = false;

        if (step.cinematicCameraNode != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCamera(step.cinematicCameraNode.position, step.cinematicCameraNode.rotation));
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
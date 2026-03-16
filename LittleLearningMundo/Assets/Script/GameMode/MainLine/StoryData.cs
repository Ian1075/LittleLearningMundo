using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 劇本資料組件。
/// 注意：現在改為 MonoBehaviour，請將此腳本掛載在場景中的一個 GameObject 上（例如 StoryManager 同一個物件）。
/// 這樣你就可以直接把場景中的 Quad 和 CameraNode 拉進來。
/// </summary>
public class StoryData : MonoBehaviour
{
    [System.Serializable]
    public class ProjectionData
    {
        [Header("內容設定")]
        public Sprite image;
        [TextArea(2, 5)]
        public string imageDescription;

        [Header("場景物件指派")]
        [Tooltip("場景中的投影面 (Quad)")]
        public Transform quad;
        [Tooltip("該照片專屬的攝影機特寫位")]
        public Transform cameraNode;
    }

    [System.Serializable]
    public class StoryStep
    {
        [Header("站點設定")]
        public string locationID;
        
        [Tooltip("基礎介紹敘述")]
        [TextArea(3, 10)]
        public string description;

        public bool useAISummary = true;

        [Header("清單")]
        public List<ProjectionData> projections = new List<ProjectionData>();
    }

    [Header("劇情全域設定")]
    public string storyTitle = "導覽劇本";
    [TextArea(2, 5)]
    public string endStoryDialogue = "導覽行程結束囉！";

    public List<StoryStep> steps = new List<StoryStep>();
}
using UnityEngine;
using System.Collections.Generic;

public class StoryData : MonoBehaviour
{
    [System.Serializable]
    public class ProjectionStep
    {
        public string stepLabel = "新照片步驟";
        
        [Header("內容設定")]
        public Sprite image;
        [TextArea(2, 5)]
        public string imageDescription;

        [Header("問答設定 (Q&A)")]
        [Tooltip("介紹完這張照片後，是否要進行選擇題問答？")]
        public bool hasQuestion = false;
        [Tooltip("給 AI 的出題提示方向")]
        [TextArea(2, 3)]
        public string questionPrompt = "請根據照片內容出一個簡單的選擇題。";

        [Header("場景物件引用")]
        public Transform quad;
        public Transform cameraNode;
    }

    [System.Serializable]
    public class StoryStep
    {
        [Header("站點設定")]
        public string locationID;
        [TextArea(3, 10)]
        public string baseIntroduction;
        public bool useAISummary = true;

        [Header("視覺與對話序列")]
        public List<ProjectionStep> projectionSteps = new List<ProjectionStep>();
    }

    [Header("劇情全域設定")]
    public string storyTitle = "導覽劇本";
    [TextArea(2, 5)]
    public string endStoryDialogue = "導覽結束囉，祝你有個美好的一天！";

    public List<StoryStep> steps = new List<StoryStep>();
}
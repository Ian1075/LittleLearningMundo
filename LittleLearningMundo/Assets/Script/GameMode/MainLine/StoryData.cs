using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStoryData", menuName = "NCKU/Story Data")]
public class StoryData : ScriptableObject
{
    [System.Serializable]
    public class StoryStep
    {
        [Header("站點設定")]
        public string locationID;
        
        [Tooltip("敘述內容。")]
        [TextArea(3, 10)]
        public string description;

        [Tooltip("是否使用 AI 介紹。")]
        public bool useAISummary = true;

        [Header("視覺演出 (非必填)")]
        [Tooltip("要投射在建築物上的照片")]
        public Sprite projectionImage;
        
        [Tooltip("是否在介紹時切換到該建築的導覽視角")]
        public bool switchCameraView = true;
    }

    [Header("劇情設定")]
    public string storyTitle = "新生入學導覽";

    [TextArea(2, 5)]
    public string endStoryDialogue = "導覽行程結束囉！";

    public List<StoryStep> steps = new List<StoryStep>();
}
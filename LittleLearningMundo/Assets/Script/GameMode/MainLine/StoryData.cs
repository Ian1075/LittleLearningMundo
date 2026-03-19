using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 掛載在場景中 (例如一個叫 MainStory 的空物件上)。
/// 可以直接在這裡拖拉場景中的 Quad 和 CameraNode！
/// </summary>
public class StoryData : MonoBehaviour
{
    [System.Serializable]
    public class ProjectionStep
    {
        [Header("照片與視角 (直接拖入場景物件)")]
        public Transform quad;          // 顯示照片的牆面或投影布幕
        public Sprite image;            // 照片圖片
        public Transform cameraNode;    // 觀看這張照片時的攝影機位置

        [Header("AI 解說設定")]
        [TextArea(2, 5)]
        public string imageDescription; // 給 AI 參考的照片內容描述

        [Header("動態問答 (Q&A)")]
        public bool hasQuestion = false;
        [TextArea(2, 5)]
        public string questionPrompt;
    }

    [System.Serializable]
    public class StoryStep
    {
        [Header("站點設定")]
        public string locationID;
        public bool useAISummary = true;
        
        [TextArea(3, 10)]
        public string baseIntroduction;

        [Header("視覺演出")]
        [Tooltip("剛抵達這個地點時的大遠景相機點位")]
        public Transform cinematicCameraNode; 

        [Header("照片導覽序列")]
        public List<ProjectionStep> projectionSteps = new List<ProjectionStep>();
    }

    [Header("劇情全域設定")]
    public string storyTitle = "新生入學導覽";

    [TextArea(2, 5)]
    public string endStoryDialogue = "好啦，今天的校園導覽就到這邊結束囉！祝你在成大生活愉快！";

    [Header("筆記本設定 (整條路線完成後解鎖)")]
    public string noteTitle = "資工系館初探";
    [TextArea(3, 8)]
    public string noteContent = "今天學長帶我參觀了資工系館...";

    [Header("導覽步驟清單")]
    public List<StoryStep> steps = new List<StoryStep>();
}
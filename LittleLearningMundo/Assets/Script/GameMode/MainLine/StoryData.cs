using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 用於定義主線導覽流程的資料檔案。
/// </summary>
[CreateAssetMenu(fileName = "NewStoryData", menuName = "NCKU/Story Data")]
public class StoryData : ScriptableObject
{
    [System.Serializable]
    public class StoryStep
    {
        [Tooltip("目的地的 Location ID (需與 WaypointNode 的一致)")]
        public string locationID;
        
        [Tooltip("抵達後的自定義對話內容 (若不填寫則由 AI 自由發揮)")]
        [TextArea(3, 10)]
        public string customDialogue;

        [Tooltip("是否強制使用 AI 介紹該區域")]
        public bool useAISummary = true;
    }

    [Header("劇情章節名稱")]
    public string storyTitle = "新生入學導覽";

    [Header("導覽步驟清單")]
    public List<StoryStep> steps = new List<StoryStep>();
}
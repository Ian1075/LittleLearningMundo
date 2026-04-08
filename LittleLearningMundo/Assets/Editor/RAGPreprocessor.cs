using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using NCKU.RAG;

public class RAGPreprocessor : EditorWindow
{
    [MenuItem("Tools/NCKU RAG/建置向量資料庫")]
    public static void ShowWindow() => GetWindow<RAGPreprocessor>("RAG 建置器");

    private async void OnGUI()
    {
        if (GUILayout.Button("開始建置 (需執行 Ollama)"))
        {
            await Build();
        }
    }

    private async Task Build()
    {
        // 假設你有一個 RawKnowledge.json 包含簡單的 content 列表
        string inputPath = Application.dataPath + "/Resources/RawKnowledge.json";
        if (!File.Exists(inputPath)) { Debug.LogError("找不到原始資料！"); return; }

        string rawJson = File.ReadAllText(inputPath);
        // 這裡建議定義一個簡單的類別來讀取你的原始文字
        List<string> lines = new List<string>(File.ReadAllLines(Application.dataPath + "/Resources/RawKnowledge.txt"));

        VectorDatabase db = new VectorDatabase();
        var client = FindObjectOfType<OllamaApiClient>();

        for (int i = 0; i < lines.Count; i++)
        {
            EditorUtility.DisplayProgressBar("RAG 建置中", $"處理第 {i}/{lines.Count} 條", (float)i / lines.Count);
            float[] vec = await client.GetEmbeddingAsync(lines[i]);
            db.entries.Add(new KnowledgeEntry { content = lines[i], vector = vec });
        }

        File.WriteAllText(Application.dataPath + "/Resources/VectorDatabase.json", JsonUtility.ToJson(db));
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        Debug.Log("向量庫建置成功！");
    }
}
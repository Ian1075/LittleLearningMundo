using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using NCKU.RAG; // 確保有引用命名空間

public class RAGPreprocessor : EditorWindow
{
    private string rawTextPath = "Assets/Resources/RawKnowledge.txt";
    private string serverUrl = "http://140.116.154.86:11434"; // 建議直接寫死或在介面輸入

    [MenuItem("Tools/NCKU RAG/建置向量資料庫")]
    public static void ShowWindow() => GetWindow<RAGPreprocessor>("RAG 建置器");

    private async void OnGUI()
    {
        GUILayout.Label("RAG 向量化工具", EditorStyles.boldLabel);
        rawTextPath = EditorGUILayout.TextField("原始文本路徑", rawTextPath);
        serverUrl = EditorGUILayout.TextField("Ollama URL", serverUrl);

        if (GUILayout.Button("開始建置 (需執行 Ollama)"))
        {
            await Build();
        }
    }

    private async Task Build()
    {
        if (!File.Exists(rawTextPath))
        {
            Debug.LogError($"找不到原始資料：{rawTextPath}");
            return;
        }

        // 讀取所有行
        string[] lines = File.ReadAllLines(rawTextPath);
        VectorDatabase db = new VectorDatabase();

        // 解決 NullReferenceException：
        // 我們直接在 Editor 下建立一個臨時的 Client，不依賴場景物件
        GameObject tempGO = new GameObject("TempClient");
        OllamaApiClient client = tempGO.AddComponent<OllamaApiClient>();
        client.baseHostUrl = serverUrl;

        try
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                EditorUtility.DisplayProgressBar("RAG 建置中", $"處理第 {i + 1}/{lines.Length} 條: {lines[i]}", (float)i / lines.Length);
                
                // 呼叫 API 獲取向量
                float[] vec = await client.GetEmbeddingAsync(lines[i]);
                
                if (vec != null)
                {
                    db.entries.Add(new KnowledgeEntry { content = lines[i], vector = vec });
                }
                else
                {
                    Debug.LogWarning($"第 {i} 條轉換失敗，請檢查 Ollama 是否有 nomic-embed-text 模型");
                }
            }

            // 儲存結果
            string json = JsonUtility.ToJson(db);
            string outputPath = Application.dataPath + "/Resources/VectorDatabase.json";
            File.WriteAllText(outputPath, json);
            
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>向量庫建置成功！總計 {db.entries.Count} 條</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"建置過程出錯: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            DestroyImmediate(tempGO); // 刪除臨時物件
        }
    }
}
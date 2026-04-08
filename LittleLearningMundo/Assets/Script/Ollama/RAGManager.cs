using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NCKU.RAG; // 引用剛才定義的命名空間

public class RAGManager : MonoBehaviour
{
    public static RAGManager Instance { get; private set; }

    [Header("配置")]
    public string vectorDataFileName = "VectorDatabase"; 
    [Range(0, 1)] public float similarityThreshold = 0.5f;

    private VectorDatabase _db; // 這裡會自動指向 NCKU.RAG 裡的類別
    private OllamaApiClient _apiClient;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadDatabase();
    }

    private void Start()
    {
        _apiClient = FindObjectOfType<OllamaApiClient>();
    }

    private void LoadDatabase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(vectorDataFileName);
        if (jsonFile != null)
        {
            _db = JsonUtility.FromJson<VectorDatabase>(jsonFile.text);
            Debug.Log($"[RAG] 成功載入 {_db.entries.Count} 條向量知識碎片");
        }
    }

    public async Task<string> SearchSemanticKnowledgeAsync(string userInput, string currentLocationName, int limit = 3)
    {
        if (_db == null || _apiClient == null) return "";

        // 注入地點上下文優化搜尋
        string augmentedQuery = $"[當前在：{currentLocationName}] " + userInput;
        
        float[] queryVector = await _apiClient.GetEmbeddingAsync(augmentedQuery);
        if (queryVector == null) return "";

        var results = _db.entries
            .Select(e => new { e.content, score = CalculateCosineSimilarity(queryVector, e.vector) })
            .Where(x => x.score >= similarityThreshold)
            .OrderByDescending(x => x.score)
            .Take(limit)
            .ToList();

        if (results.Count == 0) return "";
        return string.Join("\n", results.Select(r => "• " + r.content));
    }

    private float CalculateCosineSimilarity(float[] vecA, float[] vecB)
    {
        float dotProduct = 0, magA = 0, magB = 0;
        for (int i = 0; i < vecA.Length; i++)
        {
            dotProduct += vecA[i] * vecB[i];
            magA += vecA[i] * vecA[i];
            magB += vecB[i] * vecB[i];
        }
        return dotProduct / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB));
    }
}
using System;
using System.Collections.Generic;

namespace NCKU.RAG
{
    [Serializable]
    public class KnowledgeEntry
    {
        public string content;   // 知識內容
        public float[] vector;    // 向量數據
    }

    [Serializable]
    public class VectorDatabase
    {
        public List<KnowledgeEntry> entries = new List<KnowledgeEntry>();
    }
}
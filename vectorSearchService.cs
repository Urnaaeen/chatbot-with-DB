using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleDbConnection
{
    public class VectorSearchService
    {
        private List<string> _columnTexts;
        private List<float[]> _embeddings;

        /// <summary>
        /// Vector store-д өгөгдөл ачаалах
        /// </summary>
        public void LoadVectorStore(List<string> columnTexts, List<float[]> embeddings)
        {
            if (columnTexts == null || embeddings == null)
                throw new ArgumentException("Column texts болон embeddings null байж болохгүй");

            if (columnTexts.Count != embeddings.Count)
                throw new ArgumentException("Column texts болон embeddings-ийн тоо таарахгүй байна");

            _columnTexts = columnTexts;
            _embeddings = embeddings;

            Console.WriteLine($"✅ Vector store-д {_columnTexts.Count} column ачаалагдлаа");
        }

        /// <summary>
        /// Cosine similarity тооцоолох
        /// </summary>
        private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                return 0f;

            float dotProduct = 0f;
            float magnitude1 = 0f;
            float magnitude2 = 0f;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            if (magnitude1 == 0f || magnitude2 == 0f)
                return 0f;

            return dotProduct / (float)(Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
        }

        /// <summary>
        /// Хайлт хийх
        /// </summary>
        public async Task<List<SearchResult>> SearchAsync(string query, EmbeddingService embeddingService, int topK = 5)
        {
            if (_columnTexts == null || _embeddings == null)
                throw new InvalidOperationException("Vector store ачаалаагүй байна. LoadVectorStore() дуудна уу.");

            Console.WriteLine($"🔍 Хайлт: '{query}'");

            // Query-г embedding болгох
            float[] queryEmbedding = await embeddingService.GetEmbeddingAsync(query);
            
            var results = new List<SearchResult>();

            // Бүх column-тай харьцуулах
            for (int i = 0; i < _columnTexts.Count; i++)
            {
                float similarity = CalculateCosineSimilarity(queryEmbedding, _embeddings[i]);
                
                results.Add(new SearchResult
                {
                    ColumnText = _columnTexts[i],
                    Similarity = similarity,
                    Index = i
                });
            }

            // Similarity-ээр эрэмбэлж top K авах
            var topResults = results
                .OrderByDescending(r => r.Similarity)
                .Take(topK)
                .ToList();

            Console.WriteLine($"📊 Хамгийн ойр {topResults.Count} үр дүн:");
            for (int i = 0; i < topResults.Count; i++)
            {
                var result = topResults[i];
                Console.WriteLine($"   {i + 1}. {result.ColumnText} (similarity: {result.Similarity:F4})");
            }

            return topResults;
        }

        /// <summary>
        /// Vector store-ийн статистик
        /// </summary>
        public void PrintStats()
        {
            if (_columnTexts == null || _embeddings == null)
            {
                Console.WriteLine("❌ Vector store ачаалаагүй байна");
                return;
            }

            Console.WriteLine("\n📊 Vector Store статистик:");
            Console.WriteLine($"   📋 Нийт column: {_columnTexts.Count}");
            if (_embeddings.Count > 0)
            {
                Console.WriteLine($"   🔢 Embedding dimension: {_embeddings[0].Length}");
            }
            Console.WriteLine($"   💾 Санах ойн хэрэглээ: ~{(_embeddings.Count * _embeddings[0].Length * 4) / 1024.0:F2} KB");
        }
    }

    /// <summary>
    /// Хайлтын үр дүн
    /// </summary>
    public class SearchResult
    {
        public string ColumnText { get; set; }
        public float Similarity { get; set; }
        public int Index { get; set; }
    }
}
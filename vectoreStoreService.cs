// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;

// public class VectorStoreService
// {
//     // Үндсэн өгөгдөл: Table -> Column -> Embedding vector
//     private Dictionary<string, Dictionary<string, float[]>> _embeddingData;

//     public VectorStoreService()
//     {
//         _embeddingData = new Dictionary<string, Dictionary<string, float[]>>();
//     }

//     // JSON файлнаас embedding-үүдийг унших
//     public async Task LoadFromFileAsync(string filePath)
//     {
//         if (!File.Exists(filePath))
//             throw new FileNotFoundException($"Файл олдсонгүй: {filePath}");

//         string json = await File.ReadAllTextAsync(filePath);
//         _embeddingData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, float[]>>>(json);
//         Console.WriteLine($"✅ Vector Store-д {_embeddingData.Sum(t => t.Value.Count)} embedding ачааллаа.");
//     }

//     // Cosine similarity-г тооцох
//     private double CosineSimilarity(float[] vectorA, float[] vectorB)
//     {
//         double dot = 0.0, denomA = 0.0, denomB = 0.0;
//         for (int i = 0; i < vectorA.Length; i++)
//         {
//             dot += vectorA[i] * vectorB[i];
//             denomA += vectorA[i] * vectorA[i];
//             denomB += vectorB[i] * vectorB[i];
//         }
//         return dot / (Math.Sqrt(denomA) * Math.Sqrt(denomB));
//     }

//     // Оролтын embedding-тай хамгийн ойр баганын нэрсийг хайх
//     public List<(string Table, string Column, double Similarity)> Search(float[] queryEmbedding, int topK = 5)
//     {
//         var results = new List<(string Table, string Column, double Similarity)>();

//         foreach (var table in _embeddingData)
//         {
//             foreach (var column in table.Value)
//             {
//                 double sim = CosineSimilarity(queryEmbedding, column.Value);
//                 results.Add((table.Key, column.Key, sim));
//             }
//         }

//         return results.OrderByDescending(r => r.Similarity).Take(topK).ToList();
//     }
// }

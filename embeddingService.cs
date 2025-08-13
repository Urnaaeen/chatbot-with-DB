using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OracleDbConnection
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public EmbeddingService(string apiKey, string model = "text-embedding-3-large")
        {
            _apiKey = apiKey;
            _model = model;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        /// <summary>
        /// Текстийг embedding болгох
        /// </summary>
        /// <param name="inputText">Embedding хийх текст</param>
        /// <returns>Float array embedding</returns>
        public async Task<float[]> GetEmbeddingAsync(string inputText)
        {
            try
            {
                var requestBody = new
                {
                    input = inputText,
                    model = _model
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody), 
                    Encoding.UTF8, 
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using JsonDocument json = JsonDocument.Parse(responseString);
                
                var embeddingJson = json.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("embedding");
                
                float[] embedding = embeddingJson
                    .EnumerateArray()
                    .Select(x => (float)x.GetDouble())
                    .ToArray();

                return embedding;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Embedding хийхэд алдаа: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Олон текстийг batch-аар embedding хийх
        /// </summary>
        /// <param name="texts">Embedding хийх текстүүдийн жагсаалт</param>
        /// <returns>Embedding-үүдийн жагсаалт</returns>
        public async Task<List<float[]>> GetBatchEmbeddingsAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();
            
            // OpenAI API rate limit-ийн улмаас batch хийх
            int batchSize = 20; // Нэг удаад 20 текст
            
            for (int i = 0; i < texts.Count; i += batchSize)
            {
                var batch = texts.Skip(i).Take(batchSize).ToList();
                
                try
                {
                    var requestBody = new
                    {
                        input = batch,
                        model = _model
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody), 
                        Encoding.UTF8, 
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
                    response.EnsureSuccessStatusCode();

                    var responseString = await response.Content.ReadAsStringAsync();
                    using JsonDocument json = JsonDocument.Parse(responseString);
                    
                    var dataArray = json.RootElement.GetProperty("data");
                    
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var embeddingJson = item.GetProperty("embedding");
                        float[] embedding = embeddingJson
                            .EnumerateArray()
                            .Select(x => (float)x.GetDouble())
                            .ToArray();
                        embeddings.Add(embedding);
                    }

                    // Rate limit-ийг зөөлрүүлэх
                    if (i + batchSize < texts.Count)
                    {
                        await Task.Delay(1000); // 1 секунд хүлээх
                    }

                    Console.WriteLine($"✅ Batch {i / batchSize + 1}: {batch.Count} текст боловсруулагдлаа");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Batch {i / batchSize + 1} боловсруулахад алдаа: {ex.Message}");
                    throw;
                }
            }

            return embeddings;
        }

        /// <summary>
        /// Хүснэгтийн бүх багана нэрийг embedding болгох
        /// </summary>
        /// <param name="tableColumns">Хүснэгт болон баганын мэдээлэл</param>
        /// <returns>Багана нэр болон түүний embedding</returns>
        public async Task<Dictionary<string, Dictionary<string, float[]>>> GetTableColumnsEmbeddingsAsync(
            Dictionary<string, List<string>> tableColumns)
        {
            var result = new Dictionary<string, Dictionary<string, float[]>>();
            
            Console.WriteLine("\n🔄 Хүснэгтийн баганыг embedding хийж байна...");
            
            foreach (var table in tableColumns)
            {
                string tableName = table.Key;
                List<string> columns = table.Value;
                
                Console.WriteLine($"\n📊 {tableName} хүснэгтийн {columns.Count} баганыг боловсруулж байна...");
                
                var columnEmbeddings = new Dictionary<string, float[]>();
                
                // Багана тус бүрийг embedding хийх
                for (int i = 0; i < columns.Count; i++)
                {
                    try
                    {
                        string columnInfo = columns[i];
                        
                        // Embedding хийх
                        float[] embedding = await GetEmbeddingAsync(columnInfo);
                        columnEmbeddings[columnInfo] = embedding;
                        
                        Console.WriteLine($"   ✅ {i + 1}/{columns.Count}: {columnInfo}");
                        
                        // Rate limit-ийг зөөлрүүлэх
                        await Task.Delay(200); // 200ms хүлээх
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ {columns[i]} боловсруулахад алдаа: {ex.Message}");
                    }
                }
                
                result[tableName] = columnEmbeddings;
            }
            
            Console.WriteLine("\n✅ Бүх баганын embedding дууслаа!");
            return result;
        }

        /// <summary>
        /// Ресурс цэвэрлэх
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
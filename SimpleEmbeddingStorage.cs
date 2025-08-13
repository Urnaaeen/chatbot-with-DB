using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OracleDbConnection
{
    public class SimpleEmbeddingStorage
    {
        private readonly string _storageDirectory;
        private readonly string _dataFileName;

        public SimpleEmbeddingStorage(string storageDirectory = "column_embeddings")
        {
            _storageDirectory = storageDirectory;
            _dataFileName = Path.Combine(_storageDirectory, "column_embeddings.json");

            // Directory үүсгэх
            Directory.CreateDirectory(_storageDirectory);
        }

        /// <summary>
        /// Embedding файл байгаа эсэхийг шалгах
        /// </summary>
        public bool EmbeddingFileExists()
        {
            return File.Exists(_dataFileName);
        }

        /// <summary>
        /// TABLE.COLUMN текстүүдийг embedding болгож файлд хадгалах
        /// </summary>
        public async Task SaveColumnEmbeddingsAsync(List<string> columnTexts, EmbeddingService embeddingService)
        {
            try
            {
                Console.WriteLine($"\n💾 {columnTexts.Count} баганы мэдээллийг embedding болгож хадгалж байна...");

                var embeddings = new List<float[]>();

                // Багана тус бүрийг embedding хийх
                for (int i = 0; i < columnTexts.Count; i++)
                {
                    try
                    {
                        string columnText = columnTexts[i];
                        Console.WriteLine($"   🔄 {i + 1}/{columnTexts.Count}: {columnText}");

                        // Embedding хийх
                        float[] embedding = await embeddingService.GetEmbeddingAsync(columnText);
                        embeddings.Add(embedding);

                        Console.WriteLine($"   ✅ Бэлэн! ({embedding.Length} dimensions)");

                        // Rate limiting - 200ms хүлээх
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ {columnTexts[i]} боловсруулахад алдаа: {ex.Message}");
                    }
                }

                // JSON файлд хадгалах
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(embeddings, jsonOptions);
                await File.WriteAllTextAsync(_dataFileName, jsonString);

                Console.WriteLine($"\n✅ Хадгалагдлаа:");
                Console.WriteLine($"   📊 {embeddings.Count} багана");
                Console.WriteLine($"   💾 Файл: {_dataFileName}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Embedding хадгалахад алдаа: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Хадгалсан embedding өгөгдлийг унших
        /// </summary>
        public async Task<List<float[]>> LoadColumnEmbeddingsAsync()
        {
            if (!EmbeddingFileExists())
                return null;

            try
            {
                string jsonString = await File.ReadAllTextAsync(_dataFileName);
                return JsonSerializer.Deserialize<List<float[]>>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Embedding файл унших алдаа: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Column embedding өгөгдлийн бүтэц
    /// </summary>
    public class ColumnEmbeddingData
    {
        public DateTime CreatedAt { get; set; }
        public int TotalColumns { get; set; }
        public Dictionary<string, float[]> ColumnEmbeddings { get; set; } = new();
    }
}
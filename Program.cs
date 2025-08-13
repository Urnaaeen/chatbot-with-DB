using System;
using System.Threading.Tasks;

namespace OracleDbConnection
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configuration
            string connectionString = "Data Source=160.187.40.43:1521/dw;User Id=erp_development;Password=green;";
            var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            // Services
            var oracleService = new OracleService(connectionString);
            var embeddingService = new EmbeddingService(openAiApiKey, "text-embedding-3-large");
            var storageService = new SimpleEmbeddingStorage();
            var vectorSearchService = new VectorSearchService();
            var chatGptService = new ChatGptService(openAiApiKey);

            try
            {
                Console.WriteLine("🚀 Column Embedding & Vector Search Програм");
                Console.WriteLine("=".PadRight(50, '='));

                List<string> columnTextsDB = null;
                List<string> columnTexts = null;
                List<float[]> embeddings = null;

                // Файл байгаа эсэхийг шалгах
                if (storageService.EmbeddingFileExists())
                {
                    Console.WriteLine("✅ Embedding файл байна - уншиж байна...");

                    // Oracle-аас column мэдээлэл авах (текст жагсаалт)
                    columnTextsDB = oracleService.GetBiEmployeeTablesInfo();

                    // Хадгалсан embeddings унших
                    embeddings = await storageService.LoadColumnEmbeddingsAsync();

                    if (embeddings != null && columnTextsDB.Count == embeddings.Count)
                    {
                        Console.WriteLine($"✅ {embeddings.Count} embedding амжилттай уншигдлаа");
                    }
                    else
                    {
                        Console.WriteLine("❌ Column тоо болон embedding тоо таарахгүй байна. Дахин үүсгэнэ...");
                        embeddings = null;
                    }
                }

                // Хэрэв embedding байхгүй бол шинээр үүсгэх
                if (embeddings == null)
                {
                    Console.WriteLine("❌ Embedding файл байхгүй эсвэл алдаатай - шинээр үүсгэнэ...\n");

                    // Oracle-аас column мэдээлэл авах
                    Console.WriteLine("🔍 Oracle-аас мэдээлэл авч байна...");
                    columnTextsDB = oracleService.GetBiEmployeeTablesInfo();

                    //энэ хэсэгт columnTexts ийн ард chatgpt-гээр монгол decs-ийг бичүүлээд өөр list авах

                    if (columnTextsDB.Count == 0)
                    {
                        Console.WriteLine("❌ Column олдсонгүй!");
                        return;
                    }

                    Console.WriteLine($"✅ {columnTextsDB.Count} column олдлоо\n");

                    // ChatGPT ашиглан монгол тайлбар нэмэх
                    columnTexts = await chatGptService.AddMongolianDescriptionsAsync(columnTextsDB);

                    //

                    // Embedding үүсгэж хадгалах
                    Console.WriteLine("🤖 Embedding үүсгэж байна...");
                    await storageService.SaveColumnEmbeddingsAsync(columnTexts, embeddingService);

                    // Шинээр үүсгэсэн embeddings унших
                    embeddings = await storageService.LoadColumnEmbeddingsAsync();
                }

                // Vector store-д ачаалах
                Console.WriteLine("\n📥 Vector store-д ачаалж байна...");
                vectorSearchService.LoadVectorStore(columnTexts, embeddings);
                vectorSearchService.PrintStats();

                // Интерактив хайлт эхлүүлэх
                Console.WriteLine("\n🔍 Хайлт режим эхэллээ!");
                Console.WriteLine("Хайх текст оруулна уу (гарахын тулд 'exit' гэж бичнэ үү):");
                Console.WriteLine("-".PadRight(50, '-'));

                while (true)
                {
                    Console.Write("\n> ");
                    string userQuery = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(userQuery) || userQuery.ToLower() == "exit")
                    {
                        Console.WriteLine("👋 Баяртай!");
                        break;
                    }

                    try
                    {
                        var searchResults = await vectorSearchService.SearchAsync(userQuery, embeddingService, 20);

                        if (searchResults.Count == 0)
                        {
                            Console.WriteLine("❌ Үр дүн олдсонгүй");
                        }
                    }
                    catch (Exception searchEx)
                    {
                        Console.WriteLine($"❌ Хайлтын алдаа: {searchEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Алдаа: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Дэлгэрэнгүй: {ex.InnerException.Message}");
                }
            }
            finally
            {
                embeddingService?.Dispose();
            }
        }
    }
}
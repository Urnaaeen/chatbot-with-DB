// ChatGptService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OracleDbConnection
{
    public class ChatGptService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ChatGptService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        /// <summary>
        /// Oracle column нэрүүдэд ChatGPT ашиглан монгол тайлбар нэмэх
        /// </summary>
        /// <param name="columnTexts">Анхны column нэрүүд</param>
        /// <returns>Монгол тайлбартай column нэрүүд</returns>
        public async Task<List<string>> AddMongolianDescriptionsAsync(List<string> columnTexts)
        {
            if (columnTexts == null || columnTexts.Count == 0)
            {
                Console.WriteLine("⚠️ Column жагсаалт хоосон байна");
                return columnTexts ?? new List<string>();
            }

            var enhancedTexts = new List<string>();

            Console.WriteLine("🤖 ChatGPT-ээр монгол тайлбар нэмж байна...");
            Console.WriteLine($"   📝 {columnTexts.Count} column боловсруулж байна...");

            try
            {
                var prompt = CreateMongolianDescriptionPrompt(columnTexts);
                var response = await CallChatGptApiAsync(prompt);
                enhancedTexts = ParseChatGptResponse(response);

                if (enhancedTexts.Count == columnTexts.Count)
                {
                    Console.WriteLine($"✅ {enhancedTexts.Count} column-д монгол тайлбар амжилттай нэмэгдлээ");
                }
                else
                {
                    Console.WriteLine($"⚠️ ChatGPT хариу тоо таарахгүй байна. Анхны жагсаалтаар үргэлжлүүлнэ...");
                    return columnTexts;
                }

                return enhancedTexts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ChatGPT API алдаа: {ex.Message}");
                Console.WriteLine("⚠️ Анхны column нэрүүдээр үргэлжлүүлнэ...");
                return columnTexts;
            }
        }

        /// <summary>
        /// ChatGPT-д илгээх prompt үүсгэх
        /// </summary>
        private string CreateMongolianDescriptionPrompt(List<string> columnTexts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Доорх Oracle database column нэрүүдэд монгол тайлбар нэм.");
            sb.AppendLine("Жишээ: BI_HREMPLOYEE.COMPANYNAME -> BI_HREMPLOYEE.COMPANYNAME (Компаний нэр)");
            sb.AppendLine("Зөвхөн үр дүнг л харуул, тайлбар бүү өг. Мөр бүрийг '-' тэмдгээр эхлүүл:");
            sb.AppendLine();

            foreach (var columnText in columnTexts)
            {
                sb.AppendLine($"- {columnText}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// ChatGPT API дуудах
        /// </summary>
        private async Task<string> CallChatGptApiAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Та MongoDB column нэрүүдэд монгол тайлбар нэмдэг туслах програм юм. Товч бөгөөд ойлгомжтой тайлбар өгнө үү."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_tokens = 3000,
                temperature = 0.3
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic responseObj = JsonConvert.DeserializeObject(responseJson);

            return responseObj.choices[0].message.content.ToString();
        }

        /// <summary>
        /// ChatGPT хариуг боловсруулах
        /// </summary>
        private List<string> ParseChatGptResponse(string response)
        {
            var enhancedTexts = new List<string>();
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && trimmedLine.StartsWith("-"))
                {
                    // "-" тэмдгийг арилгах
                    var cleanedLine = trimmedLine.Substring(1).Trim();
                    if (!string.IsNullOrEmpty(cleanedLine))
                    {
                        enhancedTexts.Add(cleanedLine);
                    }
                }
            }

            Console.WriteLine("\n📌 Хүснэгтүүдийн багана мэдээлэл:");
                    foreach (var entry in enhancedTexts)
                    {
                        Console.WriteLine($"   - {entry}");
                    }

            return enhancedTexts;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
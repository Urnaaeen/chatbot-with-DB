// ChatGptService.cs
using System;
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
        /// Монгол текстийг ChatGPT ашиглан англи хэл рүү орчуулах
        /// </summary>
        /// <param name="text">Орчуулах монгол текст</param>
        /// <returns>Англи хэл дээрх орчуулга</returns>
        public async Task<string> TranslateMongolianToEnglishAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("⚠️ Текст хоосон байна");
                return text ?? string.Empty;
            }

            Console.WriteLine("🤖 ChatGPT-ээр монгол текстийг англи хэл рүү орчуулж байна...");

            try
            {
                var prompt = CreateTranslationPrompt(text);
                var response = await CallChatGptApiAsync(prompt);
                
                // Хэрэв ChatGPT хариу өгөхгүй бол анхны текстийг буцаах
                if (string.IsNullOrWhiteSpace(response) || 
                    response.Contains("cannot translate") ||
                    response.Contains("not displaying properly") ||
                    response.Contains("provide the") ||
                    response.Contains("sorry") ||
                    response.Contains("шаардлагатай мэдээлэл байхгүй"))
                {
                    Console.WriteLine("⚠️ ChatGPT орчуулж чадсангүй, анхны текстээр үргэлжлүүлнэ...");
                    return text;
                }

                Console.WriteLine("✅ Текст амжилттай орчуулагдлаа");
                Console.WriteLine($"📌 Орчуулагдсан текст: {response}");

                return response.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ChatGPT API алдаа: {ex.Message}");
                Console.WriteLine("⚠️ Анхны текстээр үргэлжлүүлнэ...");
                return text;
            }
        }

        /// <summary>
        /// ChatGPT-д илгээх орчуулгын prompt үүсгэх
        /// </summary>
        private string CreateTranslationPrompt(string text)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Please translate this Mongolian (Cyrillic script) text to English:");
            sb.AppendLine($"\"{text}\"");
            sb.AppendLine();
            sb.AppendLine("Important:");
            sb.AppendLine("- This is Mongolian text written in Cyrillic alphabet");
            sb.AppendLine("- Provide only the English translation");
            sb.AppendLine("- Do not ask for clarification");
            sb.AppendLine("- If you cannot read the text, just try your best to translate");

            return sb.ToString();
        }

        /// <summary>
        /// ChatGPT API дуудах
        /// </summary>
        private async Task<string> CallChatGptApiAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a professional translator. You can read and translate Mongolian text written in Cyrillic script to English. Always provide a direct translation without asking for clarification. Examples: сургууль = school, ажилтан = employee, нийт = total."
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

            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"🔍 API алдааны дэлгэрэнгүй мэдээлэл:");
                    Console.WriteLine($"   Status Code: {response.StatusCode}");
                    Console.WriteLine($"   Error Response: {errorContent}");
                    
                    throw new HttpRequestException($"API хүсэлт амжилтгүй: {response.StatusCode} - {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                dynamic responseObj = JsonConvert.DeserializeObject(responseJson);

                if (responseObj?.choices == null || responseObj.choices.Count == 0)
                {
                    throw new Exception("ChatGPT хариунаас choices олдсонгүй");
                }

                return responseObj.choices[0].message.content.ToString();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"🌐 HTTP хүсэлтийн алдаа: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔧 Ерөнхий алдаа: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
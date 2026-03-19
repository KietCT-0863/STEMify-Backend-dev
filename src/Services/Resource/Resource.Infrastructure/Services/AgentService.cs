using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Resource.Application.Common.Interfaces;
using Resource.Application.Models.Agent;
using System.Text;

using Content = Resource.Application.Models.Agent.Content;

namespace Resource.Infrastructure.Services
{
    public class AgentService : IAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AgentService> _logger;

        public AgentService(
            HttpClient httpClient,
            ILogger<AgentService> logger,
            IConfiguration config)
        {
            _config = config;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async IAsyncEnumerable<string> AnswerGeneralStemQuestionAsync(string userPrompt)
        {
            var prompt = $@"Người dùng vừa đặt câu hỏi hoặc đưa ra yêu cầu sau:
                ""{userPrompt}""
                Hãy:
                1. Phân tích xem câu hỏi có liên quan đến các chủ đề STEM (khoa học, công nghệ, kỹ thuật, toán học, robot, lập trình, hoặc học tập STEMify) hay không.
                2. Nếu **có liên quan**, hãy:
                   - Giải thích ngắn gọn, dễ hiểu, bằng ngôn ngữ thân thiện như giáo viên hướng dẫn học sinh.
                   - Nếu phù hợp, có thể gợi ý nội dung, bài học hoặc khóa học liên quan trên STEMify (ví dụ: {_config["ClientApp"]}/vi/resource/courses hoặc {_config["ClientApp"]}/vi/resource/lessons).
                   - Giữ câu trả lời ngắn gọn, rõ ràng (không quá 100 từ).
                3. Nếu **không liên quan đến STEM hoặc nền tảng STEMify**, hãy lịch sự từ chối bằng câu:
                   ""Xin lỗi, tôi chỉ hỗ trợ các chủ đề liên quan đến STEM và nền tảng STEMify.""";

            // Use cached system context to reduce latency and cost by 90%
            //var cacheName = await _cacheService.GetOrCreateSystemContextCacheAsync();

            await foreach (var chunk in GenerateStreamingResponseWithSystemInstructionAsync(prompt))
            {
                yield return chunk;
            }
        }

        public async IAsyncEnumerable<string> GenerateCourseRecommendationsAsync(string userPrompt, string courses)
        {
            var prompt = $@"Dựa trên hồ sơ người học STEMify:
                    Kỹ năng và Sở thích: {userPrompt} — [Đây là phần người dùng nhập, hãy xem xét mức độ liên quan đến lĩnh vực STEM]

                    Danh sách các khóa học hiện có: {courses}
                    Hãy xem xét xem người dùng có nhắc đến chủ đề hoặc lĩnh vực STEM cụ thể nào không (ví dụ: robot, lập trình, khoa học, mô phỏng 3D...).

                    Hãy gợi ý các khóa học phù hợp nhất, đảm bảo rằng:
                    1. Phù hợp với trình độ hoặc độ tuổi hiện tại của người học.
                    2. Giúp họ phát triển thêm kiến thức và kỹ năng trong các lĩnh vực STEM (Khoa học, Công nghệ, Kỹ thuật, Toán học).
                    3. Liên quan đến sở thích cá nhân của họ nhưng vẫn nằm trong phạm vi học tập của nền tảng STEMify.
                    4. Đề xuất lộ trình học hợp lý (ví dụ: cơ bản → trung cấp → nâng cao).
                    5. Gợi ý các khóa học phù hợp kèm đường dẫn theo định dạng:
                       {_config["ClientApp"]}/vi/resource/course/courseId — ẩn đường dẫn dài bằng alias là **tên khóa học**.

                    Với mỗi gợi ý, hãy cung cấp:
                    - Tên khóa học (kèm liên kết)
                    - 1–2 câu ngắn gọn giải thích vì sao phù hợp với người học
                    - (Tùy chọn) Gợi ý khóa học tiếp theo nếu có.";

            // Use cached system context
            //var cacheName = await _cacheService.GetOrCreateSystemContextCacheAsync();

            await foreach (var chunk in GenerateStreamingResponseWithSystemInstructionAsync(prompt))
            {
                yield return chunk;
            }
        }

        public async IAsyncEnumerable<string> SummarizeSectionForPresentationAsync(string userPrompt, List<string> sectionContents)
        {
            var sectionsJoined = string.Join("\n\n===========================\n\n", sectionContents.Select((s, i) => $"Section {i + 1}:\n{s}"));
            var prompt = $@"Nhiệm vụ của bạn:
                        - Bạn nhận được nhiều phần nội dung HTML của một bài học STEMify.
                        - Hãy tóm tắt từng phần riêng biệt thành các slide ngắn gọn, rõ ràng và hấp dẫn.
                        - Mỗi slide tương ứng với một phần (section), và trả về dưới dạng danh sách HTML.
                        - Mỗi slide nên có:
                          • Tiêu đề ngắn (dựa theo section nếu có)
                          • Nội dung súc tích, 2–3 đoạn hoặc danh sách bullet
                          • Giọng điệu thân thiện, dễ hiểu cho học sinh tiểu học
                          • Không vượt quá 150 từ mỗi slide
                        - Giữ định dạng HTML gọn gàng, có thể chèn trực tiếp vào trình chiếu web hoặc PowerPoint.

                        Yêu cầu đặc biệt từ người dùng:
                        {userPrompt}

                        Dưới đây là các phần nội dung HTML của bài học:
                        --------------------------
                        {sectionsJoined}
                        --------------------------

                        Trả về danh sách các slide HTML theo dạng mảng JSON, ví dụ:
                        [
                          ""<section>...</section>"",
                          ""<section>...</section>""
                        ]
                        ";

            // Use cached system context
            //var cacheName = await _cacheService.GetOrCreateSystemContextCacheAsync();

            await foreach (var chunk in GenerateStreamingResponseWithSystemInstructionAsync(prompt))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Generates streaming response using Gemini API with cached context
        /// </summary>
        /// <param name="prompt">User prompt (without system context)</param>
        /// <param name="cacheName">Cached content name from Gemini (e.g., "cachedContents/abc123")</param>
        private async IAsyncEnumerable<string> GenerateStreamingResponseAsync(string prompt, string cacheName)
        {
            // Build request with cached content reference
            var request = new
            {
                contents = new[]
                {
                    new Content
                    {
                        parts = new[]
                        {
                            new Part { text = prompt }
                        }
                    }
                },
                cachedContent = cacheName // Reference to cached system instruction
            };

            string jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Use Gemini streaming endpoint with cached content
            var modelName = _config["Gemini:Model"] ?? "gemini-1.5-flash-001";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:streamGenerateContent?key={_config["Gemini:ApiKey"]}&alt=sse";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var jsonData = line.Substring(6); // Remove "data: " prefix
                    if (jsonData == "[DONE]") break;

                    var streamResponse = JsonConvert.DeserializeObject<Application.Models.Agent.ContentResponse.ContentResponse>(jsonData);
                    if (streamResponse?.Candidates?.Length > 0)
                    {
                        var text = streamResponse.Candidates[0]?.Content?.Parts?[0]?.Text;
                        if (!string.IsNullOrEmpty(text))
                        {
                            yield return text;
                        }
                    }
                }
            }
        }

        private async IAsyncEnumerable<string> GenerateStreamingResponseWithSystemInstructionAsync(string prompt)
        {
            const string SYSTEM_INSTRUCTION = @"Bạn là STEMify Assistant — trợ lý ảo chính thức của nền tảng học tập STEMify (https://stemifi.com),
                một website học STEM thế hệ mới kết hợp mô phỏng 3D, lập trình kéo-thả, và lộ trình học cá nhân hóa cho học sinh tiểu học.
                Nhiệm vụ của bạn:
                - Giải thích, hướng dẫn, hoặc hỗ trợ người dùng về các chủ đề thuộc STEM, giáo dục, khoa học, robot, công nghệ, và lập trình.
                - Có thể giới thiệu hoặc mô tả các tính năng của STEMify (ví dụ: mô phỏng 3D, bài học lập trình, khung chương trình STEM, khóa học hoặc nội dung học tập).
                - Nếu người dùng hỏi ngoài phạm vi STEM hoặc không liên quan đến STEMify, hãy lịch sự từ chối bằng câu:
                  ""Xin lỗi, tôi chỉ hỗ trợ các chủ đề liên quan đến STEM và nền tảng STEMify.""
                - Luôn trả lời bằng tiếng Việt, ngắn gọn, dễ hiểu, và không quá 100 từ.
                - Giọng điệu thân thiện, mang phong cách giáo viên hướng dẫn học sinh.";

            var request = new
            {
                contents = new[]
                {
                    new Content
                    {
                        parts = new[] { new Part { text = prompt } }
                    }
                },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = SYSTEM_INSTRUCTION }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2048
                }
            };

            string jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var modelName = _config["Gemini:Model"] ?? "gemini-2.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:streamGenerateContent?key={_config["Gemini:ApiKey"]}&alt=sse";

            _logger?.LogInformation("Gemini API Key: {ApiKey}", _config["Gemini:ApiKey"]);
            _logger?.LogInformation("Gemini Model: {ModelName}", modelName);
            _logger?.LogInformation("Gemini URL: {Url}", url);
            _logger?.LogInformation("Prompt: {Prompt}", prompt);
            _logger?.LogDebug("System Instruction: {SystemInstruction}", SYSTEM_INSTRUCTION);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            _logger?.LogInformation("HTTP Response Status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            var buffer = new StringBuilder();
            int chunkCount = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("data: ")) continue;

                var jsonData = line.Substring(6);
                if (jsonData == "[DONE]")
                {
                    _logger?.LogInformation("Streaming completed. Total chunks: {ChunkCount}", chunkCount);
                    break;
                }

                var streamResponse = JsonConvert.DeserializeObject<Application.Models.Agent.ContentResponse.ContentResponse>(jsonData);
                var text = streamResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;
                if (string.IsNullOrEmpty(text)) continue;

                buffer.Append(text);
                chunkCount++;
                _logger?.LogDebug("Received chunk #{ChunkCount}: {ChunkText}", chunkCount, text);
                yield return text;
            }
            _logger?.LogInformation("Total response length: {Length}", buffer.Length);
        }
    }
}

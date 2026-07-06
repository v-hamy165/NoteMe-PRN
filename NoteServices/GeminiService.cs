using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoteMe.Services
{
    public class MeetingAnalysisResult
    {
        public string Transcript { get; set; } = string.Empty;

        public string MainContent { get; set; } = string.Empty;

        public List<string> CompletedSteps { get; set; } = new List<string>();

        public List<string> NextSteps { get; set; } = new List<string>();
    }

    public class GeminiService
    {
        private const string BaseUrl = "https://generativelanguage.googleapis.com";
        private const long InlineAudioLimitBytes = 15 * 1024 * 1024;
        private const string DefaultModel = "gemini-2.5-flash";

        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        private readonly string apiKey;

        public string Model { get; }

        public GeminiService()
        {
            (apiKey, Model) = LoadConfiguration();
        }

        public async Task<MeetingAnalysisResult> AnalyzeMeetingAudioAsync(string audioFullPath)
        {
            if (!File.Exists(audioFullPath))
            {
                throw new FileNotFoundException(
                    "File ghi âm không còn tồn tại.",
                    audioFullPath
                );
            }

            string prompt =
                "Bạn là thư ký cuộc họp chuyên nghiệp. Hãy nghe file ghi âm cuộc họp đính kèm và thực hiện:\n" +
                "1. transcript: ghi lại toàn bộ lời thoại theo ngôn ngữ gốc trong file.\n" +
                "2. mainContent: tóm tắt nội dung chính của cuộc họp.\n" +
                "3. completedSteps: liệt kê các công việc đã hoàn thành được nhắc đến.\n" +
                "4. nextSteps: liệt kê các bước tiếp theo hoặc công việc cần làm được nhắc đến.\n" +
                "Các phần tóm tắt viết bằng tiếng Việt, ngắn gọn và trung thực với nội dung. " +
                "Không bịa thêm thông tin không có trong ghi âm. " +
                "Nếu file không có giọng nói rõ ràng, đặt mainContent là mô tả vấn đề và để các danh sách rỗng.";

            long fileSize = new FileInfo(audioFullPath).Length;

            object audioPart;

            if (fileSize <= InlineAudioLimitBytes)
            {
                byte[] audioBytes = await File.ReadAllBytesAsync(audioFullPath);

                audioPart = new
                {
                    inlineData = new
                    {
                        mimeType = "audio/wav",
                        data = Convert.ToBase64String(audioBytes)
                    }
                };
            }
            else
            {
                string fileUri = await UploadFileAsync(audioFullPath);

                audioPart = new
                {
                    fileData = new
                    {
                        mimeType = "audio/wav",
                        fileUri
                    }
                };
            }

            return await GenerateAnalysisAsync(new[] { new { text = prompt } as object, audioPart });
        }

        public async Task<MeetingAnalysisResult> SummarizeTextAsync(string title, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Ghi chú không có nội dung để tóm tắt.");
            }

            string prompt =
                "Bạn là trợ lý tóm tắt chuyên nghiệp. Hãy đọc ghi chú dưới đây và thực hiện:\n" +
                "1. mainContent: tóm tắt nội dung chính.\n" +
                "2. completedSteps: liệt kê các công việc đã hoàn thành được nhắc đến.\n" +
                "3. nextSteps: liệt kê các bước tiếp theo hoặc công việc cần làm được nhắc đến.\n" +
                "Để transcript rỗng. Trả lời bằng tiếng Việt, ngắn gọn, không bịa thêm thông tin.\n\n" +
                $"Tiêu đề: {title}\n\nNội dung:\n{content}";

            return await GenerateAnalysisAsync(new object[] { new { text = prompt } });
        }

        private async Task<MeetingAnalysisResult> GenerateAnalysisAsync(object[] parts)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            transcript = new { type = "STRING" },
                            mainContent = new { type = "STRING" },
                            completedSteps = new
                            {
                                type = "ARRAY",
                                items = new { type = "STRING" }
                            },
                            nextSteps = new
                            {
                                type = "ARRAY",
                                items = new { type = "STRING" }
                            }
                        },
                        required = new[] { "mainContent" }
                    }
                }
            };

            string url = $"{BaseUrl}/v1beta/models/{Model}:generateContent";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(BuildApiErrorMessage(response, responseText));
            }

            using JsonDocument document = JsonDocument.Parse(responseText);

            string? resultJson = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(resultJson))
            {
                throw new InvalidOperationException("Gemini trả về kết quả rỗng, vui lòng thử lại.");
            }

            return ParseAnalysisResult(resultJson);
        }

        private async Task<string> UploadFileAsync(string filePath)
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            string displayName = Path.GetFileName(filePath);

            // Buoc 1: khoi tao phien upload resumable de lay upload URL
            using var startRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{BaseUrl}/upload/v1beta/files"
            );

            startRequest.Headers.Add("x-goog-api-key", apiKey);
            startRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            startRequest.Headers.Add("X-Goog-Upload-Command", "start");
            startRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", fileBytes.Length.ToString());
            startRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", "audio/wav");
            startRequest.Content = new StringContent(
                JsonSerializer.Serialize(new { file = new { display_name = displayName } }),
                Encoding.UTF8,
                "application/json"
            );

            using HttpResponseMessage startResponse = await httpClient.SendAsync(startRequest);

            if (!startResponse.IsSuccessStatusCode ||
                !startResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var uploadUrls))
            {
                string startBody = await startResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException(BuildApiErrorMessage(startResponse, startBody));
            }

            string uploadUrl = string.Empty;

            foreach (string value in uploadUrls)
            {
                uploadUrl = value;
                break;
            }

            // Buoc 2: upload noi dung file va finalize
            using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
            uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            uploadRequest.Content = new ByteArrayContent(fileBytes);
            uploadRequest.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/octet-stream");

            using HttpResponseMessage uploadResponse = await httpClient.SendAsync(uploadRequest);
            string uploadBody = await uploadResponse.Content.ReadAsStringAsync();

            if (!uploadResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(BuildApiErrorMessage(uploadResponse, uploadBody));
            }

            string fileUri;
            string fileName;
            string state;

            using (JsonDocument uploadDocument = JsonDocument.Parse(uploadBody))
            {
                JsonElement file = uploadDocument.RootElement.GetProperty("file");

                fileUri = file.GetProperty("uri").GetString() ?? string.Empty;
                fileName = file.GetProperty("name").GetString() ?? string.Empty;
                state = file.GetProperty("state").GetString() ?? string.Empty;
            }

            // Buoc 3: cho Gemini xu ly xong file (audio dai co the mat vai chuc giay)
            DateTime deadline = DateTime.UtcNow.AddMinutes(3);

            while (state == "PROCESSING")
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("Gemini xử lý file audio quá lâu, vui lòng thử lại.");
                }

                await Task.Delay(TimeSpan.FromSeconds(2));

                using var stateRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{BaseUrl}/v1beta/{fileName}"
                );
                stateRequest.Headers.Add("x-goog-api-key", apiKey);

                using HttpResponseMessage stateResponse = await httpClient.SendAsync(stateRequest);
                string stateBody = await stateResponse.Content.ReadAsStringAsync();

                if (!stateResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(BuildApiErrorMessage(stateResponse, stateBody));
                }

                using JsonDocument stateDocument = JsonDocument.Parse(stateBody);
                state = stateDocument.RootElement.GetProperty("state").GetString() ?? string.Empty;
            }

            if (state != "ACTIVE")
            {
                throw new InvalidOperationException(
                    $"Gemini không xử lý được file audio (trạng thái: {state})."
                );
            }

            return fileUri;
        }

        private static MeetingAnalysisResult ParseAnalysisResult(string resultJson)
        {
            using JsonDocument document = JsonDocument.Parse(resultJson);
            JsonElement root = document.RootElement;

            var result = new MeetingAnalysisResult
            {
                Transcript = GetStringOrEmpty(root, "transcript"),
                MainContent = GetStringOrEmpty(root, "mainContent"),
                CompletedSteps = GetStringList(root, "completedSteps"),
                NextSteps = GetStringList(root, "nextSteps")
            };

            if (string.IsNullOrWhiteSpace(result.MainContent))
            {
                throw new InvalidOperationException(
                    "Gemini không trích xuất được nội dung, vui lòng thử lại."
                );
            }

            return result;
        }

        private static string GetStringOrEmpty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static List<string> GetStringList(JsonElement element, string propertyName)
        {
            var items = new List<string>();

            if (element.TryGetProperty(propertyName, out JsonElement property) &&
                property.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in property.EnumerateArray())
                {
                    string? value = item.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        items.Add(value);
                    }
                }
            }

            return items;
        }

        private static string BuildApiErrorMessage(HttpResponseMessage response, string responseBody)
        {
            if ((int)response.StatusCode == 429)
            {
                return "Đã vượt giới hạn gọi Gemini API, vui lòng chờ một lúc rồi thử lại.";
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);

                string? message = document.RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    return $"Gemini API báo lỗi ({(int)response.StatusCode}): {message}";
                }
            }
            catch (Exception)
            {
                // Body khong phai JSON loi chuan cua Google, dung thong bao chung.
            }

            return $"Gemini API báo lỗi ({(int)response.StatusCode}).";
        }

        private static (string apiKey, string model) LoadConfiguration()
        {
            string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            string model = DefaultModel;

            string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            if (File.Exists(settingsPath))
            {
                using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsPath));

                if (settings.RootElement.TryGetProperty("Gemini", out JsonElement gemini))
                {
                    if (string.IsNullOrWhiteSpace(apiKey) &&
                        gemini.TryGetProperty("ApiKey", out JsonElement keyElement))
                    {
                        apiKey = keyElement.GetString();
                    }

                    if (gemini.TryGetProperty("Model", out JsonElement modelElement) &&
                        !string.IsNullOrWhiteSpace(modelElement.GetString()))
                    {
                        model = modelElement.GetString()!;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình Gemini API key. Hãy đặt biến môi trường GEMINI_API_KEY " +
                    "hoặc điền vào mục Gemini:ApiKey trong appsettings.json. " +
                    "Lấy key miễn phí tại https://aistudio.google.com/apikey."
                );
            }

            return (apiKey, model);
        }
    }
}

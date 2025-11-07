using Application.Models;
using Core.Enums;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.AiLayer
{
    public class GeminiAiClient : IAiGenerator
    {
        private readonly HttpClient _http;
        private IReadOnlyDictionary<string, string>? _creds;
        private string _apiKey = string.Empty;
        private string _model = "gemini-2.5-flash";

        public AiProviderType ProviderType => AiProviderType.GoogleVertex;

        public GeminiAiClient(HttpClient http)
        {
            _http = http;
        }

        // 🔹 Credential bilgilerini runtime’da set ediyoruz
        public void Initialize(IReadOnlyDictionary<string, string> creds)
        {
            _creds = creds;
            _apiKey = creds.TryGetValue("apiKey", out var key)
                ? key
                : throw new InvalidOperationException("Gemini apiKey missing");

            if (creds.TryGetValue("model", out var model))
                _model = model;
        }

        public async Task<string> GenerateTextAsync(
            string prompt,
            double temperature = 0.7,
            string? model = null,
            CancellationToken ct = default)
        {
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new { temperature }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{(model ?? _model)}:generateContent?key={_apiKey}";
            var res = await _http.PostAsJsonAsync(url, payload, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gemini API Error ({res.StatusCode}): {json}");

            try
            {
                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("Gemini boş yanıt döndürdü.");

                return StripCodeFences(text).Trim();
            }
            catch (Exception ex)
            {
                throw new Exception($"Gemini response parse failed: {ex.Message}\nRaw: {json[..Math.Min(json.Length, 500)]}");
            }
        }

        // 🔥 Yeni versiyon — TopicGenerationRequest destekli
        public async Task<IReadOnlyList<TopicResult>> GenerateTopicsAsync(
            TopicGenerationRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Gemini client not initialized — missing API key.");

            var fullPrompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}"
                .Trim();

            // 🧩 Ek bağlamı prompt içine dahil et (opsiyonel)
            if (!string.IsNullOrWhiteSpace(request.ProductionType))
                fullPrompt += $"\n\n[ProductionType: {request.ProductionType}]";
            if (!string.IsNullOrWhiteSpace(request.RenderStyle))
                fullPrompt += $"\n[RenderStyle: {request.RenderStyle}]";
            if (!string.IsNullOrWhiteSpace(request.Category))
                fullPrompt += $"\n[Category: {request.Category}]";
            if (!string.IsNullOrWhiteSpace(request.SubCategory))
                fullPrompt += $"\n[SubCategory: {request.SubCategory}]";

            var result = await GenerateTextAsync(
                fullPrompt,
                request.Temperature,
                request.Model ?? _model,
                ct);

            try
            {
                var topics = JsonSerializer.Deserialize<List<TopicResult>>(result,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (topics == null || topics.Count == 0)
                    throw new InvalidOperationException("Gemini boş veya geçersiz TopicResult JSON döndürdü.");

                return topics;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gemini Topic JSON parse failed: {ex.Message}\n\nRaw:\n{result}");
            }
        }

        public Task<byte[]> GenerateImageAsync(string prompt, string size = "1024x1024", string? style = null, CancellationToken ct = default)
            => Task.FromResult(Array.Empty<byte>());

        public Task<float[]> GetEmbeddingAsync(string text, string? model = null, CancellationToken ct = default)
            => Task.FromResult(Array.Empty<float>());

        public Task<bool> TestConnectionAsync(CancellationToken ct = default)
            => Task.FromResult(true);

        private static string StripCodeFences(string s)
        {
            var txt = s.Trim();
            if (txt.StartsWith("```"))
            {
                var i = txt.IndexOf('\n');
                if (i > 0) txt = txt[(i + 1)..];
                if (txt.EndsWith("```"))
                {
                    var last = txt.LastIndexOf("```", StringComparison.Ordinal);
                    if (last >= 0) txt = txt[..last];
                }
            }
            return txt.Trim();
        }
    }
}

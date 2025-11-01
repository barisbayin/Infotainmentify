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
        private string _model = "gemini-1.5-flash";

        public AiProviderType ProviderType => AiProviderType.GoogleVertex;

        // 🔹 HttpClient DI'den geliyor
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

            // 🔥 Gemini API error handling
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gemini API Error ({res.StatusCode}): {json}");

            try
            {
                using var doc = JsonDocument.Parse(json);

                var candidate = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(candidate))
                    throw new InvalidOperationException("Gemini boş yanıt döndürdü.");

                // 🔥 Markdown temizliği
                candidate = StripCodeFences(candidate);

                return candidate.Trim();
            }
            catch (Exception ex)
            {
                throw new Exception($"Gemini response parse failed: {ex.Message}\nRaw: {json[..Math.Min(json.Length, 400)]}");
            }
        }

        public async Task<IReadOnlyList<TopicResult>> GenerateTopicsAsync(
            string systemPrompt,
            string userPrompt,
            int count,
            string? model = null,
            double temperature = 0.7,
            CancellationToken ct = default)
        {
            var fullPrompt = $"{systemPrompt}\n\n{userPrompt}\n\nReturn a valid JSON array of {count} objects.";
            var result = await GenerateTextAsync(fullPrompt, temperature, model ?? _model, ct);

            try
            {
                return JsonSerializer.Deserialize<List<TopicResult>>(result,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
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

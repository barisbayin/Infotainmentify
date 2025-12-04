using Application.AiLayer.Abstract;
using Application.Attributes;
using Application.Models;
using Core.Enums;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.AiLayer.Concrete
{
    [AiProvider(AiProviderType.GoogleAIStudio)]
    public class GoogleAiStudioService : ITextGenerator, IImageGenerator
    {
        private readonly HttpClient _http;
        private string _apiKey = string.Empty;

        public AiProviderType ProviderType => AiProviderType.GoogleAIStudio;

        public GoogleAiStudioService(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromMinutes(3); // Resim üretimi sürebilir
        }

        public void Initialize(string apiKey, string? extraId = null)
        {
            _apiKey = apiKey;
        }

        public Task<bool> TestConnectionAsync(CancellationToken ct = default)
        {
            // Basit bir model listesi çekerek test edilebilir, şimdilik true dönüyoruz.
            return Task.FromResult(!string.IsNullOrEmpty(_apiKey));
        }

        // ============================================================
        // 1. TEXT GENERATION (CORE)
        // ============================================================
        public async Task<string> GenerateTextAsync(string prompt, double temperature = 0.7, string? model = null, CancellationToken ct = default)
        {
            var selectedModel = model ?? "gemini-1.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{selectedModel}:generateContent?key={_apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature,
                    maxOutputTokens = 8192 // Uzun scriptler için limit artırıldı
                }
            };

            var res = await _http.PostAsJsonAsync(url, payload, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Google AI Studio Text Error ({res.StatusCode}): {json}");

            return ExtractTextFromResponse(json);
        }

        // ============================================================
        // 2. IMAGE GENERATION (IMAGEN via AI Studio REST)
        // ============================================================
        public async Task<byte[]> GenerateImageAsync(string prompt, string? negativePrompt, string size = "1080x1920", string? style = null, string? model = null, CancellationToken ct = default)
        {
            // AI Studio'da Imagen kullanımı biraz farklıdır. 
            // Genelde 'imagen-3.0-generate-001' gibi modeller kullanılır.
            var selectedModel = model ?? "imagen-3.0-generate-001";

            // Not: AI Studio'nun Imagen REST API'si bazen :predict kullanır.
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{selectedModel}:generateContent?key={_apiKey}";

            var fullPrompt = string.IsNullOrWhiteSpace(style) ? prompt : $"{prompt}, style: {style}, negative: {negativePrompt}";

            // Aspect Ratio (En boy oranı) mapping
            var aspectRatio = size switch
            {
                "1080x1920" or "9:16" => "9:16",
                "1920x1080" or "16:9" => "16:9",
                _ => "1:1"
            };

            var payload = new
            {
                contents = new[]
                {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                },
                        generationConfig = new
                        {
                            responseModalities = new[] { "IMAGE" },
                            imageConfig = new
                            {
                                aspectRatio = aspectRatio
                            },
                            temperature = 0.8
                        }
            };

            var res = await _http.PostAsJsonAsync(url, payload, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Google AI Studio Image Error ({res.StatusCode}): {json}");

            try
            {
                using var doc = JsonDocument.Parse(json);

                // 🎨 "inline_data.data" veya "inlineData.data" altında base64 görsel olabilir
                string? base64 = null;
                if (doc.RootElement.TryGetProperty("candidates", out var candidates))
                {
                    foreach (var part in candidates[0].GetProperty("content").GetProperty("parts").EnumerateArray())
                    {
                        if (part.TryGetProperty("inline_data", out var idata) && idata.TryGetProperty("data", out var d1))
                            base64 = d1.GetString();
                        else if (part.TryGetProperty("inlineData", out var idata2) && idata2.TryGetProperty("data", out var d2))
                            base64 = d2.GetString();

                        if (!string.IsNullOrWhiteSpace(base64))
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(base64))
                    throw new InvalidOperationException("Gemini image generation boş veya geçersiz yanıt döndürdü.");

                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gemini image parse error: {ex.Message}\nRaw: {json[..Math.Min(json.Length, 600)]}");
            }
        }

        // ============================================================
        // 3. HELPER WRAPPERS (Topic & Script)
        // ============================================================

        public async Task<IReadOnlyList<TopicResult>> GenerateTopicsAsync(TopicGenerationRequest request, CancellationToken ct = default)
        {
            // 1. Prompt'u birleştir
            var fullPrompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}";

            // 2. Text Metodunu Çağır
            var jsonResponse = await GenerateTextAsync(fullPrompt, request.Temperature, request.Model, ct);

            // 3. Temizle ve Deserialize Et
            var cleanJson = StripCodeFences(jsonResponse);

            try
            {
                var topics = JsonSerializer.Deserialize<List<TopicResult>>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return topics ?? new List<TopicResult>();
            }
            catch
            {
                // Eğer liste dönmediyse boş dön (veya logla)
                return new List<TopicResult>();
            }
        }

        public async Task<string> GenerateScriptsAsync(ScriptGenerationRequest request, CancellationToken ct = default)
        {
            var fullPrompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}";
            // Script için ham metin (JSON string) dönüyoruz, executor parse edecek.
            return await GenerateTextAsync(fullPrompt, request.Temperature, request.Model, ct);
        }

        // ============================================================
        // 4. EMBEDDING (Vektör)
        // ============================================================
        public async Task<float[]> GetEmbeddingAsync(string text, string? model = null, CancellationToken ct = default)
        {
            var selectedModel = model ?? "text-embedding-004";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{selectedModel}:embedContent?key={_apiKey}";

            var payload = new
            {
                content = new { parts = new[] { new { text = text } } }
            };

            var res = await _http.PostAsJsonAsync(url, payload, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode) return Array.Empty<float>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                // embedding.values
                if (doc.RootElement.TryGetProperty("embedding", out var embedding) &&
                    embedding.TryGetProperty("values", out var values))
                {
                    return JsonSerializer.Deserialize<float[]>(values.GetRawText()) ?? Array.Empty<float>();
                }
                return Array.Empty<float>();
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private string ExtractTextFromResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "";

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Gemini API response structure: candidates[0].content.parts[0].text
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var content = candidates[0].GetProperty("content");
                    if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString() ?? "";
                    }
                }
                return "";
            }
            catch
            {
                return json; // Hata durumunda ham veriyi dön
            }
        }

        private string StripCodeFences(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```json"))
                text = text.Replace("```json", "").Replace("```", "");
            else if (text.StartsWith("```"))
                text = text.Replace("```", "");

            return text.Trim();
        }
    }
}

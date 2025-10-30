using Application.Models;
using Core.Enums;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.AiLayer
{
    public class OpenAiClient : IAiGenerator
    {
        private readonly HttpClient _http;
        private IReadOnlyDictionary<string, string>? _creds;
        private string _apiKey = string.Empty;
        private string _model = "gpt-4o-mini";

        public AiProviderType ProviderType => AiProviderType.OpenAI;

        // 🔹 HttpClient sadece DI tarafından verilir
        public OpenAiClient(HttpClient http)
        {
            _http = http;
        }

        // 🔹 Credential bilgileri runtime’da set edilir
        public void Initialize(IReadOnlyDictionary<string, string> creds)
        {
            _creds = creds;
            _apiKey = creds.TryGetValue("apiKey", out var key)
                ? key
                : throw new InvalidOperationException("OpenAI apiKey missing");

            if (creds.TryGetValue("model", out var model))
                _model = model;
        }


        // ---------------- TEXT ----------------
        public async Task<string> GenerateTextAsync(
            string prompt,
            double temperature = 0.7,
            string? model = null,
            CancellationToken ct = default)
        {
            var payload = new
            {
                model = model ?? _model,
                temperature,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = JsonContent.Create(payload);

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI Error: {json}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        // ---------------- TOPIC ----------------
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
                throw new InvalidOperationException($"OpenAI Topic JSON parse failed: {ex.Message}\n\nRaw:\n{result}");
            }
        }

        // ---------------- IMAGE ----------------
        public async Task<byte[]> GenerateImageAsync(
            string prompt,
            string size = "1024x1024",
            string? style = null,
            CancellationToken ct = default)
        {
            var payload = new
            {
                model = "gpt-image-1",
                prompt,
                size
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = JsonContent.Create(payload);

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI Image Error: {json}");

            using var doc = JsonDocument.Parse(json);
            var base64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
            return base64 is null ? Array.Empty<byte>() : Convert.FromBase64String(base64);
        }

        // ---------------- EMBEDDING ----------------
        public async Task<float[]> GetEmbeddingAsync(
            string text,
            string? model = null,
            CancellationToken ct = default)
        {
            var payload = new
            {
                model = model ?? "text-embedding-3-small",
                input = text
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = JsonContent.Create(payload);

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI Embedding Error: {json}");

            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding")
                .EnumerateArray().Select(e => e.GetSingle()).ToArray();
            return arr;
        }

        public Task<bool> TestConnectionAsync(CancellationToken ct = default)
            => Task.FromResult(true);
    }


}

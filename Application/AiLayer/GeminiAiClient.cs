using Application.Models;
using Core.Enums;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.TextToSpeech.V1;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Application.AiLayer
{
    public class GeminiAiClient : IAiGenerator
    {
        private readonly HttpClient _http;
        private IReadOnlyDictionary<string, string>? _creds;
        private string _apiKey = string.Empty;
        private string _model = "gemini-2.5-flash";
        private string _credentialJson = string.Empty;

        public AiProviderType ProviderType => AiProviderType.GoogleVertex;

        public GeminiAiClient(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromMinutes(2);
        }

        // 🔹 Credential bilgilerini runtime’da set ediyoruz
        public void Initialize(IReadOnlyDictionary<string, string> creds)
        {
            _creds = creds ?? throw new ArgumentNullException(nameof(creds));

            // 🔹 1. API Key varsa al
            _apiKey = creds.TryGetValue("apiKey", out var key) && !string.IsNullOrWhiteSpace(key)
                ? key
                : null;

            // 🔹 2. Credential JSON varsa al
            if (creds.TryGetValue("credentialJson", out var json) && !string.IsNullOrWhiteSpace(json))
                _credentialJson = json;

            // 🔹 3. Model bilgisi
            if (creds.TryGetValue("model", out var model) && !string.IsNullOrWhiteSpace(model))
                _model = model;

            // 🔸 En az biri dolu olmalı (apiKey veya credentialJson)
            if (string.IsNullOrWhiteSpace(_apiKey) && string.IsNullOrWhiteSpace(_credentialJson))
                throw new InvalidOperationException("AI connection missing authentication data (apiKey or credentialJson).");
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

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromMinutes(2)); // ✅ güvenli timeout

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

        // ------------------------------------------
        // 🧩 Script Generation (Structured JSON)
        // ------------------------------------------
        public async Task<string> GenerateScriptsAsync(
            ScriptGenerationRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Gemini client not initialized — missing API key.");

            var fullPrompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}".Trim();

            // 🧩 Topic bazlı bilgileri bağla
            if (!string.IsNullOrWhiteSpace(request.Premise))
                fullPrompt += $"\nPremise: {request.Premise}";
            if (!string.IsNullOrWhiteSpace(request.Category))
                fullPrompt += $"\nCategory: {request.Category}";
            if (!string.IsNullOrWhiteSpace(request.Tone))
                fullPrompt += $"\nTone: {request.Tone}";
            if (!string.IsNullOrWhiteSpace(request.PotentialVisual))
                fullPrompt += $"\nVisual Hint: {request.PotentialVisual}";

            // Ek metadata
            if (!string.IsNullOrWhiteSpace(request.ProductionType))
                fullPrompt += $"\n[ProductionType: {request.ProductionType}]";
            if (!string.IsNullOrWhiteSpace(request.RenderStyle))
                fullPrompt += $"\n[RenderStyle: {request.RenderStyle}]";

            var result = await GenerateTextAsync(fullPrompt, request.Temperature, request.Model ?? _model, ct);

            try
            {
                //var scripts = JsonSerializer.Deserialize<List<ScriptResult>>(result,
                //    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null || string.IsNullOrEmpty(result))
                    throw new InvalidOperationException("Gemini boş veya geçersiz string döndürdü.");

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gemini Script JSON parse failed: {ex.Message}\n\nRaw:\n{result}");
            }
        }

        public async Task<byte[]> GenerateAudioAsync(
     string text,
     string? voice = null,
     string? model = null,
     string? format = "mp3",
     CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Gemini client not initialized — missing API key.");

            // ---------------------------
            // Model fallback
            // ---------------------------
            string useModel = model ?? "gemini-2.5-flash-tts";

            // ---------------------------
            // Voice fallback
            // ---------------------------
            string useVoice = voice ?? "charon";

            // ---------------------------
            // Format fallback
            // ---------------------------
            string mime = format?.ToLower() == "wav"
                ? "audio/wav"
                : "audio/mp3";

            // ---------------------------
            // Gemini TTS payload
            // ---------------------------
            var payload = new
            {
                contents = new[]
                {
            new {
                role = "user",
                parts = new object[]
                {
                    new { text = text }
                }
            }
        },
                generationConfig = new
                {
                    responseMimeType = mime,
                    voiceName = useVoice
                }
            };

            // ---------------------------
            // Request
            // ---------------------------
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{useModel}:generateContent?key={_apiKey}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Gemini TTS Error ({res.StatusCode}): {json}");

            // ---------------------------
            // Parse audio base64
            // ---------------------------
            try
            {
                using var doc = JsonDocument.Parse(json);

                string? base64 = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("audio")
                    .GetProperty("data")
                    .GetString();

                if (string.IsNullOrWhiteSpace(base64))
                    throw new Exception("Audio data boş.");

                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                throw new Exception($"Gemini TTS parse error: {ex.Message}\nRaw: {json}");
            }
        }




        public async Task<byte[]> GenerateImageAsync(
    string prompt,
    string size = "1080x1920",
    string? style = null,
    string? model = null,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Gemini client not initialized — missing API key.");

            // 🔹 Model fallback
            model ??= _model ?? "gemini-2.5-flash-image";

            // 🔹 Prompt ve stil birleştiriliyor
            var fullPrompt = string.IsNullOrWhiteSpace(style)
                ? prompt.Trim()
                : $"{prompt.Trim()}\n\nStyle: {style.Trim()}";

            // 🔹 Dikey / yatay oran belirle
            var aspect = size switch
            {
                "1080x1920" or "9:16" => "9:16",
                "1920x1080" or "16:9" => "16:9",
                _ => "1:1"
            };

            // 🔹 Request payload (generateContent formatı)
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
                        aspectRatio = aspect
                    },
                    temperature = 0.7
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(5)); // Görsel üretimi uzun sürebilir

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", _apiKey);
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, cts.Token);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gemini Image API Error ({res.StatusCode}): {json}");

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

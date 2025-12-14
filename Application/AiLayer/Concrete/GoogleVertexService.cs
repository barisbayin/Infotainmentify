using Application.AiLayer.Abstract;
using Application.Attributes;
using Application.Models;
using Core.Enums;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V2;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Application.AiLayer.Concrete
{
    [AiProvider(AiProviderType.GoogleVertex)] // 👈 Fabrika bunu buradan tanıyacak
    public class GoogleVertexService : ITextGenerator, IImageGenerator, ITtsGenerator, ISttGenerator
    {
        private readonly HttpClient _http;

        // Google Configs
        private string _serviceAccountJson = string.Empty; // API Key yerine JSON tutuyoruz
        private string _googleProjectId = string.Empty;
        private string _geminiModel = "gemini-2.5-flash"; // Varsayılan model

        // gRPC Clients
        private TextToSpeechClient? _ttsClient;
        private SpeechClient? _speechClient;

        public AiProviderType ProviderType => AiProviderType.GoogleVertex;

        public GoogleVertexService(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromMinutes(5); // Video/Resim işlemleri için uzun süre
        }

        // =================================================================
        // 1. INITIALIZE (Kimlik Doğrulama)
        // =================================================================
        public void Initialize(string apiKey, string? extraId = null)
        {
            // Google için:
            // apiKey => Service Account JSON içeriğinin tamamı (EncryptedApiKey'den gelir)
            // extraId => Google Project ID (UserAiConnection.ExtraId'den gelir)

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "Google Service Account JSON boş olamaz.");

            _serviceAccountJson = apiKey;
            _googleProjectId = extraId ?? ExtractProjectIdFromJson(_serviceAccountJson);

            if (string.IsNullOrWhiteSpace(_googleProjectId))
                throw new InvalidOperationException("Google Project ID bulunamadı. Lütfen bağlantı ayarlarında 'ExtraId' alanını doldurun veya JSON dosyasını kontrol edin.");

            // -------------------------------------------------------------
            // gRPC Credentials Oluşturma
            // -------------------------------------------------------------
            var credential = GoogleCredential.FromJson(_serviceAccountJson);

            // TTS Client Build
            _ttsClient = new TextToSpeechClientBuilder
            {
                Credential = credential.CreateScoped(TextToSpeechClient.DefaultScopes)
            }.Build();

            // STT Client Build
            _speechClient = new SpeechClientBuilder
            {
                Credential = credential.CreateScoped(SpeechClient.DefaultScopes)
            }.Build();
        }

        public Task<bool> TestConnectionAsync(CancellationToken ct = default)
        {
            // Basit bir ping atılabilir veya client'ın null olup olmadığına bakılır
            return Task.FromResult(_ttsClient != null && _speechClient != null);
        }

        // =================================================================
        // 2. TEXT GENERATION (Gemini REST API)
        // =================================================================
        public async Task<string> GenerateTextAsync(string prompt, double temperature = 0.7, string? model = null, CancellationToken ct = default)
        {
            var accessToken = await GetAccessTokenAsync(ct);
            var selectedModel = model ?? _geminiModel;

            var url = $"https://us-central1-aiplatform.googleapis.com/v1/projects/{_googleProjectId}/locations/us-central1/publishers/google/models/{selectedModel}:streamGenerateContent";

            var payload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = prompt } } }
                },
                generationConfig = new { temperature, maxOutputTokens = 65535 }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            req.Content = JsonContent.Create(payload);

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Gemini Text Error ({res.StatusCode}): {json}");

            // Not: Stream response parse etmek biraz daha karışıktır, burada basitleştirilmiş hali var.
            // Gerçek implementasyonda tam JSON parse yapmalısın.
            return ExtractTextFromGeminiResponse(json);
        }

        public async Task<IReadOnlyList<TopicResult>> GenerateTopicsAsync(TopicGenerationRequest request, CancellationToken ct = default)
        {
            // Prompt hazırlama mantığı aynen korunuyor
            var fullPrompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}";
            // ... (Ek parametreler) ...

            var jsonResponse = await GenerateTextAsync(fullPrompt, request.Temperature, request.Model, ct);

            // JSON Temizleme ve Deserialize
            var cleanJson = StripCodeFences(jsonResponse);
            return JsonSerializer.Deserialize<List<TopicResult>>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<TopicResult>();
        }

        public async Task<string> GenerateScriptsAsync(ScriptGenerationRequest request, CancellationToken ct = default)
        {
            var fullPrompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}";
            // ... (Ek parametreler) ...
            return await GenerateTextAsync(fullPrompt, request.Temperature, request.Model, ct);
        }

        public Task<float[]> GetEmbeddingAsync(string text, string? model = null, CancellationToken ct = default)
        {
            // Google Embedding API implementasyonu buraya gelebilir
            return Task.FromResult(Array.Empty<float>());
        }

        // =================================================================
        // 3. IMAGE GENERATION (Imagen on Vertex AI)
        // =================================================================
        public async Task<byte[]> GenerateImageAsync(
            string prompt,
            string? negativePrompt,
            string size = "1080x1920",
            string? style = null,
            string? model = null,
            CancellationToken ct = default)
        {
            var accessToken = await GetAccessTokenAsync(ct);

            // Varsayılan olarak Imagen 3 veya Imagen 2 modelini kullan
            var selectedModel = model ?? "imagegeneration@006";

            // DİKKAT: Eğer kullanıcı yanlışlıkla "gemini" modelini seçtiyse, 
            // burada manuel olarak düzeltelim ki hata almasın.
            if (selectedModel.ToLower().Contains("gemini"))
            {
                // Fallback: Gemini resim çizemez, Imagen'e yönlendir.
                selectedModel = "imagegeneration@006";
            }

            // Imagen Endpoint'i (:predict ile biter)
            var url = $"https://us-central1-aiplatform.googleapis.com/v1/projects/{_googleProjectId}/locations/us-central1/publishers/google/models/{selectedModel}:predict";

            // Prompt Hazırlığı
            var finalPrompt = string.IsNullOrWhiteSpace(style) ? prompt : $"{prompt}, style: {style}";

            var safetySettings = new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_ONLY_HIGH" },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_ONLY_HIGH" },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_ONLY_HIGH" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_ONLY_HIGH" }
            };

            // Vertex AI Imagen Payload Yapısı
            var payload = new
            {
                instances = new[]
                {
                    new { prompt = finalPrompt }
                },
                parameters = new
                {
                    sampleCount = 1,
                    // Aspect Ratio formatı (1:1, 9:16, 16:9)
                    aspectRatio = size == "1080x1920" ? "9:16" : (size == "1920x1080" ? "16:9" : "1:1"),
                    // negativePrompt parametresi her modelde olmayabilir ama deneriz
                    negativePrompt = negativePrompt
                },
                safetySettings = safetySettings
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            req.Content = JsonContent.Create(payload);

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Google Imagen Error ({res.StatusCode}): {json}");
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 1. ÖNCE GÜVENLİK/SANSÜR KONTROLÜ (Gemini/Imagen 3 Yapısı)
                // "candidates" dizisi var mı diye güvenli bir şekilde (TryGetProperty) bakıyoruz.
                if (root.TryGetProperty("candidates", out JsonElement candidates) &&
                    candidates.ValueKind == JsonValueKind.Array &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];

                    // "finishReason" var mı? Varsa değeri "NO_IMAGE" mi?
                    if (firstCandidate.TryGetProperty("finishReason", out JsonElement finishReason))
                    {
                        string reasonStr = finishReason.GetString();
                        if (reasonStr == "NO_IMAGE" || reasonStr == "SAFETY")
                        {
                            // 🔥 YAKALADIK! Sansüre takıldı.
                            throw new Exception($"Google Safety Filter Triggered. FinishReason: {reasonStr}. Prompt sansüre takıldı.");
                        }
                    }
                }

                // 2. RESİM VERİSİNİ ALMA (Imagen 2 / Legacy Yapısı)
                // Kod buraya geldiyse sansür yok demektir. Şimdi "predictions" arayalım.
                string base64 = null;

                if (root.TryGetProperty("predictions", out JsonElement predictions) &&
                    predictions.ValueKind == JsonValueKind.Array &&
                    predictions.GetArrayLength() > 0)
                {
                    // predictions[0].bytesBase64Encoded var mı?
                    if (predictions[0].TryGetProperty("bytesBase64Encoded", out JsonElement bytesElement))
                    {
                        base64 = bytesElement.GetString();
                    }
                }

                // 3. SON KONTROL
                if (string.IsNullOrWhiteSpace(base64))
                {
                    // Hem sansür hatası yok hem de resim verisi yoksa, JSON yapısı beklenmedik bir şekildedir.
                    throw new Exception("Google Imagen boş veri döndürdü veya bilinmeyen bir format algılandı.");
                }

                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                // Hata detayını ve gelen JSON'ı logluyoruz ki ne olduğunu görelim
                throw new Exception($"Google Imagen response parse hatası: {ex.Message} \nRaw Response: {json}");
            }
        }

        // =================================================================
        // 4. TTS GENERATION (Google Cloud TTS - gRPC)
        // =================================================================
        public async Task<byte[]> GenerateAudioAsync(
            string text,
            string voiceName,
            string languageCode,
            string modelName,
            string ratePercent,
            string pitchString,
            string audioEncoding = "MP3",
            CancellationToken ct = default)
        {
            if (_ttsClient == null)
                throw new InvalidOperationException("Google TTS Client başlatılamadı.");

            // 🕵️‍♂️ 1. SES TİPİ TESPİTİ
            // Journey sesleri çok hassastır. SSML, Hız ve Pitch ayarlarını sevmezler.
            bool isJourney = voiceName.Contains("Journey", StringComparison.OrdinalIgnoreCase) ||
                             voiceName.Contains("Generative", StringComparison.OrdinalIgnoreCase);

            // 🧹 2. METİN HAZIRLIĞI
            var processedText = text.Trim();
            SynthesisInput input;

            if (isJourney)
            {
                // 🔥 JOURNEY İÇİN GÜVENLİ MOD:
                // SSML YOK. Break tagleri YOK. Sadece saf metin.
                // Eğer metin <speak> ile başlıyorsa temizle.
                if (processedText.StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
                {
                    // Basitçe tagleri söküyoruz (Regex veya substring ile daha temiz yapılabilir ama şimdilik yeterli)
                    processedText = processedText.Replace("<speak>", "").Replace("</speak>", "").Replace("<break time=\"250ms\"/>", "");
                }

                // Journey düz metin sever
                input = new SynthesisInput { Text = processedText };
            }
            else
            {
                // STANDART SESLER (Neural2, Wavenet):
                // Nefes payı (SSML) ekleyebiliriz.
                if (!processedText.StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
                {
                    var safeText = System.Security.SecurityElement.Escape(processedText);
                    processedText = $@"<speak><break time=""250ms""/>{safeText}</speak>";
                }
                input = new SynthesisInput { Ssml = processedText };
            }

            // ⚙️ 3. AYARLAR
            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = languageCode,
                Name = voiceName
            };

            var audioConfig = new AudioConfig
            {
                AudioEncoding = audioEncoding.ToUpper() == "WAV" ? AudioEncoding.Linear16 : AudioEncoding.Mp3,
                // Journey ise Hız: 1.0, Pitch: 0.0 ZORUNLU. Değilse ayarlardan gelen.
                SpeakingRate = isJourney ? 1.0 : ParseDouble(ratePercent, 1.0),
                Pitch = isJourney ? 0.0 : ParseDouble(pitchString, 0.0)
            };

            // 🚀 4. GÖNDERİM
            try
            {
                var response = await _ttsClient.SynthesizeSpeechAsync(input, voiceSelection, audioConfig, cancellationToken: ct);
                return response.AudioContent.ToByteArray();
            }
            catch (Google.GoogleApiException ex)
            {
                // Hatayı detaylandırıp fırlat ki tam sebebini görelim
                var errorMsg = $"Google TTS Hatası ({ex.HttpStatusCode}): {ex.Message} | Voice: {voiceName} | Mode: {(isJourney ? "Journey" : "Standard")}";
                Console.WriteLine(errorMsg);
                throw new Exception(errorMsg);
            }
        }

        // Helper: Güvenli Double Parse
        private double ParseDouble(string value, double defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            // "%10" gibi değerleri temizle
            value = value.Replace("%", "").Trim();

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        // =================================================================
        // 5. STT GENERATION (Google Cloud Speech V2 - gRPC)
        // =================================================================
        public async Task<SpeechToTextResult> SpeechToTextAsync(byte[] audioData, string languageCode = "en-US", string? model = null, CancellationToken ct = default)
        {
            if (_speechClient == null) throw new InvalidOperationException("Google Speech Client başlatılmadı.");

            var recognizerId = model ?? "long"; // "long" veya "short" recognizer
            var recognizerPath = $"projects/{_googleProjectId}/locations/global/recognizers/_"; // Veya spesifik bir recognizer ID

            var request = new RecognizeRequest
            {
                Recognizer = recognizerPath,
                Config = new RecognitionConfig
                {
                    AutoDecodingConfig = new AutoDetectDecodingConfig(),
                    LanguageCodes = { languageCode },
                    Model = recognizerId,
                    Features = new RecognitionFeatures
                    {
                        EnableWordTimeOffsets = true, // Kelime zamanlaması için şart
                        EnableWordConfidence = true
                    }
                },
                Content = ByteString.CopyFrom(audioData)
            };

            var response = await _speechClient.RecognizeAsync(request, cancellationToken: ct);

            // Sonucu bizim SpeechToTextResult formatına çevir
            return ParseGoogleSttResponse(response);
        }

        // =================================================================
        // HELPERS
        // =================================================================

        // Google Auth Token alır (Service Account JSON'dan)
        private async Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            var cred = GoogleCredential.FromJson(_serviceAccountJson)
                .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
            var token = await cred.UnderlyingCredential.GetAccessTokenForRequestAsync(null, ct);
            return token;
        }

        private string ExtractProjectIdFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("project_id", out var pid))
                    return pid.GetString() ?? string.Empty;
            }
            catch { }
            return string.Empty;
        }

        private double ParsePercentage(string input)
        {
            if (string.IsNullOrEmpty(input)) return 1.0;
            // "10%" -> 1.10, "-10%" -> 0.90 mantığı veya direkt "1.2"
            // Basit implementasyon:
            return double.TryParse(input.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var val)
                ? (input.Contains("%") ? 1 + val / 100 : val)
                : 1.0;
        }

        private double ParsePitch(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0.0;
            return double.TryParse(input.Replace("Hz", "").Replace("st", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0.0;
        }

        private SpeechToTextResult ParseGoogleSttResponse(RecognizeResponse response)
        {
            var result = new SpeechToTextResult { Transcript = "", Words = new List<WordTimestamp>() };

            // Sonuç boşsa dön
            if (response.Results.Count == 0) return result;

            var sb = new StringBuilder();
            foreach (var res in response.Results)
            {
                var alt = res.Alternatives.FirstOrDefault();
                if (alt == null) continue;

                sb.Append(alt.Transcript + " ");

                foreach (var w in alt.Words)
                {
                    result.Words.Add(new WordTimestamp
                    {
                        Word = w.Word,
                        // 🔥 DÜZELTME: Doğrudan .ToTimeSpan() yerine güvenli metodu kullanıyoruz
                        Start = SafeSeconds(w.StartOffset),
                        End = SafeSeconds(w.EndOffset),
                        Confidence = w.Confidence
                    });
                }
            }
            result.Transcript = sb.ToString().Trim();
            return result;
        }

        // 🔥 YENİ HELPER METOD (Null Kontrolü)
        private double SafeSeconds(Google.Protobuf.WellKnownTypes.Duration? duration)
        {
            if (duration == null) return 0.0;
            return duration.ToTimeSpan().TotalSeconds;
        }

        private string StripCodeFences(string text)
        {
            // Markdown ```json temizliği
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                var index = text.IndexOf('\n');
                if (index > -1) text = text.Substring(index + 1);
                if (text.EndsWith("```")) text = text.Substring(0, text.Length - 3);
            }
            return text.Trim();
        }

        private string ExtractTextFromGeminiResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // SENARYO 1: Streaming Response (JSON Array [...])
                // Cevap parçalı geliyorsa hepsini birleştirmemiz lazım.
                if (root.ValueKind == JsonValueKind.Array)
                {
                    var fullTextBuilder = new StringBuilder();
                    foreach (var chunk in root.EnumerateArray())
                    {
                        fullTextBuilder.Append(ExtractTextFromSingleCandidate(chunk));
                    }
                    return fullTextBuilder.ToString();
                }
                // SENARYO 2: Unary Response (JSON Object {...})
                // Cevap tek parça geliyorsa direkt alıyoruz.
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    return ExtractTextFromSingleCandidate(root);
                }

                // Tanımsız format
                return json;
            }
            catch
            {
                // JSON bozuksa veya yapı çok farklıysa ham veriyi dön (Debug için hayat kurtarır)
                return json;
            }
        }

        /// <summary>
        /// Tek bir Candidate objesinin içindeki 'text' alanını güvenli bir şekilde çeker.
        /// Path: candidates[0] -> content -> parts[*] -> text
        /// </summary>
        private string ExtractTextFromSingleCandidate(JsonElement element)
        {
            // 1. "candidates" dizisi var mı?
            if (element.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];

                // 2. "content" ve "parts" var mı?
                if (firstCandidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array)
                {
                    var textBuilder = new StringBuilder();

                    // 3. "parts" içindeki tüm metinleri topla
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var textElement))
                        {
                            textBuilder.Append(textElement.GetString());
                        }
                    }
                    return textBuilder.ToString();
                }
            }

            return string.Empty;
        }
    }
}

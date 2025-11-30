using Application.AiLayer.Abstract;
using Application.Models;
using Core.Enums;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V2;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Globalization;
using System.Net.Http;
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
        private string _googleProjectId = string.Empty;

        // ✔ TTS – Google Cloud
        private TextToSpeechClient? _ttsClient;

        // ✔ STT – Google Cloud
        private SpeechClient? _speechClient;

        public AiProviderType ProviderType => AiProviderType.GoogleVertex;

        public GeminiAiClient(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromMinutes(2);
        }

        public void Initialize(IReadOnlyDictionary<string, string> creds)
        {
            _creds = creds ?? throw new ArgumentNullException(nameof(creds));

            if (creds.TryGetValue("apiKey", out var key))
                _apiKey = key;

            if (creds.TryGetValue("credentialJson", out var json))
                _credentialJson = json;

            if (creds.TryGetValue("model", out var m) && !string.IsNullOrWhiteSpace(m))
                _model = m;

            if (creds.TryGetValue("googleProjectId", out var projectId) && !string.IsNullOrWhiteSpace(projectId))
                _googleProjectId = projectId;

            // ============================================================
            // 1) GOOGLE CLOUD TTS CLIENT
            // ============================================================
            GoogleCredential ttsCredential;

            if (!string.IsNullOrWhiteSpace(_credentialJson))
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(_credentialJson));
                ttsCredential = GoogleCredential.FromStream(ms)
                    .CreateScoped(TextToSpeechClient.DefaultScopes);
            }
            else if (creds.TryGetValue("credentialFilePath", out var filePath) &&
                     File.Exists(filePath))
            {
                ttsCredential = GoogleCredential.FromFile(filePath)
                    .CreateScoped(TextToSpeechClient.DefaultScopes);
            }
            else
            {
                throw new InvalidOperationException("Google TTS: Credential JSON or credentialFilePath missing.");
            }

            _ttsClient = new TextToSpeechClientBuilder
            {
                Credential = ttsCredential
            }.Build();


            // ============================================================
            // 2) GOOGLE CLOUD SPEECH-TO-TEXT
            // ============================================================
            GoogleCredential sttCredential;

            if (!string.IsNullOrWhiteSpace(_credentialJson))
            {
                using var msStt = new MemoryStream(Encoding.UTF8.GetBytes(_credentialJson));
                sttCredential = GoogleCredential.FromStream(msStt)
                    .CreateScoped(SpeechClient.DefaultScopes);
            }
            else if (creds.TryGetValue("credentialFilePath", out var sttFilePath) &&
                     File.Exists(sttFilePath))
            {
                sttCredential = GoogleCredential.FromFile(sttFilePath)
                    .CreateScoped(SpeechClient.DefaultScopes);
            }
            else
            {
                throw new InvalidOperationException("Google STT: Credential JSON or credentialFilePath missing.");
            }

            var request = new RecognizeRequest
            {
                // ARTIK PROJECT ID ELİMİZDE:
                Recognizer = $"projects/{_googleProjectId}/locations/global/recognizers/_",

                Config = new RecognitionConfig
                {
                    // ... diğer ayarlar
                }
                // ...
            };

            _speechClient = new SpeechClientBuilder
            {
                Credential = sttCredential
            }.Build();
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



      //  public async Task<byte[]> GenerateAudioAsync(
      //string text,
      //string voiceName,
      //string languageCode,
      //string modelName,     // IGNORE for Google TTS – kept for compatibility
      //string ratePercent,
      //string pitchString,
      //string audioEncoding = "MP3",
      //CancellationToken ct = default)
      //  {
      //      if (_ttsClient == null)
      //          throw new InvalidOperationException("Google TTS client is not initialized.");

      //      // 1) Speaking Rate
      //      float speakingRate = 1.0f;
      //      if (!string.IsNullOrWhiteSpace(ratePercent) && ratePercent.EndsWith("%"))
      //      {
      //          if (float.TryParse(ratePercent.Replace("%", ""), out var p))
      //              speakingRate = 1.0f + (p / 100f);
      //      }

      //      // 2) Pitch
      //      float pitch = 0f;
      //      if (!string.IsNullOrWhiteSpace(pitchString) && pitchString.EndsWith("Hz"))
      //      {
      //          if (float.TryParse(pitchString.Replace("Hz", "").Replace("+", ""), out var hz))
      //              pitch = hz;
      //      }

      //      // 3) Audio format
      //      AudioEncoding encoding = audioEncoding.ToLower() switch
      //      {
      //          "wav" => AudioEncoding.Linear16,
      //          _ => AudioEncoding.Mp3
      //      };

      //      // 4) FINAL TTS REQUEST (Google Cloud Compatible)
      //      var request = new SynthesizeSpeechRequest
      //      {
      //          Input = new SynthesisInput { Text = text },

      //          Voice = new VoiceSelectionParams
      //          {
      //              LanguageCode = languageCode,
      //              Name = voiceName   // ✔ Google-Cloud voice
      //                                 // ❌ ModelName REMOVED (Gemini paramı)
      //          },

      //          AudioConfig = new AudioConfig
      //          {
      //              AudioEncoding = encoding,
      //              SpeakingRate = speakingRate,
      //              Pitch = pitch,
      //              VolumeGainDb = 0
      //          }
      //      };

      //      var response = await _ttsClient.SynthesizeSpeechAsync(request, cancellationToken: ct);
      //      return response.AudioContent.ToByteArray();
      //  }



        public async Task<byte[]> GenerateAudioAsync(
            string text,
            string voiceName,    // Örn: "en-US-Journey-D" (En kalitelisi bu)
            string languageCode, // Örn: "en-US"
            string modelName,    // Google için bunu kullanmıyoruz, imzayı bozmamak için bıraktık.
            string ratePercent,  // Örn: "1.0", "1.2" veya "10%"
            string pitchString,  // Örn: "0", "-2.0" veya "500Hz"
            string audioEncoding = "MP3",
            CancellationToken ct = default)
        {
            if (_ttsClient == null)
                throw new InvalidOperationException("Google TTS client is not initialized.");

            // 1) INPUT TİPİNİ BELİRLEME (SSML vs TEXT)
            // Eğer metin <speak> ile başlıyorsa SSML olarak, yoksa Text olarak set ediyoruz.
            // Bu sayede Journey seslerine düz metin yolladığında "Text" modunda çalışıp en doğal halini verir.
            var synthesisInput = new SynthesisInput();
            if (!string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
            {
                synthesisInput.Ssml = text;
            }
            else
            {
                synthesisInput.Text = text;
            }

            // 2) SPEAKING RATE (HIZ) PARSING
            // Google 0.25 ile 4.0 arası değer bekler. 1.0 normal hızdır.
            // Kültür bağımsız (InvariantCulture) parse ediyoruz ki sunucu Türkçe olsa bile "1.5" patlamasın.
            double speakingRate = 1.0;
            if (!string.IsNullOrWhiteSpace(ratePercent))
            {
                var cleanRate = ratePercent.Replace("%", "").Trim();
                if (double.TryParse(cleanRate, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                {
                    // Eğer % işareti varsa (örn: "50%") --> 1.5 yapar. Yoksa direkt değeri alır.
                    speakingRate = ratePercent.Contains("%") ? 1.0 + (p / 100.0) : p;
                }
            }

            // 3) PITCH (TON) PARSING
            // Google genelde semitone (+2st) veya Hz bekler ama client library double ister.
            // Journey seslerinde burayı "0" veya boş bırakmak en doğal sonucu verir.
            double pitch = 0;
            if (!string.IsNullOrWhiteSpace(pitchString))
            {
                var cleanPitch = pitchString.Replace("Hz", "").Replace("st", "").Replace("+", "").Trim();
                if (double.TryParse(cleanPitch, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPitch))
                {
                    pitch = parsedPitch;
                }
            }

            // 4) AUDIO ENCODING
            AudioEncoding encoding = audioEncoding.ToLowerInvariant() switch
            {
                "wav" => AudioEncoding.Linear16,
                "ogg" => AudioEncoding.OggOpus,
                "mulaw" => AudioEncoding.Mulaw,
                _ => AudioEncoding.Mp3
            };

            // 5) REQUEST OLUŞTURMA
            var request = new SynthesizeSpeechRequest
            {
                Input = synthesisInput,

                // VoiceSelectionParams: Journey sesleri için sadece Name ve LanguageCode yeterlidir.
                Voice = new VoiceSelectionParams
                {
                    LanguageCode = languageCode,
                    Name = voiceName
                },

                AudioConfig = new AudioConfig
                {
                    AudioEncoding = encoding,
                    SpeakingRate = speakingRate,
                    Pitch = pitch,
                    VolumeGainDb = 0
                    // Efekt profili istersen (örn: kulaklık için) burayı açabilirsin:
                    // EffectsProfileId = { "headphone-class-device" } 
                }
            };

            // 6) ÇAĞRI VE DÖNÜŞ
            var response = await _ttsClient.SynthesizeSpeechAsync(request, cancellationToken: ct);
            return response.AudioContent.ToByteArray();
        }


        //  public async Task<byte[]> GenerateAudioAsync(
        //string text,
        //string voiceName,
        //string languageCode,
        //string modelName,
        //string ratePercent,   // e.g. "-5%" or "+10%"
        //string pitchString,   // e.g. "+0Hz" or "+50Hz"
        //string audioEncoding = "MP3",
        //CancellationToken ct = default)
        //  {
        //      if (_ttsClient == null)
        //          throw new InvalidOperationException("GeminiAiClient: TTS client is not initialized.");

        //      // --- 1) Speaking rate parse ---
        //      float speakingRate = 1.0f;
        //      if (!string.IsNullOrWhiteSpace(ratePercent) && ratePercent.EndsWith("%"))
        //      {
        //          if (float.TryParse(ratePercent.Replace("%", ""), out var p))
        //              speakingRate = 1.0f + (p / 100f);
        //      }

        //      // --- 2) Pitch parse ---
        //      float pitch = 0f;
        //      if (!string.IsNullOrWhiteSpace(pitchString) && pitchString.EndsWith("Hz"))
        //      {
        //          if (float.TryParse(pitchString.Replace("Hz", "").Replace("+", ""), out var hz))
        //              pitch = hz;
        //      }

        //      // --- 3) Encoding seçimi ---
        //      AudioEncoding encoding = audioEncoding.ToLower() switch
        //      {
        //          "wav" => AudioEncoding.Linear16,
        //          _ => AudioEncoding.Mp3
        //      };

        //      var request = new SynthesizeSpeechRequest
        //      {
        //          Input = new SynthesisInput { Text = text },

        //          Voice = new VoiceSelectionParams
        //          {
        //              LanguageCode = languageCode,
        //              Name = voiceName,
        //              ModelName = modelName
        //          },

        //          AudioConfig = new AudioConfig
        //          {
        //              AudioEncoding = encoding,
        //              SpeakingRate = speakingRate, // ✔ hız artık aktif
        //              Pitch = pitch,               // ✔ pitch artık aktif
        //              VolumeGainDb = 0
        //          }
        //      };

        //      var response = await _ttsClient.SynthesizeSpeechAsync(request, cancellationToken: ct);
        //      return response.AudioContent.ToByteArray();
        //  }


        public async Task<SpeechToTextResult> SpeechToTextAsync(
          byte[] audioData,
          string languageCode = "en-US",
          string? model = null,
          CancellationToken ct = default)
        {
            if (_speechClient == null)
                throw new InvalidOperationException("GeminiAiClient: STT V2 client is not initialized.");

            string sttModel = model ?? "long";

            // 🔥 ZORUNLU – recognizer path
            string recognizerPath = $"projects/{_googleProjectId}/locations/global/recognizers/_";

            var request = new RecognizeRequest
            {
                Recognizer = recognizerPath,

                Config = new RecognitionConfig
                {
                    AutoDecodingConfig = new AutoDetectDecodingConfig(),
                    LanguageCodes = { languageCode },
                    Model = sttModel,

                    Features = new RecognitionFeatures
                    {
                        EnableWordTimeOffsets = true,
                        EnableWordConfidence = true
                    }
                },

                Content = ByteString.CopyFrom(audioData)
            };

            RecognizeResponse response;
            try
            {
                response = await _speechClient.RecognizeAsync(request, cancellationToken: ct);
            }
            catch (RpcException ex)
            {
                Console.WriteLine("STT ERROR: " + ex);
                throw;
            }

            if (response.Results.Count == 0)
            {
                return new SpeechToTextResult
                {
                    Transcript = "",
                    Words = new List<WordTimestamp>()
                };
            }

            var words = new List<WordTimestamp>();
            var sb = new System.Text.StringBuilder();

            foreach (var result in response.Results)
            {
                var alt = result.Alternatives.FirstOrDefault();
                if (alt == null) continue;

                if (sb.Length > 0) sb.Append(" ");
                sb.Append(alt.Transcript);

                foreach (var w in alt.Words)
                {
                    words.Add(new WordTimestamp
                    {
                        Word = w.Word,
                        Start = ToSeconds(w.StartOffset),
                        End = ToSeconds(w.EndOffset),
                        Confidence = w.Confidence
                    });
                }
            }

            return new SpeechToTextResult
            {
                Transcript = sb.ToString(),
                Words = words.OrderBy(x => x.Start).ToList()
            };
        }

        private static double ToSeconds(Duration? d)
        {
            if (d == null)
                return 0;

            return d.Seconds + (d.Nanos / 1_000_000_000.0);
        }


        /*
        public async Task<byte[]> GenerateAudioAsync(
     string text,
     string voiceName = "Achird",
     string languageCode = "en-US",
     string model = "models/gemini-2.5-flash-lite-preview-tts",
     string audioEncoding = "MP3",
     CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Gemini API key missing");

            // FE model short-name gönderiyor → burada normalize ediyoruz
            string fullModelName = model.StartsWith("models/")
                ? model
                : $"models/{model}";

            string url = $"https://texttospeech.googleapis.com/v1beta1/text:synthesize?key={_apiKey}";

            var payload = new
            {
                audioConfig = new
                {
                    audioEncoding = "MP3"   // MP3 default
                },
                input = new
                {
                    text = text
                },
                voice = new
                {
                    languageCode = languageCode,
                    modelName = fullModelName,
                    name = voiceName
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Gemini TTS Error ({res.StatusCode}): {json}");

            using var doc = JsonDocument.Parse(json);
            string base64 = doc.RootElement.GetProperty("audioContent").GetString()!;

            return Convert.FromBase64String(base64);
        }

        */

        public async Task<byte[]> GenerateImageAsync(
    string prompt,
    string? negativePrompt,
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

            fullPrompt = fullPrompt + " NEGATIVE: [" + (negativePrompt ?? "None") + "]";

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

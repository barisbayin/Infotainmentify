using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Enums;
using System.Reflection;

namespace Application.Executors.Mock
{
    //[StageExecutor(StageType.Script)]
    public class MockScriptExecutor : BaseStageExecutor
    {
        public MockScriptExecutor(IServiceProvider sp) : base(sp) { }

        public override StageType StageType => StageType.Script;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            CancellationToken ct)
        {
            exec.AddLog("Mock Script: Önceki adımın verisi aranıyor...");

            // 🔥 DÜZELTME: Veriyi "object" olarak alıp Reflection ile okuyalım.
            string title = "Bilinmeyen Konu";

            try
            {
                // Context'ten ham objeyi al (Topic stage ne ürettiyse)
                // MockTopicExecutor anonymous type döndüğü için 'object' olarak çekiyoruz.
                var topicData = context.GetOutput<object>(StageType.Topic);

                if (topicData != null)
                {
                    // Reflection ile "Title" property'sini bulmaya çalış
                    var prop = topicData.GetType().GetProperty("Title", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var val = prop.GetValue(topicData);
                        title = val?.ToString() ?? title;
                    }
                    else
                    {
                        // Belki JSON stringdir?
                        title = topicData.ToString() ?? title;
                    }
                }
            }
            catch (Exception ex)
            {
                exec.AddLog($"Veri okuma uyarısı: {ex.Message}. Varsayılan başlık kullanılıyor.");
            }

            exec.AddLog($"Mock Script: Konu bulundu -> '{title}'");

            // İşlem simülasyonu
            exec.AddLog("Mock AI: Senaryo yazılıyor...");
            await Task.Delay(1000, ct);

            var mockScript = new
            {
                Title = title,
                Content = $"Merhaba! Bugün '{title}' hakkında konuşacağız. Çok ilginç gerçekler var.",
                Duration = 30,
                Scenes = new[]
                {
                    new { Scene = 1, Visual = "Intro", Audio = "Giriş yapılıyor." },
                    new { Scene = 2, Visual = "Main content", Audio = "Detaylar..." }
                }
            };

            return mockScript;
        }
    }
}

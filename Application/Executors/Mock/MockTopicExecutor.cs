using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Enums;

namespace Application.Executors.Mock
{
    //[StageExecutor(StageType.Topic)]
    public class MockTopicExecutor : BaseStageExecutor
    {
        public MockTopicExecutor(IServiceProvider sp) : base(sp) { }

        public override StageType StageType => StageType.Topic;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            CancellationToken ct)
        {
            // 1. Sanki AI düşünüyor...
            exec.AddLog("Mock AI: Analiz yapılıyor...");
            await Task.Delay(1500, ct); // 1.5 saniye bekle

            exec.AddLog("Mock AI: Trendler taranıyor...");
            await Task.Delay(1500, ct);

            // 2. Sahte bir çıktı üret
            var mockTopic = new
            {
                Title = "Zaman Yolculuğu Mümkün mü?",
                Premise = "Einstein'ın görelilik kuramına göre ışık hızına yaklaşınca zaman yavaşlar. Peki solucan delikleri?",
                Category = "Bilim",
                Language = "tr-TR"
            };

            exec.AddLog("Mock AI: Konu bulundu!");

            // 3. Context'e atılacak veriyi dön (Base class bunu hem DB'ye hem RAM'e yazar)
            // DTO/Payload sınıfın varsa onu dön, yoksa anonymous object şimdilik idare eder.
            return mockTopic;
        }
    }
}

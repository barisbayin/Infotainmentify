using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Extensions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;

namespace Application.Executors
{
    [StageExecutor(StageType.Topic)]
    [StagePreset(typeof(TopicPreset))]
    public class TopicStageExecutor : BaseStageExecutor
    {
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IRepository<Topic> _topicRepo; // Eğer Topic diye ayrı bir tablon varsa
        private readonly IUnitOfWork _uow;

        public TopicStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IRepository<Topic> topicRepo,
            IUnitOfWork uow)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _topicRepo = topicRepo;
            _uow = uow;
        }

        public override StageType StageType => StageType.Topic;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            CancellationToken ct)
        {
            // 1. Preseti Cast Et
            var preset = (TopicPreset)presetObj!;
            exec.AddLog($"Topic generation started using preset: {preset.Name}");

            // 2. AI Client Çözümleme
            // DÜZELTME: Property adı UserAiConnectionId oldu.
            var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 3. Prompt Hazırlığı
            // DÜZELTME: Property adı PromptTemplate oldu.
            // İleride buradaki {Category} gibi alanları replace edebiliriz.
            var prompt = preset.PromptTemplate;

            // 4. AI Çağrısı
            // DÜZELTME: ITextGenerator arayüzündeki metod GenerateTextAsync'dir.
            // ModelName preset'ten gelir.
            var topicText = await aiClient.GenerateTextAsync(
                prompt: prompt,
                temperature: preset.Temperature,
                model: preset.ModelName, // "gpt-4" vs.
                ct: ct);

            exec.AddLog("AI response received successfully.");

            // 5. DB'ye Kayıt (Topic Kütüphanesi İçin)
            // Bu kısım opsiyoneldir ama senin kodunda var diye koruyorum.
            var topic = new Topic
            {
                AppUserId = run.AppUserId,
                Premise = topicText,
                CreatedAt = DateTime.UtcNow,
                SourcePresetId = preset.Id // İzlenebilirlik için eklenebilir
            };

            await _topicRepo.AddAsync(topic, ct);
            await _uow.SaveChangesAsync(ct);

            exec.AddLog($"Topic saved to library. TopicId = {topic.Id}");

            // 6. Payload Dönüşü
            // Bir sonraki aşama (Script) bu veriyi okuyacak.
            return new TopicStagePayload
            {
                TopicId = topic.Id,
                TopicText = topicText,
                Language = preset.Language
            };
        }
    }
}

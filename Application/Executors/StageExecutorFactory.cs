using Application.Abstractions;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Executors
{
    public class StageExecutorFactory
    {
        private readonly IServiceProvider _sp;

        public StageExecutorFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public IStageExecutor GetExecutor(StageType type)
        {
            return type switch
            {
                StageType.Topic => _sp.GetRequiredService<TopicStageExecutor>(),
                StageType.ContentPlan => _sp.GetRequiredService<ContentPlanStageExecutor>(),
                StageType.Image => _sp.GetRequiredService<ImageStageExecutor>(),
                StageType.Tts => _sp.GetRequiredService<TtsStageExecutor>(),
                StageType.Video => _sp.GetRequiredService<VideoStageExecutor>(),
                StageType.Render => _sp.GetRequiredService<RenderStageExecutor>(),
                StageType.Upload => _sp.GetRequiredService<UploadStageExecutor>(),

                // İleride ihtiyaç duydukça buraya eklersin:
                //StageType.VideoAI => _sp.GetRequiredService<VideoAIStageExecutor>(),
                //StageType.Stt => _sp.GetRequiredService<SttStageExecutor>(),
                //StageType.ImageVariation => _sp.GetRequiredService<ImageVariationStageExecutor>(),
                //StageType.BRoll => _sp.GetRequiredService<BRollStageExecutor>(),
                //StageType.VideoClip => _sp.GetRequiredService<VideoClipStageExecutor>(),

                _ => throw new NotSupportedException($"StageType {type} için executor bulunamadı.")
            };
        }
    }
}

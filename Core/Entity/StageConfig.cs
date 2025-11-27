using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class StageConfig : BaseEntity
    {
        // Bu stage hangi pipeline’ın parçası?
        public int ContentPipelineTemplateId { get; set; }

        public ContentPipelineTemplate ContentPipelineTemplate { get; set; } = default!;

        // Ne tür bir stage? (Topic, Image, TTS, VideoAI vs.)
        public StageType StageType { get; set; }

        // Çalışma sırası
        public int Order { get; set; }

        // Stage ayar kaynağı (TopicPresetId, ScriptPresetId, ImagePresetId, TtsPresetId vs.)
        public int? PresetId { get; set; }

        // Stage’e has özel ayarlar (opsiyonel)
        // Örn: durationMultiplier, voiceStyle override, imageStyle override, longVideoMode flag
        public string? OptionsJson { get; set; }
    }

}

using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Pipeline
{
    public class StageConfig : BaseEntity
    {
        // Hangi Template'in parçası?
        [Required]
        public int ContentPipelineTemplateId { get; set; }

        // Navigation Property
        public ContentPipelineTemplate ContentPipelineTemplate { get; set; } = null!;

        // Hangi rolü oynuyor? (Topic, Script, Image...)
        [Required]
        public StageType StageType { get; set; }

        // Çalışma Sırası (1, 2, 3...)
        // UI'da sürükle bırak yaparken burası değişecek.
        [Required]
        public int Order { get; set; }

        // =================================================================
        // DİKKAT: BURASI SOFT KEY (Doğrudan SQL FK bağı yok)
        // StageType'a göre hangi tabloya bakılacağı Runtime'da belirlenir.
        // =================================================================
        public int? PresetId { get; set; }

        // Stage’e has anlık ayarlar (JSON)
        // Preset'i ezmek (override) veya ekstra parametreler için.
        // Veritabanında nvarchar(max) veya jsonb tutulmalı.
        public string? OptionsJson { get; set; }
    }
}

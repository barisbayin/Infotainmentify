using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Topics
{
    public class SaveTopicDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Premise { get; set; } = default!;

        [MaxLength(10)]
        public string LanguageCode { get; set; } = "tr-TR";

        [MaxLength(64)]
        public string? Category { get; set; }

        [MaxLength(128)]
        public string? SubCategory { get; set; }

        [MaxLength(128)]
        public string? Series { get; set; }

        public string? TagsJson { get; set; }

        // Kullanıcı manuel olarak ton/stil girebilir
        [MaxLength(64)]
        public string? Tone { get; set; }

        [MaxLength(64)]
        public string? RenderStyle { get; set; }

        [MaxLength(256)]
        public string? VisualPromptHint { get; set; }
    }
}

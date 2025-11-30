using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Presets
{
    public class SaveRenderPresetDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        public int OutputWidth { get; set; } = 1080;
        public int OutputHeight { get; set; } = 1920;
        public int Fps { get; set; } = 30;
        public int BitrateKbps { get; set; } = 6000;
        public string ContainerFormat { get; set; } = "mp4";

        public RenderCaptionSettingsDto CaptionSettings { get; set; } = new();
        public RenderAudioMixSettingsDto AudioMixSettings { get; set; } = new();
    }
}

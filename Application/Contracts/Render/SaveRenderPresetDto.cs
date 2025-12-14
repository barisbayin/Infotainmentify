namespace Application.Contracts.Render
{
    public class SaveRenderPresetDto
    {
        public string Name { get; set; } = default!;
        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public int Fps { get; set; }
        public int BitrateKbps { get; set; }
        public string ContainerFormat { get; set; } = "mp4";
        public string EncoderPreset { get; set; } = "medium"; // Yeni

        // Alt Objeler (Core'daki Entity Class'larını DTO olarak da kullanabilirsin 
        // veya temiz olsun dersen DTO versiyonlarını yazarsın. Pratik olsun diye Entity kullanıyorum)
        public Core.Entity.Models.RenderCaptionSettings CaptionSettings { get; set; } = new();
        public Core.Entity.Models.RenderAudioMixSettings AudioMixSettings { get; set; } = new();
        public Core.Entity.Models.RenderVisualEffectsSettings VisualEffectsSettings { get; set; } = new(); // Yeni
        public Core.Entity.Models.RenderBrandingSettings BrandingSettings { get; set; } = new(); // Yeni
    }
}

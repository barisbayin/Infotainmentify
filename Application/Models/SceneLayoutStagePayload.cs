namespace Application.Models
{
    public class SceneLayoutStagePayload
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Fps { get; set; }
        public double TotalDuration { get; set; }

        // Görsel Timeline (Resimler ve Efektler)
        public List<VisualEvent> VisualTrack { get; set; } = new();

        // Ses Timeline (Konuşma ve Müzik)
        public List<AudioEvent> AudioTrack { get; set; } = new();

        // Altyazı Timeline (Burn-in Captions)
        public List<CaptionEvent> CaptionTrack { get; set; } = new();

        public RenderStyleSettings Style { get; set; } = new();
    }
}

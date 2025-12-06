namespace Application.Models
{
    public class CaptionEvent
    {
        public string Text { get; set; } = default!;
        public double Start { get; set; }
        public double End { get; set; }
        // Stil bilgileri RenderPreset'ten alınacak, buraya koymaya gerek yok
    }
}

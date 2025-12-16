namespace Application.Models
{
    public class ReRenderRequest
    {
        public int RunId { get; set; }
        public int? NewRenderPresetId { get; set; } // Opsiyonel: Preset değiştirmek istersen
    }
}

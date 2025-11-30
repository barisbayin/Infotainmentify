namespace Application.Contracts.Presets
{
    // --- ALT DTO'lar ---

    public class RenderCaptionSettingsDto
    {
        public bool EnableCaptions { get; set; } = true;
        public string FontName { get; set; } = "Arial-Bold";
        public int FontSize { get; set; } = 48;
        public string PrimaryColor { get; set; } = "#FFFFFF";
        public string OutlineColor { get; set; } = "#000000";
        public bool EnableHighlight { get; set; } = true;
        public string HighlightColor { get; set; } = "#FFFF00";
        public int MaxWordsPerLine { get; set; } = 2;
    }
}

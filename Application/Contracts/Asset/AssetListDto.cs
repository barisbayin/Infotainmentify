namespace Application.Contracts.Asset
{
    // Listeleme ekranı için (Grid/Table)
    public class AssetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!; // FriendlyName
        public string Type { get; set; } = default!; // "Music", "Font"
        public string Url { get; set; } = default!;  // "/files/..." (Frontend direkt kullanır)
        public string SizeInfo { get; set; } = default!; // "2.5 MB"
    }
}

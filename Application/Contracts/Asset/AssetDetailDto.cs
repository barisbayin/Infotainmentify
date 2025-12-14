namespace Application.Contracts.Asset
{
    // Yükleme sonrası veya detay görüntüleme için
    public class AssetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string PhysicalName { get; set; } = default!; // Backend tarafında gerekirse diye
        public string Type { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string SizeInfo { get; set; } = default!;

        public double? DurationSec { get; set; } // Müzikse süre
        public DateTime CreatedAt { get; set; }
    }
}

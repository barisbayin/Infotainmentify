namespace Core.Entity.Models
{
    public class VideoAdvancedSettings
    {
        // Reproducibility için
        public long? Seed { get; set; }

        // Hareket Miktarı (Stability AI "Motion Bucket")
        // 1 (Donuk) - 255 (Çılgın hareket)
        public int MotionBucketId { get; set; } = 127;

        // AI'ın prompta ne kadar sadık kalacağı (CFG Scale)
        public double CfgScale { get; set; } = 7.0;

        // Video akıcılığı vs Artefakt dengesi
        public bool EnhanceMotion { get; set; } = true;

        // Döngüsel video olsun mu? (GIF mantığı)
        public bool Loop { get; set; } = false;
    }
}

namespace Core.Enums
{
    public enum AutoVideoAssetStatus
    {
        Pending = 0,            // Kayıt oluşturuldu ama işleme başlamadı

        GeneratingTopic = 1,    // Topic üretiliyor
        GeneratingScript = 2,   // Script üretiliyor
        GeneratingAssets = 3,   // Görsel / Thumbnail / Ses üretiliyor
        Rendering = 4,          // Video render ediliyor

        Completed = 5,          // Render bitti, video hazır

        Uploading = 6,          // YouTube / TikTok / Instagram'a yükleniyor
        Uploaded = 7,           // Başarıyla yüklendi

        Failed = 99             // Hata oldu
    }
}

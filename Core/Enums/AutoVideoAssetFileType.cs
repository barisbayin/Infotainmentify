namespace Core.Enums
{
    public enum AutoVideoAssetFileType
    {
        // AI Üretimi Temel Assetler
        Image = 1,            // Sahne görseli
        Audio = 2,            // TTS çıktı
        RawScene = 3,         // AI'nın ürettiği sahne JSON verisi

        // Render Ara Assetleri
        RenderInput = 10,     // Render'a girecek sahne birleşimi (image + audio pair)
        Transition = 11,      // Geçiş efekti dosyaları (opsiyonel)
        BackgroundMusic = 12, // Müzik dosyası (AI veya stok)

        // Render Sonuçları
        RenderedScene = 20,   // Tek sahne bazlı render edilmiş mp4/mov
        MergedVideo = 21,     // Tüm sahneler birleştirilmiş ancak thumbnail vs. eklenmemiş
        FinalVideo = 22,      // Tüm işlem tamamlanmış video (upload edilecek)

        // Thumbnail
        Thumbnail = 30,       // Final video thumbnail dosyası

        // Teknik Dosyalar
        TempFile = 40,        // İşlem sırasında oluşan geçici dosyalar
        LogDump = 41,         // Pipeline step log dosyaları
    }

}

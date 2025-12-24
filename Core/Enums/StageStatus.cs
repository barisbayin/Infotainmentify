namespace Core.Enums
{
    public enum StageStatus
    {
        Pending = 0,       // Henüz hiç çalışmadı
        Skipped = 1,       // Template gereği veya conditional logic gereği atlandı
        Running = 2,       // Şu an çalışıyor
        Completed = 3,     // Başarılı tamamlandı
        Failed = 4,        // Hata verdi ama pipeline durdu
        Retrying = 5,      // Fail sonrası retry deneniyor
        PermanentlyFailed = 6, // Tüm retry limitleri doldu, pipeline iptal
        Outdated = 7       // Artık geçerli değil
    }
}

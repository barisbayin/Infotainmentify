namespace Core.Enums
{
    public enum ContentPipelineStatus
    {
        Pending = 0,        // Sırada bekliyor
        Running = 1,        // Şu an çalışıyor
        Completed = 2,      // Başarıyla bitti
        Failed = 3,         // Hata aldı
        Cancelled = 4,      // Kullanıcı iptal etti
        Draft = 5           // Henüz çalıştırılmaya hazır değil (Taslak)
    }
}

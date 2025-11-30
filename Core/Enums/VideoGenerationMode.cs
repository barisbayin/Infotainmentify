namespace Core.Enums
{
    public enum VideoGenerationMode
    {
        TextToVideo = 1,  // Sıfırdan video üret (Daha tutarsız olabilir)
        ImageToVideo = 2  // Önceki sahnenin resmini canlandır (Karakter tutarlılığı için en iyisi)
    }
}

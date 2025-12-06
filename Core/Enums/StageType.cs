using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum StageType
    {
        // --- 1. METİN VE KONSEPT ---
        Topic = 1,              // Konu bulma
        Script = 2,             // Senaryo yazma (Text generation)
        Translation = 3,        // Çeviri (Opsiyonel)
        KeywordAnalysis = 4,    // SEO / Tag üretimi

        // --- 2. GÖRSEL DÜNYA ---
        Image = 20,             // DALL-E, Stable Diffusion (Prompt -> Image)
        VideoAI = 21,           // Sora, Runway, Pika (Text/Image -> Video)
        Avatar = 22,            // HeyGen, D-ID (Talking Head)

        // --- 3. SES DÜNYA ---
        Tts = 40,               // Metinden sese (ElevenLabs, Google)
        VoiceClone = 41,        // Ses klonlama işlemi (Özel bir stage olabilir)
        AudioMix = 42,          // Arka plan müziği + Ses birleştirme (Render öncesi hazırlık)
        Stt = 43,

        // --- 4. KURGU VE MONTAJ (Timeline Oluşturma) ---
        // Burası "Hangi saniyede ne girecek?" planını yapan yer.
        SceneLayout = 60,

        // Altyazı, render aşamasında da yapılabilir ama ayrı bir stage olması 
        // Whisper ile ses analizi yapıp zaman damgası (timestamp) çıkarmak için iyidir.
        Subtitle = 61,

        // --- 5. FİNAL RENDER (FFmpeg) ---
        Render = 80,            // Tüm assetleri birleştirip .mp4 çıkarma

        // --- 6. DAĞITIM ---
        Thumbnail = 100,        // Kapak resmi üretimi
        Upload = 101,           // YouTube/TikTok API Upload
        SocialShare = 102       // Link paylaşımı
    }
}

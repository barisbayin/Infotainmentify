using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum StageType
    {
        // ----------------------------------------------------------------------
        // 1) İÇERİK OLUŞTURMA (AI / metin / fikir)
        // ----------------------------------------------------------------------
        Topic = 1,                  // konu veya fikir üretimi
        ContentPlan = 2,            // sahneler + text + durations + prompts
        TextEnhance = 3,            // metin stil düzeltme (AI editor)
        Translation = 4,            // çoklu dil çeviri
        Summarize = 5,              // özet çıkarma (long video → short)
        KeywordExtract = 6,         // SEO, tag, title desteği (meta)
        FactCheck = 7,              // doğruluk kontrolü (istesen)

        // ----------------------------------------------------------------------
        // 2) GÖRSEL ÜRETİM (AI + traditional)
        // ----------------------------------------------------------------------
        Image = 20,                 // image generation (her sahne için)
        ImageVariation = 21,        // varyasyon üretimi
        ImageEnhance = 22,          // upscaling / denoise / sharpen
        ImageAnimation = 23,        // görüntüyü animasyona dönüştürme
        ImageToVideo = 24,          // I2V modelleri
        VideoAI = 25,               // Sora / Pika / Runway AI video production

        // ----------------------------------------------------------------------
        // 3) SES ÜRETİMİ
        // ----------------------------------------------------------------------
        Tts = 40,                   // metinden ses üretimi
        VoiceClone = 41,            // kullanıcı sesi klonlama
        VoiceEnhance = 42,          // gürültü giderme, EQ
        BackgroundMusic = 43,       // AI-generated müzik
        AudioMix = 44,              // müzik + voiceover karıştırma

        // ----------------------------------------------------------------------
        // 4) SES ANALİZİ (kullanıcı videosu / podcast vs.)
        // ----------------------------------------------------------------------
        Stt = 60,                   // speech-to-text
        AudioDetect = 61,           // ses analizi, ritim / bölme
        SilenceDetection = 62,      // sessizlik bulma (podcast kes)

        // ----------------------------------------------------------------------
        // 5) VIDEO KAYNAKLARI / B-ROLL / STOK VİDEO
        // ----------------------------------------------------------------------
        VideoClip = 80,             // stok video veya cut
        BRoll = 81,                 // documentary tarzı ek sahne
        TransitionMaterial = 82,    // geçiş sahnesi malzemesi
        MotionTemplate = 83,        // preset motion (pan/zoom templates)

        // ----------------------------------------------------------------------
        // 6) VİDEO DÜZENLEME / ÜRETİM
        // ----------------------------------------------------------------------
        SceneLayout = 100,          // sahneleri timeline'a çeviren
        CaptionGenerate = 101,      // altyazı oluşturma
        CaptionStyle = 102,         // altyazı stil efektleri
        VideoEffects = 103,         // efektler (shake, color, glow)
        ColorGrading = 104,         // color LUT
        SpeedRamp = 105,            // hız değiştirme
        Resize = 106,               // 9:16 → 16:9 dönüştürme
        Video = 120,                // tüm sahneleri birleştirip video yapmak
        Render = 121,               // final .mp4 export (ffmpeg)

        // ----------------------------------------------------------------------
        // 7) YAYIN / DIŞ ENTEGRASYON
        // ----------------------------------------------------------------------
        Thumbnail = 140,            // kapak görseli üretimi
        Metadata = 141,             // title + description + tags
        Upload = 160,               // platform upload
        Publish = 161,              // schedule / auto publish
        SocialShare = 162           // diğer sosyal ağlara paylaşım
    }


}

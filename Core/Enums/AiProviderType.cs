using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum AiProviderType
    {
        // --- LLM (Beyin Takımı) ---
        OpenAI = 1,             // GPT-4, DALL-E, TTS
        GoogleVertex = 2,       // Gemini, Veo
        Anthropic = 3,          // Claude 3 (Harika script yazar)
        DeepSeek = 4,           // Kod ve Text (F/P ürünü)
        AzureOpenAI = 5,        // Kurumsal OpenAI

        // --- GÖRSEL (Ressamlar) ---
        StabilityAI = 20,       // Stable Diffusion
        Midjourney = 21,        // (API'si yok ama Discord bot entegrasyonu için dursun)
        LeonardoAi = 22,        // Kaliteli görseller

        // --- VİDEO (Yönetmenler) ---
        RunwayML = 30,          // Gen-2, Gen-3
        PikaLabs = 31,
        LumaDreamMachine = 32,

        // --- SES (Seslendirmenler) ---
        ElevenLabs = 40,        // Piyasanın kralı
        AzureSpeech = 41,       // Microsoft TTS

        // --- DİĞER ---
        Custom = 99             // Local LLM (Ollama vs.) veya kendi API'miz
    }
}

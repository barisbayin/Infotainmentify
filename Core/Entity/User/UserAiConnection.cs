using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity.User
{
    public class UserAiConnection : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }

        // Navigation Property (Configuration dosyasında tanımladık ama burada da dursun)
        public AppUser AppUser { get; set; } = null!;

        // Kullanıcının verdiği isim: "Şahsi OpenAI Hesabım", "Şirket ElevenLabs"
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        // OpenAI, GoogleVertex, ElevenLabs, DeepSeek...
        [Required]
        public AiProviderType Provider { get; set; }

        // ==========================================================
        // GÜVENLİK ALANI (Secrets)
        // ==========================================================

        /// <summary>
        /// Ana API Anahtarı. (AES ile şifrelenmiş saklanmalı!)
        /// OpenAI için -> "sk-..."
        /// Google için -> Service Account JSON içeriğinin tamamı (String olarak)
        /// </summary>
        [Required, MaxLength(4000)] // Google JSON'ları uzun olabilir
        public string EncryptedApiKey { get; set; } = null!;

        /// <summary>
        /// Bazı sağlayıcılar ek ID ister.
        /// OpenAI -> Organization ID (Opsiyonel)
        /// Google -> Project ID
        /// </summary>
        [MaxLength(200)]
        public string? ExtraId { get; set; }

        // Dosya yolu yerine JSON içeriğini (EncryptedApiKey) içinde saklamak
        // Cloud/Docker ortamlarında daha güvenlidir. Dosya kaybolmaz.
    }
}

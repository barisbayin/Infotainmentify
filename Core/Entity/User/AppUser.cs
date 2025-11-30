using Core.Entity.Pipeline;
using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.User
{
    public class AppUser : BaseEntity
    {
        [Required, MaxLength(256)]
        public string Email { get; set; } = default!;

        [Required, MaxLength(128)]
        public string Username { get; set; } = default!;

        [Required, MaxLength(512)]
        public string PasswordHash { get; set; } = default!;

        public UserRole Role { get; set; } = UserRole.Normal;

        // Klasörleme stratejisi için kritik.
        // NOT: Kullanıcı ilk oluşurken ID'si 0 olabilir.
        // Önce save edip ID'yi alıp, sonra update etmen gerekecek.
        [Required, MaxLength(256)]
        public string DirectoryName { get; set; } = default!;

        // ==========================================================
        // YENİ MİMARİ İÇİN GEREKLİ BAĞLANTILAR (Navigation Props)
        // ==========================================================

        // 1. Kullanıcının Konseptleri (Markaları)
        // Örn: "History Facts Channel", "Scary Stories"
        public ICollection<Concept> Concepts { get; set; } = new List<Concept>();

        // 2. Kullanıcının Getirdiği AI Anahtarları (SaaS Modeli)
        // Örn: OpenAI Key, ElevenLabs Key
        public ICollection<UserAiConnection> AiConnections { get; set; } = new List<UserAiConnection>();

        // 3. Kullanıcının Sosyal Medya Hesapları (Upload için)
        // Örn: YouTube Channel Token, TikTok Token
        public ICollection<UserSocialChannel> SocialChannels { get; set; } = new List<UserSocialChannel>();

        // 4. Kullanıcının Video Projeleri (Geçmiş Run'lar)
        // Bunu direkt Run üzerinden sorgularız ama burada olması EF'te "Include" yaparken kolaylık sağlar.
        public ICollection<ContentPipelineRun> Runs { get; set; } = new List<ContentPipelineRun>();
    }
}

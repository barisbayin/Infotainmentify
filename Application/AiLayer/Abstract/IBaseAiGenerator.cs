using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.AiLayer.Abstract
{
    // 1. ORTAK ATA (Kimlik Doğrulama)
    public interface IBaseAiGenerator
    {
        AiProviderType ProviderType { get; }

        // ✅ YENİSİ BU OLMALI (Class ve Factory ile uyumlu):
        void Initialize(string apiKey, string? extraId = null);

        Task<bool> TestConnectionAsync(CancellationToken ct = default);
    }
}

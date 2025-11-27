using Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Application
{
    public class DataProtectionSecretStore : ISecretStore
    {
        public DataProtectionSecretStore(IDataProtectionProvider provider)
        {
            // provider kullanılmayacak
        }

        public string Protect(string plain)
        {
            // DEV ortam: direkt geri döndür
            return plain ?? "";
        }

        public string Unprotect(string cipher)
        {
            // DEV ortam: direkt geri döndür
            return cipher ?? "";
        }
    }

}

using Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Application
{
    public class DataProtectionSecretStore : ISecretStore
    {
        private readonly IDataProtector _protector;
        public DataProtectionSecretStore(IDataProtectionProvider provider)
            => _protector = provider.CreateProtector("UserAiConnections.v1");

        public string Protect(string plain) => _protector.Protect(plain);
        public string Unprotect(string cipher) => _protector.Unprotect(cipher);
    }
}

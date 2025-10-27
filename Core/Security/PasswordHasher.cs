using System.Security.Cryptography;

namespace Core.Security
{
    public static class PasswordHasher
    {
        // çıktı formatı: PBKDF2|{iter}|{saltBase64}|{hashBase64}
        public static string Hash(string password, int iterations = 100_000)
        {
            using var rng = RandomNumberGenerator.Create();
            Span<byte> salt = stackalloc byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt.ToArray(), iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(32);
            return $"PBKDF2|{iterations}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string stored)
        {
            var parts = stored.Split('|');
            if (parts.Length != 4 || parts[0] != "PBKDF2") return false;

            var iter = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var key = Convert.FromBase64String(parts[3]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256);
            var test = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(test, key);
        }
    }
}

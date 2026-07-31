using System;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyBenhVien.Helpers
{
    public static class HashHelper
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        // Format: "v2$<iterations>$<base64 salt>$<base64 hash>". Legacy hashes
        // (raw base64 SHA-256, no '$') are still verified for accounts that
        // haven't logged in since this upgrade - AuthController.Login rehashes
        // them to this format the moment the plaintext password is next proven.
        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, Algorithm, KeySize);
            return $"v2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (IsLegacyHash(hashedPassword))
            {
                return LegacyHash(password) == hashedPassword;
            }

            var parts = hashedPassword.Split('$');
            if (parts.Length != 4 || parts[0] != "v2" || !int.TryParse(parts[1], out var iterations))
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, Algorithm, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        public static bool IsLegacyHash(string hashedPassword) =>
            !string.IsNullOrEmpty(hashedPassword) && !hashedPassword.Contains('$');

        private static string LegacyHash(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}

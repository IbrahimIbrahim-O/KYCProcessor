using System.Security.Cryptography;
using System.Text;

namespace KYCProcessor.Api.Helpers
{
    public class PasswordHash
    {
        public static (string hashedPassword, string salt) HashPassword(string password)
        {
            var salt = GenerateSalt();
            var hashedPassword = HashPasswordWithSalt(password, salt);
            return (hashedPassword, salt);
        }

        public static string GenerateSalt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var salt = new byte[16]; // 128-bit salt
                rng.GetBytes(salt);
                return Convert.ToBase64String(salt);
            }
        }

        public static string HashPasswordWithSalt(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}

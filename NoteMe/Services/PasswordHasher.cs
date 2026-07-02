using System;
using System.Security.Cryptography;

namespace NoteMe.Services
{
    public class PasswordHasher
    {
        public string GenerateSalt()
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(saltBytes);
        }

        public string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                100000,
                HashAlgorithmName.SHA256
            );

            byte[] hashBytes = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string salt, string storedHash)
        {
            string newHash = HashPassword(password, salt);

            return newHash == storedHash;
        }
    }
}
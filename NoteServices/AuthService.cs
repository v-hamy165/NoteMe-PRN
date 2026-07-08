using NoteMe.Data;
using NoteMe.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace NoteMe.Services
{
    public class AuthService
    {
        private readonly PasswordHasher passwordHasher = new PasswordHasher();

        public void CreateDefaultAdminAccount()
        {
            using var context = new NoteMeDbContext();

            context.Database.Migrate();

            bool hasUser = context.Users.Any();

            if (!hasUser)
            {
                string salt = passwordHasher.GenerateSalt();
                string hash = passwordHasher.HashPassword("123456", salt);

                User admin = new User
                {
                    Username = "admin",
                    PasswordSalt = salt,
                    PasswordHash = hash
                };

                context.Users.Add(admin);
                context.SaveChanges();
            }
        }

        public User? Login(string username, string password)
        {
            using var context = new NoteMeDbContext();

            User? user = context.Users
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return null;
            }

            bool isValidPassword = passwordHasher.VerifyPassword(
                password,
                user.PasswordSalt,
                user.PasswordHash
            );

            return isValidPassword ? user : null;
        }

        public bool Register(string username, string password)
        {
            string normalizedUsername = username.Trim();

            using var context = new NoteMeDbContext();

            if (context.Users.Any(u => u.Username == normalizedUsername))
            {
                return false;
            }

            string salt = passwordHasher.GenerateSalt();
            string hash = passwordHasher.HashPassword(password, salt);

            User user = new User
            {
                Username = normalizedUsername,
                PasswordSalt = salt,
                PasswordHash = hash
            };

            context.Users.Add(user);

            try
            {
                context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                // Unique index trong database vẫn bảo vệ khi hai yêu cầu đăng ký
                // cùng một username xảy ra gần như đồng thời.
                using var verificationContext = new NoteMeDbContext();

                if (verificationContext.Users.Any(u => u.Username == normalizedUsername))
                {
                    return false;
                }

                throw;
            }
        }
    }
}

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
    }
}

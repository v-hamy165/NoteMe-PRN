using Microsoft.EntityFrameworkCore;
using NoteMe.Models;
using System;
using System.IO;
using System.Text.Json;

namespace NoteMe.Data
{
    public class NoteMeDbContext : DbContext
    {
        public DbSet<Note> Notes { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<AudioRecording> AudioRecordings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsPath));

            string? connectionString = settings.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("NoteMeDB")
                .GetString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy chuỗi kết nối 'NoteMeDB' trong appsettings.json."
                );
            }

            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.UserId, c.Name })
                .IsUnique();

            modelBuilder.Entity<AudioRecording>()
                .HasOne(a => a.Note)
                .WithMany(n => n.AudioRecordings)
                .HasForeignKey(a => a.NoteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

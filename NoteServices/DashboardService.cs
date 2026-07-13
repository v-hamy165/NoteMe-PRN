using NoteMe.Data;
using NoteMe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NoteMe.Services
{
    /// <summary>
    /// Lightweight DTO returned by <see cref="DashboardService.GetCategoryStatsAsync"/>.
    /// </summary>
    public class CategoryStat
    {
        public string Name { get; set; } = string.Empty;
        public int NoteCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Lightweight DTO for simulated recent activities.
    /// </summary>
    public class RecentActivityDto
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Provides aggregated statistics used by DashboardWindow.
    /// Follows the same pattern as NoteService / CategoryService.
    /// </summary>
    public class DashboardService
    {
        public async Task<int> GetTotalNotesAsync(int userId)
        {
            using var context = new NoteMeDbContext();
            return await context.Notes
                .CountAsync(n => n.UserId == userId);
        }

        public async Task<int> GetTotalCategoriesAsync(int userId)
        {
            using var context = new NoteMeDbContext();
            return await context.Categories
                .CountAsync(c => c.UserId == userId);
        }

        public async Task<int> GetTotalAudioRecordingsAsync(int userId)
        {
            using var context = new NoteMeDbContext();
            return await context.AudioRecordings
                .CountAsync(a => a.Note != null && a.Note.UserId == userId);
        }

        public async Task<int> GetTotalAiSummariesAsync(int userId)
        {
            using var context = new NoteMeDbContext();
            return await context.MeetingSummaries
                .CountAsync(s => s.Note != null && s.Note.UserId == userId);
        }

        public async Task<int> GetTotalOpenWorkTasksAsync(int userId)
        {
            using var context = new NoteMeDbContext();
            return await context.WorkTasks
                .CountAsync(t => t.UserId == userId &&
                    (t.Status == WorkTaskStatus.Todo || t.Status == WorkTaskStatus.InProgress));
        }

        public async Task<List<Note>> GetLatestNotesAsync(int userId, int count = 5)
        {
            using var context = new NoteMeDbContext();
            return await context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<CategoryStat>> GetCategoryStatsAsync(int userId)
        {
            using var context = new NoteMeDbContext();
            
            int totalNotes = await context.Notes.CountAsync(n => n.UserId == userId);
            if (totalNotes == 0)
            {
                return new List<CategoryStat>();
            }

            var stats = await context.Notes
                .Where(n => n.UserId == userId && n.Category != string.Empty)
                .GroupBy(n => n.Category)
                .Select(g => new CategoryStat
                {
                    Name = g.Key,
                    NoteCount = g.Count(),
                    Percentage = 0 // Will be calculated after materialization to avoid EF translation issues
                })
                .OrderByDescending(s => s.NoteCount)
                .ToListAsync();

            foreach (var stat in stats)
            {
                stat.Percentage = Math.Round((double)stat.NoteCount / totalNotes * 100, 1);
            }

            return stats;
        }

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int userId, int count = 10)
        {
            using var context = new NoteMeDbContext();

            // 1. Get recently created notes
            var notes = await context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Select(n => new RecentActivityDto
                {
                    Timestamp = n.CreatedAt,
                    ActivityType = "Ghi chú",
                    Description = $"Đã tạo ghi chú \"{n.Title}\""
                })
                .ToListAsync();

            // 2. Get recently recorded audios
            var audios = await context.AudioRecordings
                .Where(a => a.Note != null && a.Note.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .Select(a => new RecentActivityDto
                {
                    Timestamp = a.CreatedAt,
                    ActivityType = "Ghi âm",
                    Description = $"Đã ghi âm \"{a.FileName}\" cho ghi chú \"{(a.Note != null ? a.Note.Title : string.Empty)}\""
                })
                .ToListAsync();

            // 3. Get recently created meeting summaries
            var summaries = await context.MeetingSummaries
                .Where(s => s.Note != null && s.Note.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .Select(s => new RecentActivityDto
                {
                    Timestamp = s.CreatedAt,
                    ActivityType = "Tóm tắt AI",
                    Description = $"Đã tạo tóm tắt AI cho ghi chú \"{(s.Note != null ? s.Note.Title : string.Empty)}\""
                })
                .ToListAsync();

            // Combine and get latest top count
            return notes
                .Concat(audios)
                .Concat(summaries)
                .OrderByDescending(act => act.Timestamp)
                .Take(count)
                .ToList();
        }
    }
}

using NoteMe.Data;
using NoteMe.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NoteMe.Services
{
    public class SummaryService
    {
        private readonly GeminiService geminiService = new GeminiService();

        public List<MeetingSummary> GetSummariesByNote(int noteId, int userId)
        {
            using var context = new NoteMeDbContext();

            return context.MeetingSummaries
                .Where(s => s.NoteId == noteId && s.Note != null && s.Note.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();
        }

        public async Task<MeetingSummary> CreateFromAudioAsync(int noteId, int audioId, int userId)
        {
            string filePath;
            string fileName;

            using (var context = new NoteMeDbContext())
            {
                var audio = context.AudioRecordings
                    .Where(a => a.Id == audioId &&
                                a.NoteId == noteId &&
                                a.Note != null &&
                                a.Note.UserId == userId)
                    .Select(a => new { a.FilePath, a.FileName })
                    .FirstOrDefault();

                if (audio == null)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy file ghi âm của ghi chú này."
                    );
                }

                filePath = audio.FilePath;
                fileName = audio.FileName;
            }

            string fullPath = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(AppContext.BaseDirectory, filePath);

            MeetingAnalysisResult analysis =
                await geminiService.AnalyzeMeetingAudioAsync(fullPath);

            return SaveSummary(noteId, userId, fileName, analysis);
        }

        public async Task<MeetingSummary> CreateFromTextAsync(int noteId, int userId)
        {
            string title;
            string content;

            using (var context = new NoteMeDbContext())
            {
                var note = context.Notes
                    .Where(n => n.Id == noteId && n.UserId == userId)
                    .Select(n => new { n.Title, n.Content })
                    .FirstOrDefault();

                if (note == null)
                {
                    throw new InvalidOperationException("Ghi chú không tồn tại.");
                }

                title = note.Title;
                content = note.Content;
            }

            MeetingAnalysisResult analysis =
                await geminiService.SummarizeTextAsync(title, content);

            return SaveSummary(noteId, userId, string.Empty, analysis);
        }

        public bool DeleteSummary(int summaryId, int userId)
        {
            using var context = new NoteMeDbContext();

            var summary = context.MeetingSummaries
                .FirstOrDefault(s => s.Id == summaryId &&
                                     s.Note != null &&
                                     s.Note.UserId == userId);

            if (summary == null)
            {
                return false;
            }

            context.MeetingSummaries.Remove(summary);
            context.SaveChanges();
            return true;
        }

        private MeetingSummary SaveSummary(
            int noteId,
            int userId,
            string audioFileName,
            MeetingAnalysisResult analysis
        )
        {
            using var context = new NoteMeDbContext();

            bool noteExists = context.Notes
                .Any(n => n.Id == noteId && n.UserId == userId);

            if (!noteExists)
            {
                throw new InvalidOperationException(
                    "Ghi chú không còn tồn tại nên không thể lưu bản tóm tắt."
                );
            }

            var summary = new MeetingSummary
            {
                NoteId = noteId,
                AudioFileName = audioFileName,
                Transcript = analysis.Transcript,
                MainContent = analysis.MainContent,
                CompletedSteps = string.Join("\n", analysis.CompletedSteps),
                NextSteps = string.Join("\n", analysis.NextSteps),
                ModelUsed = geminiService.Model,
                CreatedAt = DateTime.Now
            };

            context.MeetingSummaries.Add(summary);
            context.SaveChanges();

            return summary;
        }
    }
}

using NoteMe.Data;
using NoteMe.Models;
using System.Collections.Generic;
using System.Linq;

namespace NoteMe.Services
{
    public class NoteService
    {
        public List<Note> GetAllNotes(int userId)
        {
            using var context = new NoteMeDbContext();

            return context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public List<Note> SearchNotes(int userId, string? keyword, string? category)
        {
            using var context = new NoteMeDbContext();

            var query = context.Notes
                .Where(n => n.UserId == userId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(n =>
                    n.Title.Contains(keyword) ||
                    n.Content.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                category = category.Trim();

                query = query.Where(n => n.Category == category);
            }

            return query
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public void AddNote(Note note, int userId)
        {
            using var context = new NoteMeDbContext();

            note.UserId = userId;

            context.Notes.Add(note);
            context.SaveChanges();
        }

        public void UpdateNote(Note note, int userId)
        {
            using var context = new NoteMeDbContext();

            var oldNote = context.Notes
                .FirstOrDefault(n => n.Id == note.Id && n.UserId == userId);

            if (oldNote != null)
            {
                oldNote.Title = note.Title;
                oldNote.Content = note.Content;
                oldNote.Category = note.Category;

                context.SaveChanges();
            }
        }

        public void DeleteNote(int id, int userId)
        {
            using var context = new NoteMeDbContext();

            var note = context.Notes
                .FirstOrDefault(n => n.Id == id && n.UserId == userId);

            if (note != null)
            {
                context.Notes.Remove(note);
                context.SaveChanges();
            }
        }
    }
}

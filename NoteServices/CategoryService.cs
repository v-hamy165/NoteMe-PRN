using NoteMe.Data;
using NoteMe.Models;
using System.Collections.Generic;
using System.Linq;

namespace NoteMe.Services
{
    public class CategoryService
    {
        public List<Category> GetCategoriesByUser(int userId)
        {
            using var context = new NoteMeDbContext();

            return context.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public bool AddCategory(int userId, string categoryName)
        {
            using var context = new NoteMeDbContext();

            categoryName = categoryName.Trim();

            bool existed = context.Categories
                .Any(c => c.UserId == userId && c.Name == categoryName);

            if (existed)
            {
                return false;
            }

            Category category = new Category
            {
                Name = categoryName,
                UserId = userId
            };

            context.Categories.Add(category);
            context.SaveChanges();

            return true;
        }

        public bool UpdateCategory(int userId, int categoryId, string newName)
        {
            using var context = new NoteMeDbContext();

            newName = newName.Trim();

            Category? category = context.Categories
                .FirstOrDefault(c => c.Id == categoryId && c.UserId == userId);

            if (category == null)
            {
                return false;
            }

            bool existed = context.Categories.Any(c =>
                c.UserId == userId && c.Id != categoryId && c.Name == newName);

            if (existed)
            {
                return false;
            }

            string oldName = category.Name;
            category.Name = newName;

            // Notes currently store the category name, so keep them in sync.
            var notes = context.Notes
                .Where(n => n.UserId == userId && n.Category == oldName)
                .ToList();

            foreach (Note note in notes)
            {
                note.Category = newName;
            }

            context.SaveChanges();
            return true;
        }

        public bool DeleteCategory(int userId, int categoryId)
        {
            using var context = new NoteMeDbContext();

            Category? category = context.Categories
                .FirstOrDefault(c => c.Id == categoryId && c.UserId == userId);

            if (category == null)
            {
                return false;
            }

            bool isInUse = context.Notes.Any(n =>
                n.UserId == userId && n.Category == category.Name);

            if (isInUse)
            {
                return false;
            }

            context.Categories.Remove(category);
            context.SaveChanges();
            return true;
        }
    }
}

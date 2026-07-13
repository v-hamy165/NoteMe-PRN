using Microsoft.EntityFrameworkCore;
using NoteMe.Data;
using NoteMe.Models;

namespace NoteMe.Services
{
    public class WorkTaskService
    {
        private static DateTime CalculateReminderAt(DateTime dueDate) => dueDate.AddDays(-1);

        public List<WorkTask> GetTasks(int userId, WorkTaskStatus? status = null, DateTime? date = null)
        {
            using var context = new NoteMeDbContext();
            IQueryable<WorkTask> query = context.WorkTasks
                .AsNoTracking()
                .Include(t => t.Note)
                .Where(t => t.UserId == userId);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);
            if (date.HasValue)
            {
                DateTime from = date.Value.Date;
                DateTime to = from.AddDays(1);
                query = query.Where(t => t.DueDate >= from && t.DueDate < to);
            }

            var tasks = query.OrderBy(t => t.Status == WorkTaskStatus.Completed)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.Priority)
                .ToList();

            foreach (var task in tasks.Where(t => t.ReminderAt == null))
            {
                task.ReminderAt = CalculateReminderAt(task.DueDate);
            }

            return tasks;
        }

        public WorkTask Save(WorkTask task, int userId)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
                throw new InvalidOperationException("Tiêu đề công việc không được để trống.");
            if (task.DueDate == default)
                throw new InvalidOperationException("Vui lòng chọn hạn hoàn thành.");

            using var context = new NoteMeDbContext();
            WorkTask entity;
            DateTime reminderAt = CalculateReminderAt(task.DueDate);
            if (task.Id == 0)
            {
                entity = task;
                entity.UserId = userId;
                entity.CreatedAt = DateTime.Now;
                entity.ReminderAt = reminderAt;
                entity.ReminderSent = false;
                context.WorkTasks.Add(entity);
            }
            else
            {
                entity = context.WorkTasks.FirstOrDefault(t => t.Id == task.Id && t.UserId == userId)
                    ?? throw new InvalidOperationException("Công việc không còn tồn tại.");
                DateTime? oldReminderAt = entity.ReminderAt;
                WorkTaskStatus oldStatus = entity.Status;
                entity.Title = task.Title.Trim();
                entity.Description = task.Description.Trim();
                entity.DueDate = task.DueDate;
                entity.Priority = task.Priority;
                entity.Status = task.Status;
                entity.ReminderAt = reminderAt;
                if (oldReminderAt != entity.ReminderAt ||
                    (oldStatus is WorkTaskStatus.Completed or WorkTaskStatus.Cancelled &&
                     entity.Status is WorkTaskStatus.Todo or WorkTaskStatus.InProgress))
                {
                    entity.ReminderSent = false;
                }
            }
            entity.UpdatedAt = DateTime.Now;
            context.SaveChanges();
            return entity;
        }

        public int CreateFromAiSteps(int userId, int noteId, int summaryId, IEnumerable<string> steps,
            DateTime dueDate, WorkTaskPriority priority)
        {
            using var context = new NoteMeDbContext();
            bool sourceExists = context.MeetingSummaries.Any(s => s.Id == summaryId && s.NoteId == noteId &&
                s.Note != null && s.Note.UserId == userId);
            if (!sourceExists)
                throw new InvalidOperationException("Bản tóm tắt AI không còn tồn tại.");

            var titles = steps.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
            foreach (string title in titles)
            {
                context.WorkTasks.Add(new WorkTask
                {
                    UserId = userId,
                    NoteId = noteId,
                    MeetingSummaryId = summaryId,
                    Title = title.Length <= 200 ? title : title[..200],
                    Description = "Được tạo từ bước tiếp theo do AI trích xuất.",
                    DueDate = dueDate,
                    Priority = priority,
                    Status = WorkTaskStatus.Todo,
                    ReminderAt = CalculateReminderAt(dueDate),
                    ReminderSent = false
                });
            }
            context.SaveChanges();
            return titles.Count;
        }

        public bool Delete(int taskId, int userId)
        {
            using var context = new NoteMeDbContext();
            var task = context.WorkTasks.FirstOrDefault(t => t.Id == taskId && t.UserId == userId);
            if (task == null) return false;
            context.WorkTasks.Remove(task);
            context.SaveChanges();
            return true;
        }

        public List<WorkTask> GetDueReminders(int userId)
        {
            using var context = new NoteMeDbContext();
            DateTime now = DateTime.Now;
            DateTime reminderWindowEnd = now.AddDays(1);
            return context.WorkTasks.Where(t => t.UserId == userId && !t.ReminderSent &&
                    t.DueDate <= reminderWindowEnd &&
                    t.Status != WorkTaskStatus.Completed && t.Status != WorkTaskStatus.Cancelled)
                .OrderBy(t => t.DueDate).ToList();
        }

        public void MarkRemindersSent(IEnumerable<int> taskIds, int userId)
        {
            using var context = new NoteMeDbContext();
            var ids = taskIds.ToList();
            foreach (var task in context.WorkTasks.Where(t => t.UserId == userId && ids.Contains(t.Id)))
                task.ReminderSent = true;
            context.SaveChanges();
        }
    }
}

using NoteMe.Services;
using System.Windows;
using System.Windows.Threading;

namespace NoteMe
{
    public static class TaskReminderManager
    {
        private static readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMinutes(1) };
        private static int userId;
        private static bool checking;

        static TaskReminderManager() => timer.Tick += (_, _) => Check();

        public static void Start(int currentUserId)
        {
            userId = currentUserId;
            if (!timer.IsEnabled) timer.Start();
            Check();
        }

        public static void Stop() { timer.Stop(); userId = 0; }

        private static void Check()
        {
            if (checking || userId == 0 || Application.Current.Windows.OfType<ReminderWindow>().Any()) return;
            checking = true;
            try
            {
                var service = new WorkTaskService();
                var reminders = service.GetDueReminders(userId);
                if (reminders.Count == 0) return;
                service.MarkRemindersSent(reminders.Select(t => t.Id), userId);
                new ReminderWindow(reminders).Show();
            }
            catch { /* Lần kiểm tra sau sẽ thử lại; không làm gián đoạn công việc của người dùng. */ }
            finally { checking = false; }
        }
    }
}

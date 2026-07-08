using NoteMe.Models;
using System.Windows;

namespace NoteMe
{
    public partial class ReminderWindow : Window
    {
        public ReminderWindow(IEnumerable<WorkTask> tasks)
        {
            InitializeComponent();
            lstReminders.ItemsSource = tasks;
            Loaded += (_, _) =>
            {
                Left = SystemParameters.WorkArea.Right - Width - 16;
                Top = SystemParameters.WorkArea.Bottom - Height - 16;
            };
        }
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void btnOpenTasks_Click(object sender, RoutedEventArgs e)
        {
            var existing = Application.Current.Windows.OfType<TaskWindow>().FirstOrDefault();
            if (existing == null) { existing = new TaskWindow(); existing.Show(); } else existing.Activate();
            Close();
        }
    }
}

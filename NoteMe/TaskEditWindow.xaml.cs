using NoteMe.Models;
using System.Windows;

namespace NoteMe
{
    public partial class TaskEditWindow : Window
    {
        private sealed record Choice<T>(T Key, string Label)
        {
            public override string ToString() => Label;
        }

        public WorkTask Task { get; }

        public TaskEditWindow(WorkTask? task = null)
        {
            InitializeComponent();
            Task = task == null ? new WorkTask() : new WorkTask
            {
                Id = task.Id, UserId = task.UserId, NoteId = task.NoteId,
                MeetingSummaryId = task.MeetingSummaryId, Title = task.Title,
                Description = task.Description, DueDate = task.DueDate,
                Priority = task.Priority, Status = task.Status,
                ReminderAt = task.ReminderAt, ReminderSent = task.ReminderSent,
                CreatedAt = task.CreatedAt
            };

            var hours = Enumerable.Range(0, 24).Select(x => x.ToString("00")).ToList();
            var minutes = new[] { "00", "15", "30", "45" };
            cboDueHour.ItemsSource = hours; cboReminderHour.ItemsSource = hours;
            cboDueMinute.ItemsSource = minutes; cboReminderMinute.ItemsSource = minutes;
            cboPriority.ItemsSource = new[]
            {
                new Choice<WorkTaskPriority>(WorkTaskPriority.Low, "Thấp"),
                new Choice<WorkTaskPriority>(WorkTaskPriority.Medium, "Trung bình"),
                new Choice<WorkTaskPriority>(WorkTaskPriority.High, "Cao")
            };
            cboStatus.ItemsSource = new[]
            {
                new Choice<WorkTaskStatus>(WorkTaskStatus.Todo, "Cần làm"),
                new Choice<WorkTaskStatus>(WorkTaskStatus.InProgress, "Đang làm"),
                new Choice<WorkTaskStatus>(WorkTaskStatus.Completed, "Hoàn thành"),
                new Choice<WorkTaskStatus>(WorkTaskStatus.Cancelled, "Đã hủy")
            };

            DateTime due = task?.DueDate ?? DateTime.Now.Date.AddDays(1).AddHours(17);
            txtHeading.Text = task == null ? "Tạo công việc mới" : "Cập nhật công việc";
            txtTitle.Text = Task.Title; txtDescription.Text = Task.Description;
            dpDueDate.SelectedDate = due.Date; cboDueHour.SelectedItem = due.ToString("HH");
            cboDueMinute.SelectedItem = ClosestMinute(due.Minute);
            cboPriority.SelectedValue = Task.Priority;
            cboStatus.SelectedValue = Task.Status;
            chkReminder.IsChecked = Task.ReminderAt.HasValue;
            DateTime reminder = Task.ReminderAt ?? due.AddHours(-1);
            dpReminderDate.SelectedDate = reminder.Date; cboReminderHour.SelectedItem = reminder.ToString("HH");
            cboReminderMinute.SelectedItem = ClosestMinute(reminder.Minute);
        }

        private static string ClosestMinute(int minute) => (Math.Min(45, (minute / 15) * 15)).ToString("00");
        private static DateTime Combine(DateTime? date, object hour, object minute) =>
            date!.Value.Date.AddHours(int.Parse(hour.ToString()!)).AddMinutes(int.Parse(minute.ToString()!));

        private void chkReminder_Changed(object sender, RoutedEventArgs e) => pnlReminder.IsEnabled = chkReminder.IsChecked == true;

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text) || dpDueDate.SelectedDate == null)
            { MessageBox.Show("Vui lòng nhập tiêu đề và hạn hoàn thành."); return; }
            DateTime due = Combine(dpDueDate.SelectedDate, cboDueHour.SelectedItem, cboDueMinute.SelectedItem);
            DateTime? reminder = chkReminder.IsChecked == true
                ? Combine(dpReminderDate.SelectedDate, cboReminderHour.SelectedItem, cboReminderMinute.SelectedItem) : null;
            if (reminder >= due) { MessageBox.Show("Thời điểm nhắc phải trước hạn hoàn thành."); return; }
            Task.Title = txtTitle.Text.Trim(); Task.Description = txtDescription.Text.Trim();
            Task.DueDate = due; Task.Priority = (WorkTaskPriority)cboPriority.SelectedValue;
            Task.Status = (WorkTaskStatus)cboStatus.SelectedValue; Task.ReminderAt = reminder;
            DialogResult = true;
        }
    }
}

using NoteMe.Models;
using NoteMe.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NoteMe
{
    public partial class AiTaskWindow : Window
    {
        private sealed record Choice<T>(T Key, string Label)
        {
            public override string ToString() => Label;
        }

        private sealed class StepChoice : INotifyPropertyChanged
        {
            private bool isSelected = true;
            public string Title { get; init; } = string.Empty;
            public bool IsSelected { get => isSelected; set { isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private readonly int userId;
        private readonly int noteId;
        private readonly int summaryId;

        public AiTaskWindow(int userId, int noteId, int summaryId, IEnumerable<string> steps)
        {
            InitializeComponent();
            this.userId = userId; this.noteId = noteId; this.summaryId = summaryId;
            lstSteps.ItemsSource = steps.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => new StepChoice { Title = s.Trim() }).ToList();
            dpDueDate.SelectedDate = DateTime.Today.AddDays(1);
            cboPriority.ItemsSource = new[]
            {
                new Choice<WorkTaskPriority>(WorkTaskPriority.Low, "Thấp"),
                new Choice<WorkTaskPriority>(WorkTaskPriority.Medium, "Trung bình"),
                new Choice<WorkTaskPriority>(WorkTaskPriority.High, "Cao")
            };
            cboPriority.SelectedValue = WorkTaskPriority.Medium;
            cboReminderOffset.ItemsSource = new[]
            {
                new Choice<TimeSpan>(TimeSpan.FromMinutes(30), "30 phút"),
                new Choice<TimeSpan>(TimeSpan.FromHours(1), "1 giờ"),
                new Choice<TimeSpan>(TimeSpan.FromHours(3), "3 giờ"),
                new Choice<TimeSpan>(TimeSpan.FromDays(1), "1 ngày")
            };
            cboReminderOffset.SelectedValue = TimeSpan.FromHours(1);
            UpdateReminderPreview();
        }

        private void ReminderInput_Changed(object sender, RoutedEventArgs e) =>
            UpdateReminderPreview();

        private void UpdateReminderPreview()
        {
            if (txtReminderAtPreview == null)
                return;

            if (chkReminder?.IsChecked != true)
            {
                txtReminderAtPreview.Text = "ReminderAt: Không nhắc";
                if (cboReminderOffset != null) cboReminderOffset.IsEnabled = false;
                return;
            }

            if (cboReminderOffset != null) cboReminderOffset.IsEnabled = true;
            if (dpDueDate?.SelectedDate == null ||
                cboReminderOffset?.SelectedValue is not TimeSpan offset)
            {
                txtReminderAtPreview.Text = "ReminderAt: Chọn hạn hoàn thành và thời gian nhắc";
                return;
            }

            DateTime reminderAt = dpDueDate.SelectedDate.Value.Date.AddHours(17) - offset;
            txtReminderAtPreview.Text = $"ReminderAt: {reminderAt:dd/MM/yyyy HH:mm}";
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            var selected = lstSteps.Items.Cast<StepChoice>().Where(x => x.IsSelected).Select(x => x.Title).ToList();
            if (selected.Count == 0) { MessageBox.Show("Vui lòng chọn ít nhất một bước."); return; }
            if (dpDueDate.SelectedDate == null) { MessageBox.Show("Vui lòng chọn hạn hoàn thành."); return; }
            DateTime due = dpDueDate.SelectedDate.Value.Date.AddHours(17);
            DateTime? reminder = chkReminder.IsChecked == true ? due - (TimeSpan)cboReminderOffset.SelectedValue : null;
            try
            {
                int count = new WorkTaskService().CreateFromAiSteps(userId, noteId, summaryId, selected, due,
                    (WorkTaskPriority)cboPriority.SelectedValue, reminder);
                MessageBox.Show($"Đã tạo {count} công việc từ AI.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể tạo công việc", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}

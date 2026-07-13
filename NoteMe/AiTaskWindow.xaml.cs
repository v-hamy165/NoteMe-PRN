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
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            var selected = lstSteps.Items.Cast<StepChoice>().Where(x => x.IsSelected).Select(x => x.Title).ToList();
            if (selected.Count == 0) { MessageBox.Show("Vui lòng chọn ít nhất một bước."); return; }
            if (dpDueDate.SelectedDate == null) { MessageBox.Show("Vui lòng chọn hạn hoàn thành."); return; }
            DateTime due = dpDueDate.SelectedDate.Value.Date.AddHours(17);
            try
            {
                int count = new WorkTaskService().CreateFromAiSteps(userId, noteId, summaryId, selected, due,
                    (WorkTaskPriority)cboPriority.SelectedValue);
                MessageBox.Show($"Đã tạo {count} công việc từ AI.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                TaskReminderManager.Start(userId);
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể tạo công việc", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}

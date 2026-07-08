using NoteMe.Models;
using NoteMe.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NoteMe
{
    public partial class TaskWindow : Window
    {
        private sealed record StatusFilter(WorkTaskStatus? Value, string Label)
        {
            public override string ToString() => Label;
        }
        private readonly WorkTaskService service = new();
        private readonly int userId;

        public TaskWindow()
        {
            InitializeComponent();
            userId = AppSession.CurrentUser?.Id ?? 0;
            cboStatusFilter.ItemsSource = new[]
            {
                new StatusFilter(null, "Tất cả"), new StatusFilter(WorkTaskStatus.Todo, "Cần làm"),
                new StatusFilter(WorkTaskStatus.InProgress, "Đang làm"),
                new StatusFilter(WorkTaskStatus.Completed, "Hoàn thành"),
                new StatusFilter(WorkTaskStatus.Cancelled, "Đã hủy")
            };
            cboStatusFilter.DisplayMemberPath = nameof(StatusFilter.Label);
            cboStatusFilter.SelectedIndex = 0;
            calendar.SelectedDate = DateTime.Today;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (userId == 0) { Close(); return; }
            LoadTasks(); LoadCalendar();
        }

        private void LoadTasks()
        {
            WorkTaskStatus? status = (cboStatusFilter.SelectedItem as StatusFilter)?.Value;
            var tasks = service.GetTasks(userId, status);
            if (chkHideCompleted.IsChecked == true)
                tasks = tasks.Where(t => t.Status != WorkTaskStatus.Completed).ToList();
            dgTasks.ItemsSource = tasks;
            int overdue = tasks.Count(t => t.DueDate < DateTime.Now && t.Status is not WorkTaskStatus.Completed and not WorkTaskStatus.Cancelled);
            int open = tasks.Count(t => t.Status is WorkTaskStatus.Todo or WorkTaskStatus.InProgress);
            txtSummary.Text = $"{open} việc đang mở  •  {overdue} việc quá hạn";
        }

        private void LoadCalendar()
        {
            DateTime date = calendar.SelectedDate ?? DateTime.Today;
            txtCalendarDate.Text = $"Công việc ngày {date:dd/MM/yyyy}";
            dgCalendarTasks.ItemsSource = service.GetTasks(userId, date: date);
        }

        private void OpenEditor(WorkTask? task = null)
        {
            var dialog = new TaskEditWindow(task) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            try { service.Save(dialog.Task, userId); LoadTasks(); LoadCalendar(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể lưu", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e) => OpenEditor();
        private void btnEdit_Click(object sender, RoutedEventArgs e) { if (dgTasks.SelectedItem is WorkTask task) OpenEditor(task); else MessageBox.Show("Vui lòng chọn công việc cần sửa."); }
        private void dgTasks_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (dgTasks.SelectedItem is WorkTask task) OpenEditor(task); }
        private void btnRefresh_Click(object sender, RoutedEventArgs e) { LoadTasks(); LoadCalendar(); }
        private void cboStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) LoadTasks(); }
        private void filter_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) LoadTasks(); }
        private void calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) LoadCalendar(); }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTasks.SelectedItem is not WorkTask task) { MessageBox.Show("Vui lòng chọn công việc."); return; }
            task.Status = WorkTaskStatus.Completed;
            service.Save(task, userId); LoadTasks(); LoadCalendar();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTasks.SelectedItem is not WorkTask task) { MessageBox.Show("Vui lòng chọn công việc cần xóa."); return; }
            if (MessageBox.Show($"Xóa công việc “{task.Title}”?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            service.Delete(task.Id, userId); LoadTasks(); LoadCalendar();
        }
    }
}

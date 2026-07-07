using NoteMe.Models;
using NoteMe.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace NoteMe.Dashboard
{
    public partial class DashboardWindow : Window
    {
        private readonly DashboardService dashboardService = new DashboardService();
        private readonly int currentUserId;
        private DispatcherTimer? clockTimer;

        public DashboardWindow()
        {
            InitializeComponent();
            currentUserId = AppSession.CurrentUser?.Id ?? 0;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (currentUserId == 0)
            {
                MessageBox.Show("Bạn cần đăng nhập trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }

            // Start live clock
            UpdateWelcomeMessage();
            clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            clockTimer.Tick += (s, ev) => UpdateWelcomeMessage();
            clockTimer.Start();

            await LoadDashboardAsync();
        }

        private void UpdateWelcomeMessage()
        {
            if (AppSession.CurrentUser != null)
            {
                txtWelcome.Text = $"Chào mừng quay lại, {AppSession.CurrentUser.Username}  |  🕒 {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            }
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                btnRefresh.IsEnabled = false;

                // Load all dashboard statistics & data asynchronously
                var totalNotesTask = dashboardService.GetTotalNotesAsync(currentUserId);
                var totalCategoriesTask = dashboardService.GetTotalCategoriesAsync(currentUserId);
                var totalAudioTask = dashboardService.GetTotalAudioRecordingsAsync(currentUserId);
                var totalSummariesTask = dashboardService.GetTotalAiSummariesAsync(currentUserId);
                var latestNotesTask = dashboardService.GetLatestNotesAsync(currentUserId, 5);
                var categoryStatsTask = dashboardService.GetCategoryStatsAsync(currentUserId);
                var activitiesTask = dashboardService.GetRecentActivitiesAsync(currentUserId, 10);

                await Task.WhenAll(
                    totalNotesTask,
                    totalCategoriesTask,
                    totalAudioTask,
                    totalSummariesTask,
                    latestNotesTask,
                    categoryStatsTask,
                    activitiesTask
                );

                // Bind stat cards
                txtTotalNotes.Text = totalNotesTask.Result.ToString();
                txtTotalCategories.Text = totalCategoriesTask.Result.ToString();
                txtTotalAudio.Text = totalAudioTask.Result.ToString();
                txtTotalSummaries.Text = totalSummariesTask.Result.ToString();

                // Bind Latest Notes & handle empty state
                var latestNotes = latestNotesTask.Result;
                dgLatestNotes.ItemsSource = null;
                dgLatestNotes.ItemsSource = latestNotes;
                lblNoNotes.Visibility = latestNotes.Any() ? Visibility.Collapsed : Visibility.Visible;

                // Bind Category Stats & handle empty state
                var categoryStats = categoryStatsTask.Result;
                dgCategoryStats.ItemsSource = null;
                dgCategoryStats.ItemsSource = categoryStats;
                lblNoCategories.Visibility = categoryStats.Any() ? Visibility.Collapsed : Visibility.Visible;

                // Bind Recent Activities & handle empty state
                var activities = activitiesTask.Result;
                dgRecentActivities.ItemsSource = null;
                dgRecentActivities.ItemsSource = activities;
                lblNoActivities.Visibility = activities.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể tải dữ liệu Dashboard: {ex.Message}",
                    "Lỗi kết nối cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                btnRefresh.IsEnabled = true;
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardAsync();
        }

        // --- Quick Actions / Navigation ---

        private void btnNewNote_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = OpenMainWindow();
            mainWindow.TriggerClearForm();
        }

        private void btnManageNotes_Click(object sender, RoutedEventArgs e)
        {
            OpenMainWindow();
        }

        private void btnOpenCategories_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = OpenMainWindow();
            mainWindow.FocusCategorySection();
        }

        private void btnRecordAudio_Click(object sender, RoutedEventArgs e)
        {
            if (dgLatestNotes.SelectedItem is Note note)
            {
                MainWindow mainWindow = OpenMainWindow();
                mainWindow.SelectNoteById(note.Id);
                mainWindow.FocusAudioSection();
            }
            else
            {
                MessageBox.Show(
                    "Vui lòng chọn một ghi chú từ bảng '5 Ghi chú gần nhất' để bắt đầu ghi âm cho ghi chú đó.",
                    "Hướng dẫn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private async void btnAiSummary_Click(object sender, RoutedEventArgs e)
        {
            if (dgLatestNotes.SelectedItem is Note note)
            {
                var summaryWindow = new SummaryWindow(note.Id, note.Title, currentUserId)
                {
                    Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault() ?? OpenMainWindow()
                };
                summaryWindow.ShowDialog();
                await LoadDashboardAsync();
            }
            else
            {
                MessageBox.Show(
                    "Vui lòng chọn một ghi chú từ bảng '5 Ghi chú gần nhất' để tạo tóm tắt AI.",
                    "Hướng dẫn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void btnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (dgLatestNotes.SelectedItem is not Note note)
            {
                MessageBox.Show(
                    "Vui lòng chọn một ghi chú từ bảng '5 Ghi chú gần nhất' để xuất PDF.",
                    "Hướng dẫn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                var summaryService = new SummaryService();
                var summaries = summaryService.GetSummariesByNote(note.Id, currentUserId);

                if (summaries == null || !summaries.Any())
                {
                    var result = MessageBox.Show(
                        "Ghi chú này chưa có bản tóm tắt AI nào để xuất PDF. Bạn có muốn tạo tóm tắt AI cho ghi chú này ngay bây giờ không?",
                        "Không tìm thấy bản tóm tắt",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        var summaryWindow = new SummaryWindow(note.Id, note.Title, currentUserId)
                        {
                            Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault() ?? OpenMainWindow()
                        };
                        summaryWindow.ShowDialog();
                        _ = LoadDashboardAsync();
                    }
                    return;
                }

                // Export the latest AI summary
                var latestSummary = summaries.First();
                
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF file (*.pdf)|*.pdf",
                    FileName = $"TomTat_{note.Title.Replace(" ", "_")}_{latestSummary.CreatedAt:yyyyMMdd_HHmm}.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    var pdfExportService = new PdfExportService();
                    pdfExportService.ExportSummary(latestSummary, note.Title, dialog.FileName);

                    var openResult = MessageBox.Show(
                        $"Đã xuất PDF thành công:\n{dialog.FileName}\n\nBạn có muốn mở file không?",
                        "Xuất PDF",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (openResult == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            bool mainWindowOpen = Application.Current.Windows.OfType<MainWindow>().Any();
            if (!mainWindowOpen)
            {
                OpenMainWindow();
            }
            this.Close();
        }

        private void dgLatestNotes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgLatestNotes.SelectedItem is Note note)
            {
                MainWindow mainWindow = OpenMainWindow();
                mainWindow.SelectNoteById(note.Id);
            }
        }

        /// <summary>
        /// Brings the existing MainWindow to the front or instantiates a new one if it's not open.
        /// </summary>
        private static MainWindow OpenMainWindow()
        {
            MainWindow? existing = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (existing != null)
            {
                existing.Activate();
                return existing;
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                return mainWindow;
            }
        }
    }
}

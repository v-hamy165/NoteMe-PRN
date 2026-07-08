using NoteMe.Models;
using NoteMe.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NoteMe
{
    public partial class MainWindow : Window
    {
        private readonly NoteService noteService = new NoteService();
        private readonly CategoryService categoryService = new CategoryService();
        private readonly AudioService audioService = new AudioService();

        private int selectedNoteId = 0;
        private int selectedAudioId = 0;
        private int currentUserId = 0;
        private const string AllCategoriesFilter = "Tất cả danh mục";

        public MainWindow()
        {
            InitializeComponent();

            if (AppSession.CurrentUser != null)
            {
                currentUserId = AppSession.CurrentUser.Id;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (currentUserId == 0)
            {
                MessageBox.Show("Bạn cần đăng nhập trước.");
                this.Close();
                return;
            }

            LoadCategories();
            LoadNotes();
            LoadAudios();
            TaskReminderManager.Start(currentUserId);
        }

        private void LoadCategories()
        {
            string? selectedFilter = cboFilterCategory.SelectedItem as string;

            cboCategory.ItemsSource = null;
            cboCategory.ItemsSource = categoryService.GetCategoriesByUser(currentUserId);

            var filterCategories = categoryService
                .GetCategoriesByUser(currentUserId)
                .Select(c => c.Name)
                .ToList();

            filterCategories.Insert(0, AllCategoriesFilter);

            cboFilterCategory.ItemsSource = null;
            cboFilterCategory.ItemsSource = filterCategories;

            if (!string.IsNullOrWhiteSpace(selectedFilter) &&
                filterCategories.Contains(selectedFilter))
            {
                cboFilterCategory.SelectedItem = selectedFilter;
            }
            else
            {
                cboFilterCategory.SelectedIndex = 0;
            }
        }

        private void LoadNotes()
        {
            string keyword = txtSearch.Text.Trim();
            string? selectedCategory = cboFilterCategory.SelectedItem as string;

            if (selectedCategory == AllCategoriesFilter)
            {
                selectedCategory = null;
            }

            dgNotes.ItemsSource = null;
            dgNotes.ItemsSource = noteService.SearchNotes(
                currentUserId,
                keyword,
                selectedCategory
            );
        }

        private void LoadAudios()
        {
            selectedAudioId = 0;
            dgAudios.ItemsSource = null;

            if (selectedNoteId == 0)
            {
                txtAudioStatus.Text = "Chọn ghi chú để ghi âm hoặc phát lại.";
                return;
            }

            var audios = audioService.GetAudiosByNote(selectedNoteId, currentUserId);

            dgAudios.ItemsSource = audios;
            txtAudioStatus.Text = audios.Any()
                ? $"Đã gắn {audios.Count} file ghi âm."
                : "Ghi chú này chưa có file ghi âm.";
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề ghi chú.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtContent.Text))
            {
                MessageBox.Show("Vui lòng nhập nội dung ghi chú.");
                return false;
            }

            if (cboCategory.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục.");
                return false;
            }

            return true;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            Note note = new Note
            {
                Title = txtTitle.Text.Trim(),
                Content = txtContent.Text.Trim(),
                Category = cboCategory.SelectedValue.ToString() ?? string.Empty
            };

            noteService.AddNote(note, currentUserId);

            MessageBox.Show("Thêm ghi chú thành công.");

            LoadNotes();
            ClearForm();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (selectedNoteId == 0)
            {
                MessageBox.Show("Vui lòng chọn ghi chú cần sửa.");
                return;
            }

            if (!ValidateInput())
            {
                return;
            }

            Note note = new Note
            {
                Id = selectedNoteId,
                Title = txtTitle.Text.Trim(),
                Content = txtContent.Text.Trim(),
                Category = cboCategory.SelectedValue.ToString() ?? string.Empty
            };

            noteService.UpdateNote(note, currentUserId);

            MessageBox.Show("Cập nhật ghi chú thành công.");

            LoadNotes();
            ClearForm();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedNoteId == 0)
            {
                MessageBox.Show("Vui lòng chọn ghi chú cần xóa.");
                return;
            }

            if (audioService.IsRecording)
            {
                MessageBox.Show("Vui lòng dừng ghi âm trước khi xóa ghi chú.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa ghi chú này không?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                noteService.DeleteNote(selectedNoteId, currentUserId);

                MessageBox.Show("Xóa ghi chú thành công.");

                LoadNotes();
                ClearForm();
            }
        }

        private void dgNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgNotes.SelectedItem is Note note)
            {
                selectedNoteId = note.Id;

                txtTitle.Text = note.Title;
                txtContent.Text = note.Content;

                cboCategory.SelectedValue = note.Category;
                LoadAudios();
            }
        }

        private void dgAudios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAudios.SelectedItem is AudioRecording audio)
            {
                selectedAudioId = audio.Id;
            }
        }

        // Ham ghi am
        private void btnStartRecording_Click(object sender, RoutedEventArgs e)
        {
            if (selectedNoteId == 0)
            {
                MessageBox.Show("Vui lòng chọn ghi chú để gắn file ghi âm.");
                return;
            }

            try
            {
                audioService.StartRecording(selectedNoteId, currentUserId);

                btnStartRecording.IsEnabled = false;
                btnStopRecording.IsEnabled = true;
                txtAudioStatus.Text = "Đang ghi âm...";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Ham dung ghi am
        private async void btnStopRecording_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AudioRecording? audio = await audioService.StopRecordingAsync();

                if (audio == null)
                {
                    MessageBox.Show("File ghi âm rỗng nên không được lưu.");
                }
                else
                {
                    MessageBox.Show("Dừng ghi âm và lưu file thành công.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnStartRecording.IsEnabled = true;
                btnStopRecording.IsEnabled = false;
                LoadAudios();
            }
        }

        // Ham phat lai audio
        private void btnPlayAudio_Click(object sender, RoutedEventArgs e)
        {
            if (selectedNoteId == 0)
            {
                MessageBox.Show("Vui lòng chọn ghi chú.");
                return;
            }

            if (selectedAudioId == 0)
            {
                MessageBox.Show("Vui lòng chọn file ghi âm cần phát.");
                return;
            }

            try
            {
                audioService.PlayAudio(selectedAudioId, selectedNoteId, currentUserId);
                txtAudioStatus.Text = "Đang phát lại file ghi âm.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnAiSummary_Click(object sender, RoutedEventArgs e)
        {
            if (dgNotes.SelectedItem is not Note note)
            {
                MessageBox.Show("Vui lòng chọn ghi chú cần tóm tắt.");
                return;
            }

            var summaryWindow = new SummaryWindow(note.Id, note.Title, currentUserId)
            {
                Owner = this
            };

            summaryWindow.ShowDialog();
        }

        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = txtNewCategory.Text.Trim();

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Vui lòng nhập tên danh mục.");
                return;
            }

            bool isAdded = categoryService.AddCategory(currentUserId, categoryName);

            if (!isAdded)
            {
                MessageBox.Show("Danh mục này đã tồn tại.");
                return;
            }

            MessageBox.Show("Thêm danh mục thành công.");

            LoadCategories();

            cboCategory.SelectedValue = categoryName;
            txtNewCategory.Clear();
        }

        private void cboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCategory.SelectedItem is Category category)
            {
                txtNewCategory.Text = category.Name;
            }
        }

        private void btnUpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            if (cboCategory.SelectedItem is not Category category)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần sửa.");
                return;
            }

            string newName = txtNewCategory.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Vui lòng nhập tên danh mục mới.");
                return;
            }

            bool isUpdated = categoryService.UpdateCategory(
                currentUserId,
                category.Id,
                newName
            );

            if (!isUpdated)
            {
                MessageBox.Show("Tên danh mục đã tồn tại hoặc danh mục không còn tồn tại.");
                return;
            }

            LoadCategories();
            LoadNotes();
            ClearForm();
            cboCategory.SelectedValue = newName;

            MessageBox.Show("Sửa danh mục thành công.");
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (cboCategory.SelectedItem is not Category category)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần xóa.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa danh mục '{category.Name}' không?",
                "Xác nhận xóa danh mục",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            bool isDeleted = categoryService.DeleteCategory(
                currentUserId,
                category.Id
            );

            if (!isDeleted)
            {
                MessageBox.Show(
                    "Không thể xóa danh mục đang được sử dụng bởi ghi chú. " +
                    "Hãy chuyển hoặc xóa các ghi chú trong danh mục trước."
                );
                return;
            }

            LoadCategories();
            ClearForm();
            MessageBox.Show("Xóa danh mục thành công.");
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentUserId != 0)
            {
                LoadNotes();
            }
        }

        private void cboFilterCategory_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e
        )
        {
            if (currentUserId != 0)
            {
                LoadNotes();
            }
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cboFilterCategory.SelectedIndex = 0;
            LoadNotes();
        }

        private void btnOpenDashboard_Click(object sender, RoutedEventArgs e)
        {
            NoteMe.Dashboard.DashboardWindow? existing = Application.Current.Windows
                .OfType<NoteMe.Dashboard.DashboardWindow>()
                .FirstOrDefault();

            if (existing != null)
            {
                existing.Activate();
            }
            else
            {
                NoteMe.Dashboard.DashboardWindow dashboard = new NoteMe.Dashboard.DashboardWindow();
                dashboard.Show();
            }
        }

        private void btnOpenTasks_Click(object sender, RoutedEventArgs e)
        {
            TaskWindow? existing = Application.Current.Windows.OfType<TaskWindow>().FirstOrDefault();
            if (existing != null) existing.Activate();
            else new TaskWindow().Show();
        }

        private void ClearForm()
        {
            selectedNoteId = 0;
            selectedAudioId = 0;

            txtTitle.Clear();
            txtContent.Clear();
            txtNewCategory.Clear();

            cboCategory.SelectedIndex = -1;
            dgNotes.SelectedItem = null;
            dgAudios.ItemsSource = null;
            txtAudioStatus.Text = "Chọn ghi chú để ghi âm hoặc phát lại.";
        }

        public void TriggerClearForm()
        {
            ClearForm();
            txtTitle.Focus();
        }

        public void SelectNoteById(int noteId)
        {
            if (dgNotes.ItemsSource is System.Collections.IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is Note note && note.Id == noteId)
                    {
                        dgNotes.SelectedItem = note;
                        dgNotes.ScrollIntoView(note);
                        break;
                    }
                }
            }
        }

        public void FocusCategorySection()
        {
            txtNewCategory.Focus();
        }

        public void FocusAudioSection()
        {
            btnStartRecording.Focus();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            audioService.Dispose();
            base.OnClosing(e);
        }
    }
}

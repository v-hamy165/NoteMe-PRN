using NoteMe.Models;
using NoteMe.Services;
using System.Windows;
using System.Windows.Controls;

namespace NoteMe
{
    public partial class MainWindow : Window
    {
        private readonly NoteService noteService = new NoteService();
        private readonly CategoryService categoryService = new CategoryService();

        private int selectedNoteId = 0;
        private int currentUserId = 0;

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
        }

        private void LoadCategories()
        {
            cboCategory.ItemsSource = null;
            cboCategory.ItemsSource = categoryService.GetCategoriesByUser(currentUserId);
        }

        private void LoadNotes()
        {
            dgNotes.ItemsSource = null;
            dgNotes.ItemsSource = noteService.GetAllNotes(currentUserId);
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
            }
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

        private void ClearForm()
        {
            selectedNoteId = 0;

            txtTitle.Clear();
            txtContent.Clear();
            txtNewCategory.Clear();

            cboCategory.SelectedIndex = -1;
            dgNotes.SelectedItem = null;
        }
    }
}

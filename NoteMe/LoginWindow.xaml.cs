using NoteMe.Dashboard;
using NoteMe.Models;
using NoteMe.Services;
using System.Windows;

namespace NoteMe
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService authService = new AuthService();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            authService.CreateDefaultAdminAccount();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu.");
                return;
            }

            User? user = authService.Login(username, password);

            if (user != null)
            {
                AppSession.CurrentUser = user;

                DashboardWindow dashboardWindow = new DashboardWindow();
                dashboardWindow.Show();

                this.Close();
            }
            else
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng.");
            }
        }

        private void btnShowRegister_Click(object sender, RoutedEventArgs e)
        {
            SetRegisterMode(true);
        }

        private void btnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            SetRegisterMode(false);
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập.");
                return;
            }

            if (username.Length > 50)
            {
                MessageBox.Show("Tên đăng nhập không được vượt quá 50 ký tự.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu.");
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự.");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp.");
                return;
            }

            bool isRegistered = authService.Register(username, password);

            if (!isRegistered)
            {
                MessageBox.Show("tên đăng nhập đã tồn tại");
                return;
            }

            MessageBox.Show("Đăng ký tài khoản thành công. Vui lòng đăng nhập.");

            SetRegisterMode(false);
            txtUsername.Text = username;
            txtPassword.Clear();
            txtPassword.Focus();
        }

        private void SetRegisterMode(bool isRegisterMode)
        {
            registerFields.Visibility = isRegisterMode
                ? Visibility.Visible
                : Visibility.Collapsed;

            btnLogin.Visibility = isRegisterMode
                ? Visibility.Collapsed
                : Visibility.Visible;
            btnShowRegister.Visibility = isRegisterMode
                ? Visibility.Collapsed
                : Visibility.Visible;
            btnRegister.Visibility = isRegisterMode
                ? Visibility.Visible
                : Visibility.Collapsed;
            btnBackToLogin.Visibility = isRegisterMode
                ? Visibility.Visible
                : Visibility.Collapsed;

            txtFormSubtitle.Text = isRegisterMode
                ? "Tạo tài khoản NoteMe mới"
                : "Đăng nhập tài khoản của bạn";

            txtPassword.Clear();
            txtConfirmPassword.Clear();
            txtUsername.Focus();
        }
    }
}

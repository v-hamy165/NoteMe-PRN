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
    }
}

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MultiFuelMaster.Services;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;
        public User? LoggedInUser { get; private set; }

        public LoginWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await TryLoginAsync();
        }

        private async void LoginTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TryLoginAsync();
            }
        }

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TryLoginAsync();
            }
        }

        private async Task TryLoginAsync()
        {
            var login = LoginTextBox.Text?.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Пожалуйста, введите логин и пароль");
                return;
            }

            // Show loading
            LoadingBar.Visibility = Visibility.Visible;
            LoginButton.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;

            try
            {
                var (success, user, message) = await _authService.LoginAsync(login, password);

                if (success && user != null)
                {
                    LoggedInUser = user;
                    // Close with success
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                LoginButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}

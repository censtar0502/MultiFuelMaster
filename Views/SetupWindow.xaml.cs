using System;
using System.Threading.Tasks;
using System.Windows;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.Views
{
    public partial class SetupWindow : Window
    {
        private readonly AuthService _authService;
        private bool _created = false;

        public SetupWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            await TryCreateAdminAsync();
        }

        private async Task TryCreateAdminAsync()
        {
            var fullName = FullNameTextBox.Text?.Trim();
            var login = LoginTextBox.Text?.Trim();
            var password = PasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            // Validate input
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Пожалуйста, заполните все поля");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Пароль должен содержать минимум 4 символа");
                return;
            }

            // Show loading
            LoadingBar.Visibility = Visibility.Visible;
            CreateButton.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;
            SuccessText.Visibility = Visibility.Collapsed;

            try
            {
                var (success, message) = await _authService.CreateSuperAdminAsync(login, password, fullName);

                if (success)
                {
                    SuccessText.Text = message + "\nТеперь вы можете войти в систему";
                    SuccessText.Visibility = Visibility.Visible;
                    _created = true;
                    
                    // Wait a moment and close
                    await Task.Delay(1500);
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
                CreateButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
            SuccessText.Visibility = Visibility.Collapsed;
        }
    }
}

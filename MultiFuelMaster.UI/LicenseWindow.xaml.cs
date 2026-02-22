using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultiFuelMaster.UI
{
    public partial class LicenseWindow : Window
    {
        private readonly LicenseManager _licenseManager;
        private readonly int _daysRemaining;

        public LicenseWindow(int trialDaysRemaining = 10)
        {
            InitializeComponent();

            _licenseManager = new LicenseManager();
            _daysRemaining  = trialDaysRemaining;

            // Отображаем HWID
            string hwid = _licenseManager.GetHardwareID();
            if (hwid.Length > 16)
                hwid = Regex.Replace(hwid, "(.{8})", "$1-").TrimEnd('-');
            HwidText.Text = hwid;

            // Статус
            UpdateStatus();

            LicenseKeyTextBox.PreviewTextInput += LicenseKeyTextBox_PreviewTextInput;
            LicenseKeyTextBox.PreviewKeyDown   += LicenseKeyTextBox_PreviewKeyDown;
        }

        private void UpdateStatus()
        {
            var info = _licenseManager.CheckLicense();
            if (info.IsActivated && info.Status == LicenseStatus.Valid)
            {
                StatusText.Text       = "✓ Лицензия активирована";
                StatusText.Foreground = FindResource("GreenBrush") as System.Windows.Media.Brush;
                DaysText.Text         = "Бессрочная лицензия";
                SlotsText.Text        = $"Активных ключей: {info.ActiveKeys}  (постов: {info.MaxPanels})";
                ActivateButton.Content = "Добавить ключ";
            }
            else if (_daysRemaining <= 0)
            {
                StatusText.Text       = "✗ Пробный период истёк";
                StatusText.Foreground = FindResource("RedBrush") as System.Windows.Media.Brush;
                DaysText.Text         = "Требуется активация";
                SlotsText.Text        = "";
            }
            else
            {
                StatusText.Text       = "Пробный период";
                StatusText.Foreground = FindResource("TextPrimaryBrush") as System.Windows.Media.Brush;
                DaysText.Text         = $"Осталось: {_daysRemaining} дней";
                SlotsText.Text        = "Доступно: 1 пост (триал)";
            }
        }

        private void LicenseKeyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "[A-Za-z0-9\\-]");
        }

        private void LicenseKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back && e.Key != Key.Delete && sender is TextBox tb)
            {
                int idx = tb.CaretIndex;
                string text = tb.Text.Replace("-", "");
                // Авто-тире каждые 5 символов (без учёта уже введённых)
                int rawLen = text.Length;
                if (rawLen > 0 && rawLen % 5 == 0 && rawLen < 25 && !tb.Text.EndsWith("-"))
                {
                    int insertAt = tb.Text.Length;
                    tb.Text = tb.Text.Insert(insertAt, "-");
                    tb.CaretIndex = insertAt + 1;
                    e.Handled = true;
                }
            }
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            string licenseKey = LicenseKeyTextBox.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(licenseKey))
            {
                MessageBox.Show("Введите лицензионный ключ.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string cleanKey = licenseKey.Replace("-", "");
            if (cleanKey.Length != 25)
            {
                MessageBox.Show("Неверный формат ключа. Ключ должен содержать 25 символов.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool success = _licenseManager.ActivateLicense(licenseKey);
            if (success)
            {
                var updatedInfo = _licenseManager.CheckLicense();
                MessageBox.Show(
                    $"Ключ успешно добавлен!\n\nАктивных ключей: {updatedInfo.ActiveKeys}\nМакс. постов: {updatedInfo.MaxPanels}\n\nСпасибо за приобретение MultiFuelMaster.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateStatus();
                LicenseKeyTextBox.Clear();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(
                    "Ошибка активации.\n\nВозможные причины:\n- Неверный ключ\n- Ключ уже активирован\n- Ключ для другого компьютера",
                    "Ошибка активации", MessageBoxButton.OK, MessageBoxImage.Error);
                LicenseKeyTextBox.Clear();
                LicenseKeyTextBox.Focus();
            }
        }

        private void CopyHwidButton_Click(object sender, RoutedEventArgs e)
        {
            string hwid = _licenseManager.GetHardwareID();
            Clipboard.SetText(hwid);
            CopyHwidButton.Content = "Скопировано!";

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, args) => { CopyHwidButton.Content = "Копировать"; timer.Stop(); };
            timer.Start();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

using System;
using System.Windows;

namespace MultiFuelMaster.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверка лицензии
            var lm = new LicenseManager();
            var status = lm.CheckLicense();

            if (status.Status == LicenseStatus.TrialExpired)
            {
                // Показываем окно лицензии; если не активирована — выходим
                var licWin = new LicenseWindow(0);
                bool? result = licWin.ShowDialog();
                // Перепроверяем после попытки активации
                status = lm.CheckLicense();
                if (status.MaxPanels <= 0)
                {
                    Shutdown();
                    return;
                }
            }
            else if (status.Status == LicenseStatus.Trial && status.DaysRemaining <= 3)
            {
                MessageBox.Show(
                    $"Пробный период заканчивается!\nОсталось: {status.DaysRemaining} дней.\n\nПожалуйста, приобретите лицензию.",
                    "MultiFuelMaster", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // TODO: убрать хардкод после тестирования
            int maxPanels = 3; // Math.Max(1, status.MaxPanels);

            // Создаём и показываем главное MDI-окно
            var mainWindow = new MainWindow(maxPanels);
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace MultiFuelMaster.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var lm = new LicenseManager();
            var status = lm.CheckLicense();

            if (status.Status == LicenseStatus.TrialExpired)
            {
                var licWin = new LicenseWindow(0);
                licWin.ShowDialog();
                status = lm.CheckLicense();
                if (status.MaxPanels <= 0 && status.Status == LicenseStatus.TrialExpired)
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

            // Какие слоты (посты) лицензированы
            HashSet<int> licensedSlots = lm.GetLicensedSlots();

            // Количество постов из конфига (settings_global.json → PanelCount)
            int panelCount = ReadPanelCount();

            // Если конфиг не задан: кол-во постов = кол-во ключей (min 1 при триале)
            if (panelCount <= 0)
                panelCount = Math.Max(licensedSlots.Count, status.Status == LicenseStatus.Trial ? 1 : 0);

            if (panelCount <= 0) panelCount = 1;

            var mainWindow = new MainWindow(panelCount, licensedSlots, status.Status == LicenseStatus.Trial);
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        /// <summary>
        /// Читает PanelCount из settings_global.json.
        /// Возвращает 0 если файл не существует или нет поля.
        /// </summary>
        private static int ReadPanelCount()
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MultiFuelMaster", "settings_global.json");

                if (!File.Exists(path)) return 0;

                string json = File.ReadAllText(path);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("PanelCount", out var prop))
                    return prop.GetInt32();
            }
            catch { }
            return 0;
        }
    }
}

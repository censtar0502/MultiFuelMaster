using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Сервис/Система: сведения о приложении и базовые параметры (UI-скелет).
    /// </summary>
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly Action _onClose;

        [ObservableProperty]
        private string _applicationVersion = "—";

        [ObservableProperty]
        private string _frameworkVersion = "—";

        [ObservableProperty]
        private string _databasePath = "—";

        [ObservableProperty]
        private string _logDirectory = "—";

        [ObservableProperty]
        private bool _autoRefreshEnabled = true;

        [ObservableProperty]
        private int _refreshIntervalMinutes = 5;

        [ObservableProperty]
        private int _dataRetentionDays = 365;

        public SettingsViewModel(Action onClose)
        {
            _onClose = onClose;

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var version = asm.GetName().Version;
                ApplicationVersion = version != null ? version.ToString() : "—";
                FrameworkVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

                var baseDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MultiFuelMaster");
                DatabasePath = System.IO.Path.Combine(baseDir, "fuelmaster.db");

                LogDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }
            catch
            {
                // UI-only: игнор
            }
        }
    }
}

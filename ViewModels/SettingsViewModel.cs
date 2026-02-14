using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Settings view model (read-only settings display)
    /// </summary>
    public partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _applicationVersion = "1.0.0";

        [ObservableProperty]
        private string _databasePath = "fuelmaster.db";

        [ObservableProperty]
        private int _dataRetentionDays = 365;

        [ObservableProperty]
        private bool _autoRefreshEnabled = true;

        [ObservableProperty]
        private int _refreshIntervalMinutes = 5;

        public SettingsViewModel()
        {
            // Load settings from configuration
            LoadSettings();
        }

        private void LoadSettings()
        {
            // In a real application, load from app.config or registry
            // For now, using default values
        }
    }
}
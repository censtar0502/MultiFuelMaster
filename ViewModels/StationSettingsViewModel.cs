using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    public partial class StationSettingsViewModel : BaseViewModel
    {
        private readonly StationSettingsService _settingsService;
        private readonly Action _onComplete;

        [ObservableProperty]
        private string _stationName = "";

        [ObservableProperty]
        private string _stationAddress = "";

        [ObservableProperty]
        private string _companyName = "";

        [ObservableProperty]
        private string _selectedLanguage = "ru";

        [ObservableProperty]
        private string _selectedCurrency = "UZS";

        [ObservableProperty]
        private string _archivePath = "";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "";

        public string[] Languages { get; } = { "ru", "uz", "en" };
        public string[] LanguagesDisplay { get; } = { "Русский", "Узбекский", "Английский" };
        
        public string[] Currencies { get; } = { "UZS", "USD", "EUR", "RUB" };
        public string[] CurrenciesDisplay { get; } = { "Узбекский сум", "Доллар США", "Евро", "Российский рубль" };

        public StationSettingsViewModel(StationSettingsService settingsService, Action onComplete)
        {
            _settingsService = settingsService;
            _onComplete = onComplete;
            LoadSettingsAsync();
        }

        private async void LoadSettingsAsync()
        {
            try
            {
                IsLoading = true;
                var settings = await _settingsService.GetDisplayModelAsync();
                
                StationName = settings.StationName ?? "";
                StationAddress = settings.StationAddress ?? "";
                CompanyName = settings.CompanyName ?? "";
                SelectedLanguage = settings.Language;
                SelectedCurrency = settings.Currency;
                ArchivePath = settings.ArchivePath ?? "";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Сохранение...";
                
                var model = new StationSettingsDisplayModel
                {
                    StationName = StationName,
                    StationAddress = StationAddress,
                    CompanyName = CompanyName,
                    Language = SelectedLanguage,
                    Currency = SelectedCurrency,
                    ArchivePath = ArchivePath
                };
                
                await _settingsService.SaveSettingsAsync(model);
                StatusMessage = "Настройки сохранены!";
                
                // Wait a moment then close
                await Task.Delay(1000);
                _onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            // Reload original values
            LoadSettingsAsync();
            StatusMessage = "Изменения отменены!";
            
            // Wait a moment then close
            Task.Delay(1000).ContinueWith(_ => 
            {
                Application.Current.Dispatcher.Invoke(() => _onComplete?.Invoke());
            });
        }
    }
}

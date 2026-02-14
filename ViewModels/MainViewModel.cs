using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Views;
using MultiFuelMaster.Services;
using MultiFuelMaster.Models;
using MultiFuelMaster.ViewModels;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Main application view model
    /// </summary>
    public partial class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly StationSettingsService _stationSettingsService;
        private readonly FuelTypeService _fuelTypeService;

        [ObservableProperty]
        private object _currentView = null!;

        [ObservableProperty]
        private string _windowTitle = "MultiFuelMaster - Система управления АЗС";

        [ObservableProperty]
        private bool _isNavigationEnabled = true;

        [ObservableProperty]
        private string _currentUserLogin = "";

        [ObservableProperty]
        private bool _isAdminMenuVisible = false;

        public MainViewModel(DatabaseService databaseService, StationSettingsService stationSettingsService, FuelTypeService fuelTypeService, User? currentUser = null)
        {
            _databaseService = databaseService;
            _stationSettingsService = stationSettingsService;
            _fuelTypeService = fuelTypeService;
            
            // Set current user info
            if (currentUser != null)
            {
                CurrentUserLogin = currentUser.Login;
                IsAdminMenuVisible = currentUser.Role == UserRole.SuperAdmin;
            }
            
            // Initialize with empty dashboard view
            NavigateToEmptyDashboard();
        }

        [RelayCommand]
        private void NavigateToEmptyDashboard()
        {
            CurrentView = new EmptyDashboardView();
            WindowTitle = "MultiFuelMaster - Панель управления";
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            var viewModel = new DashboardViewModel(_databaseService);
            CurrentView = new DashboardView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster - Панель управления";
        }

        [RelayCommand]
        private void NavigateToStations()
        {
            var viewModel = new StationsViewModel(_databaseService);
            CurrentView = new StationsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster - Станции";
        }

        [RelayCommand]
        private void NavigateToStationSettings()
        {
            var viewModel = new StationSettingsViewModel(_stationSettingsService, NavigateToEmptyDashboard);
            CurrentView = new StationSettingsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster - Параметры АЗС";
        }

        [RelayCommand]
        private void NavigateToFuelTypes()
        {
            var viewModel = new FuelTypesViewModel(_fuelTypeService, NavigateToEmptyDashboard);
            CurrentView = new FuelTypesView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster - Виды топлива";
        }

        [RelayCommand]
        private void NavigateToTransactions()
        {
            var viewModel = new TransactionsViewModel(_databaseService);
            CurrentView = new TransactionsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster - Транзакции";
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            var viewModel = new ReportsViewModel(_databaseService);
            CurrentView = new ReportsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster - Отчеты";
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            CurrentView = new SettingsView();
            WindowTitle = "MultiFuelMaster - Настройки";
        }
    }
}
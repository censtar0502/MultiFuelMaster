using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Models;
using MultiFuelMaster.Services;
using MultiFuelMaster.Views;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Main application view model (navigation + shell state)
    /// </summary>
    public partial class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly StationSettingsService _stationSettingsService;
        private readonly FuelTypeService _fuelTypeService;
        private readonly TankService _tankService;
        private readonly UserService _userService;
        private readonly RuntimeStateService _runtimeState;

        private User? _currentUser;
        private readonly Action _onExit;

        [ObservableProperty]
        private object _currentView = null!;

        [ObservableProperty]
        private string _windowTitle = "MultiFuelMaster — Система управления АЗС";

        [ObservableProperty]
        private bool _isNavigationEnabled = true;

        [ObservableProperty]
        private string _currentUserLogin = "";

        [ObservableProperty]
        private bool _isAdminMenuVisible = false;

        // StatusBar (оперативная строка)
        [ObservableProperty]
        private string _statusShift = "Смена: закрыта";

        [ObservableProperty]
        private string _statusPosts = "Посты: 0/8 онлайн";

        [ObservableProperty]
        private string _statusAlerts = "Тревоги: 0";

        public MainViewModel(
            DatabaseService databaseService,
            StationSettingsService stationSettingsService,
            FuelTypeService fuelTypeService,
            TankService tankService,
            UserService userService,
            RuntimeStateService runtimeState,
            User? currentUser = null,
            Action? onExit = null)
        {
            _databaseService = databaseService;
            _stationSettingsService = stationSettingsService;
            _fuelTypeService = fuelTypeService;
            _tankService = tankService;
            _userService = userService;
            _runtimeState = runtimeState;

            _onExit = onExit ?? (() => System.Windows.Application.Current.Shutdown());

            if (currentUser != null)
            {
                _currentUser = currentUser;
                CurrentUserLogin = currentUser.Login;

                // СуперАдмин(1), Админ(2), Конфигуратор(5) — видят меню конфигурации
                IsAdminMenuVisible = currentUser.RoleId == 1 || currentUser.RoleId == 2 || currentUser.RoleId == 5;

                currentUser.LoginTime = DateTime.Now;
                LoggingService.Instance?.LogLogin(currentUser.Login);
            }

            _runtimeState.PropertyChanged += (_, __) => UpdateStatusBar();
            UpdateStatusBar();

            // Стартовая страница — Панель
            NavigateToPanel();
        }

        private void UpdateStatusBar()
        {
            StatusShift = _runtimeState.Shift.StatusTextRu;
            StatusPosts = $"Посты: {_runtimeState.OnlinePostsCount}/{_runtimeState.TotalPostsCount} онлайн";
            StatusAlerts = $"Тревоги: {_runtimeState.ActiveAlertsCount}";
        }

        [RelayCommand]
        private void NavigateToPanel()
        {
            var viewModel = new DashboardViewModel(_databaseService, _runtimeState);
            CurrentView = new DashboardView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Панель";
        }

        [RelayCommand]
        private void NavigateToTransactions()
        {
            var viewModel = new TransactionsViewModel(_databaseService);
            CurrentView = new TransactionsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Транзакции";
        }

        [RelayCommand]
        private void NavigateToShift()
        {
            var viewModel = new ShiftViewModel(_runtimeState, CurrentUserLogin);
            CurrentView = new ShiftView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Смена";
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            var viewModel = new ReportsViewModel(_databaseService);
            CurrentView = new ReportsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Отчёты";
        }

        [RelayCommand]
        private void NavigateToDiagnostics()
        {
            var viewModel = new DiagnosticsViewModel(_runtimeState);
            CurrentView = new DiagnosticsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Диагностика";
        }

        [RelayCommand]
        private void NavigateToService()
        {
            var viewModel = new SettingsViewModel(NavigateToPanel);
            CurrentView = new SettingsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Сервис";
        }

        // Конфигурация (левое меню)
        [RelayCommand]
        private void NavigateToStationSettings()
        {
            var viewModel = new StationSettingsViewModel(_stationSettingsService, NavigateToPanel);
            CurrentView = new StationSettingsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Параметры АЗС";
        }

        [RelayCommand]
        private void NavigateToUsers()
        {
            var viewModel = new UsersViewModel(_userService, NavigateToPanel);
            CurrentView = new UsersView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Пользователи и роли";
        }

        [RelayCommand]
        private void NavigateToFuelTypes()
        {
            var viewModel = new FuelTypesViewModel(_fuelTypeService, NavigateToPanel);
            CurrentView = new FuelTypesView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Топливо";
        }

        [RelayCommand]
        private void NavigateToTanks()
        {
            var viewModel = new TanksViewModel(_tankService, NavigateToPanel);
            CurrentView = new TanksView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Резервуары";
        }

        [RelayCommand]
        private void NavigateToPostsAndConnection()
        {
            var viewModel = new PostsAndConnectionViewModel(NavigateToPanel);
            CurrentView = new PostsAndConnectionView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Посты и связь";
        }

        [RelayCommand]
        private void NavigateToChannels()
        {
            var viewModel = new ChannelsViewModel(NavigateToPanel);
            CurrentView = new ChannelsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Каналы и пистолеты";
        }

        [RelayCommand]
        private void NavigateToSalesParameters()
        {
            var viewModel = new SalesParametersViewModel(NavigateToPanel);
            CurrentView = new SalesParametersView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Параметры продаж";
        }

        [RelayCommand]
        private void NavigateToSystem()
        {
            var viewModel = new SettingsViewModel(NavigateToPanel);
            CurrentView = new SettingsView { DataContext = viewModel };
            WindowTitle = "MultiFuelMaster — Система";
        }

        [RelayCommand]
        private void Exit()
        {
            if (_currentUser != null)
            {
                _currentUser.LogoutTime = DateTime.Now;
                LoggingService.Instance?.LogLogout(_currentUser.Login);
            }

            _onExit?.Invoke();
        }
    }
}

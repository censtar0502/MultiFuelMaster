using CommunityToolkit.Mvvm.ComponentModel;
using MultiFuelMaster.Models;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Dashboard view model
    /// </summary>
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private List<FuelStation> _stations = new();

        [ObservableProperty]
        private int _totalStations;

        [ObservableProperty]
        private int _totalDispensers;

        [ObservableProperty]
        private decimal _todayRevenue;

        [ObservableProperty]
        private int _todayTransactions;

        public DashboardViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _ = LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                // Load stations
                Stations = await _databaseService.GetAllStationsAsync();
                TotalStations = Stations.Count;

                // Calculate total dispensers
                foreach (var station in Stations)
                {
                    var dispensers = await _databaseService.GetDispensersByStationAsync(station.Id);
                    TotalDispensers += dispensers.Count;
                }

                // Load today's statistics
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var stats = await _databaseService.GetTransactionStatisticsAsync(today, tomorrow);
                
                TodayRevenue = stats.TotalRevenue;
                TodayTransactions = stats.TotalTransactions;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
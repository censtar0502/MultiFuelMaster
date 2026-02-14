using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Models;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Stations management view model
    /// </summary>
    public partial class StationsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private List<FuelStation> _stations = new();

        [ObservableProperty]
        private FuelStation? _selectedStation;

        [ObservableProperty]
        private List<Dispenser> _dispensers = new();

        public StationsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _ = LoadStationsAsync();
        }

        private async Task LoadStationsAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                Stations = await _databaseService.GetAllStationsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки станций: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SelectStation(FuelStation? station)
        {
            if (station == null) return;

            try
            {
                SelectedStation = station;
                Dispensers = await _databaseService.GetDispensersByStationAsync(station.Id);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки раздатчиков: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadStationsAsync();
            if (SelectedStation != null)
            {
                await SelectStation(SelectedStation);
            }
        }
    }
}
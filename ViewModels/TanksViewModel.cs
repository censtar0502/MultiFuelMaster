using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Models;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    public partial class TanksViewModel : BaseViewModel
    {
        private readonly TankService _tankService;
        private readonly Action _onComplete;

        [ObservableProperty]
        private ObservableCollection<Tank> _tanks = new();

        [ObservableProperty]
        private ObservableCollection<FuelType> _availableFuelTypes = new();

        [ObservableProperty]
        private Tank? _selectedTank;

        [ObservableProperty]
        private Tank _editingTank = new();

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isAddingNew;

        [ObservableProperty]
        private string _windowTitle = "Ёмкости";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _allowBlockingDuringArrival = true;

        [ObservableProperty]
        private CriticalLevelControl _criticalLevelControl = CriticalLevelControl.BySalesData;

        public TanksViewModel(TankService tankService, Action onComplete)
        {
            _tankService = tankService;
            _onComplete = onComplete;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                var tanks = await _tankService.GetAllAsync();
                Tanks = new ObservableCollection<Tank>(tanks);

                var fuelTypes = await _tankService.GetAvailableFuelTypesAsync();
                AvailableFuelTypes = new ObservableCollection<FuelType>(fuelTypes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddNew()
        {
            try
            {
                int nextNumber = await _tankService.GetNextNumberAsync();
                EditingTank = new Tank
                {
                    Number = nextNumber,
                    FuelTypeId = AvailableFuelTypes.FirstOrDefault()?.Id ?? 0,
                    MaxVolume = 0,
                    MinVolume = 0,
                    CriticalLevel = 0,
                    CriticalControl = CriticalLevelControl.None,
                    IsBlockedDuringArrival = false,
                    CurrentLevel = 0,
                    Status = TankStatus.Active,
                    IsActive = true
                };
                IsAddingNew = true;
                IsEditing = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Edit()
        {
            if (SelectedTank == null) return;

            EditingTank = new Tank
            {
                Id = SelectedTank.Id,
                Number = SelectedTank.Number,
                FuelTypeId = SelectedTank.FuelTypeId,
                MaxVolume = SelectedTank.MaxVolume,
                MinVolume = SelectedTank.MinVolume,
                CriticalLevel = SelectedTank.CriticalLevel,
                CriticalControl = SelectedTank.CriticalControl,
                IsBlockedDuringArrival = SelectedTank.IsBlockedDuringArrival,
                CurrentLevel = SelectedTank.CurrentLevel,
                Status = SelectedTank.Status,
                IsActive = SelectedTank.IsActive
            };
            IsAddingNew = false;
            IsEditing = true;
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedTank == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить ёмкость №{SelectedTank.Number}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _tankService.DeleteAsync(SelectedTank.Id);
                    Tanks.Remove(SelectedTank);
                    SelectedTank = null;
                    MessageBox.Show("Ёмкость успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (EditingTank.Number <= 0)
            {
                MessageBox.Show("Пожалуйста, введите номер ёмкости.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingTank.FuelTypeId <= 0)
            {
                MessageBox.Show("Пожалуйста, выберите тип топлива.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for duplicate number
            bool numberExists = await _tankService.IsNumberExistsAsync(EditingTank.Number, IsAddingNew ? null : EditingTank.Id);
            if (numberExists)
            {
                MessageBox.Show("Ёмкость с таким номером уже существует.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsAddingNew)
                {
                    var created = await _tankService.CreateAsync(EditingTank);
                    Tanks.Add(created);
                    MessageBox.Show("Ёмкость успешно добавлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _tankService.UpdateAsync(EditingTank);
                    
                    var index = Tanks.ToList().FindIndex(t => t.Id == EditingTank.Id);
                    if (index >= 0)
                    {
                        Tanks[index] = EditingTank;
                    }
                    MessageBox.Show("Ёмкость успешно обновлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                IsEditing = false;
                IsAddingNew = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            IsEditing = false;
            IsAddingNew = false;
            EditingTank = new Tank();
        }
    }
}

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
    public partial class FuelTypesViewModel : BaseViewModel
    {
        private readonly FuelTypeService _fuelTypeService;
        private readonly Action _onComplete;

        [ObservableProperty]
        private ObservableCollection<FuelType> _fuelTypes = new();

        [ObservableProperty]
        private FuelType? _selectedFuelType;

        [ObservableProperty]
        private FuelType _editingFuelType = new();

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isAddingNew;

        [ObservableProperty]
        private string _windowTitle = "Виды топлива";

        [ObservableProperty]
        private bool _isLoading;

        public FuelTypesViewModel(FuelTypeService fuelTypeService, Action onComplete)
        {
            _fuelTypeService = fuelTypeService;
            _onComplete = onComplete;
            _ = LoadFuelTypesAsync();
        }

        private async Task LoadFuelTypesAsync()
        {
            try
            {
                IsLoading = true;
                var fuelTypes = await _fuelTypeService.GetAllAsync();
                FuelTypes = new ObservableCollection<FuelType>(fuelTypes);
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
                int nextNumber = await _fuelTypeService.GetNextNumberAsync();
                EditingFuelType = new FuelType
                {
                    Number = nextNumber,
                    ShortName = "",
                    FullName = "",
                    ReceiptName = "",
                    Unit = "литры",
                    Color = "#000000",
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
            if (SelectedFuelType == null) return;

            EditingFuelType = new FuelType
            {
                Id = SelectedFuelType.Id,
                Number = SelectedFuelType.Number,
                ShortName = SelectedFuelType.ShortName,
                FullName = SelectedFuelType.FullName,
                ReceiptName = SelectedFuelType.ReceiptName,
                Unit = SelectedFuelType.Unit,
                Color = SelectedFuelType.Color,
                IkpuCode = SelectedFuelType.IkpuCode,
                PackageCode = SelectedFuelType.PackageCode,
                OriginCode = SelectedFuelType.OriginCode,
                ClientTin = SelectedFuelType.ClientTin,
                IsActive = SelectedFuelType.IsActive
            };
            IsAddingNew = false;
            IsEditing = true;
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedFuelType == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить вид топлива '{SelectedFuelType.ShortName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _fuelTypeService.DeleteAsync(SelectedFuelType.Id);
                    FuelTypes.Remove(SelectedFuelType);
                    SelectedFuelType = null;
                    MessageBox.Show("Вид топлива успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (string.IsNullOrWhiteSpace(EditingFuelType.ShortName))
            {
                MessageBox.Show("Пожалуйста, введите краткое название.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingFuelType.FullName))
            {
                MessageBox.Show("Пожалуйста, введите полное название.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsAddingNew)
                {
                    var created = await _fuelTypeService.CreateAsync(EditingFuelType);
                    FuelTypes.Add(created);
                    MessageBox.Show("Вид топлива успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _fuelTypeService.UpdateAsync(EditingFuelType);
                    
                    var index = FuelTypes.ToList().FindIndex(f => f.Id == EditingFuelType.Id);
                    if (index >= 0)
                    {
                        FuelTypes[index] = EditingFuelType;
                    }
                    MessageBox.Show("Вид топлива успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
            EditingFuelType = new FuelType();
        }
    }
}

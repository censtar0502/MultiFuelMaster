using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Параметры продаж (UI-скелет): цены, округления, ограничения, печать чека.
    /// </summary>
    public partial class SalesParametersViewModel : BaseViewModel
    {
        private readonly Action _onClose;

        public ObservableCollection<FuelPriceItem> Prices { get; } = new();

        [ObservableProperty]
        private FuelPriceItem? _selected;

        [ObservableProperty]
        private bool _enableRounding = true;

        [ObservableProperty]
        private int _roundingStep = 1;

        [ObservableProperty]
        private bool _enableReceiptPrinting = false;

        public SalesParametersViewModel(Action onClose)
        {
            _onClose = onClose;

            Prices.Add(new FuelPriceItem { FuelName = "А-92", Price = 8750m });
            Prices.Add(new FuelPriceItem { FuelName = "А-95", Price = 9450m });
            Prices.Add(new FuelPriceItem { FuelName = "ДТ", Price = 9800m });

            Selected = Prices.Count > 0 ? Prices[0] : null;
        }

        [RelayCommand]
        private void Save()
        {
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void Close()
        {
            _onClose?.Invoke();
        }
    }

    public sealed partial class FuelPriceItem : ObservableObject
    {
        [ObservableProperty] private string _fuelName = "А-92";
        [ObservableProperty] private decimal _price = 0m;
    }
}

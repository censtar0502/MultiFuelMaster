using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Каналы/пистолеты (UI-скелет конфигурации): привязка к топливу/резервуару, номера.
    /// </summary>
    public partial class ChannelsViewModel : BaseViewModel
    {
        private readonly Action _onClose;

        public ObservableCollection<ChannelConfigItem> Channels { get; } = new();

        [ObservableProperty]
        private ChannelConfigItem? _selected;

        public ChannelsViewModel(Action onClose)
        {
            _onClose = onClose;

            for (int i = 1; i <= 8; i++)
            {
                Channels.Add(new ChannelConfigItem
                {
                    ChannelNumber = i,
                    NozzleNumber = i,
                    FuelName = i % 2 == 0 ? "А-95" : "А-92",
                    TankNumber = (i % 4) + 1
                });
            }

            if (Channels.Count > 0)
                Selected = Channels[0];
        }

        [RelayCommand]
        private void AddChannel()
        {
            var next = Channels.Count + 1;
            Channels.Add(new ChannelConfigItem
            {
                ChannelNumber = next,
                NozzleNumber = next,
                FuelName = "А-92",
                TankNumber = 1
            });
            Selected = Channels[^1];
        }

        [RelayCommand]
        private void RemoveSelected()
        {
            if (Selected == null) return;
            Channels.Remove(Selected);
            Selected = Channels.Count > 0 ? Channels[0] : null;
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

    public sealed partial class ChannelConfigItem : ObservableObject
    {
        [ObservableProperty] private int _channelNumber;
        [ObservableProperty] private int _nozzleNumber;
        [ObservableProperty] private string _fuelName = "А-92";
        [ObservableProperty] private int _tankNumber = 1;
    }
}

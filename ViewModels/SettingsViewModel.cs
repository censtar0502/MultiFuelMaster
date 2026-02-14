using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiFuelMaster.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly Action _onComplete;

        public SettingsViewModel(Action onComplete)
        {
            _onComplete = onComplete;
        }

        [RelayCommand]
        private void Close()
        {
            _onComplete?.Invoke();
        }
    }
}

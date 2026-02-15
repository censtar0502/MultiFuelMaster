using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Смена: открытие/закрытие (UI-скелет). Реальная логика будет привязана к кассе/оператору.
    /// </summary>
    public partial class ShiftViewModel : ObservableObject
    {
        private readonly RuntimeStateService _runtime;

        [ObservableProperty]
        private string _operatorLogin = "";

        public bool IsShiftOpen => _runtime.Shift.IsOpen;
        public string ShiftStatus => _runtime.Shift.StatusTextRu;
        public DateTime? OpenTime => _runtime.Shift.OpenTime;
        public DateTime? CloseTime => _runtime.Shift.CloseTime;

        public bool CanOpen => !IsShiftOpen;
        public bool CanClose => IsShiftOpen;

        public ShiftViewModel(RuntimeStateService runtime, string currentUserLogin)
        {
            _runtime = runtime;
            OperatorLogin = string.IsNullOrWhiteSpace(currentUserLogin) ? "operator" : currentUserLogin;

            _runtime.Shift.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(IsShiftOpen));
                OnPropertyChanged(nameof(ShiftStatus));
                OnPropertyChanged(nameof(OpenTime));
                OnPropertyChanged(nameof(CloseTime));
                OnPropertyChanged(nameof(CanOpen));
                OnPropertyChanged(nameof(CanClose));
            };
        }

        [RelayCommand(CanExecute = nameof(CanOpen))]
        private void OpenShift()
        {
            _runtime.OpenShift(OperatorLogin);
            OpenShiftCommand.NotifyCanExecuteChanged();
            CloseShiftCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanClose))]
        private void CloseShift()
        {
            _runtime.CloseShift(OperatorLogin);
            OpenShiftCommand.NotifyCanExecuteChanged();
            CloseShiftCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void PrintXReport()
        {
            _runtime.Alerts.Insert(0, new Models.Runtime.AlertRuntimeItem
            {
                Severity = Models.Runtime.AlertSeverity.Info,
                Title = "Отчёт X",
                Message = "Печать X-отчёта (UI-скелет)"
            });
        }

        [RelayCommand]
        private void PrintZReport()
        {
            _runtime.Alerts.Insert(0, new Models.Runtime.AlertRuntimeItem
            {
                Severity = Models.Runtime.AlertSeverity.Info,
                Title = "Отчёт Z",
                Message = "Печать Z-отчёта (UI-скелет)"
            });
        }
    }
}

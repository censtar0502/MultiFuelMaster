using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiFuelMaster.Models.Runtime
{
    public enum PostConnectionState
    {
        Offline = 0,
        Online = 1,
        NoResponse = 2,
        Error = 3
    }

    public enum PostOperationState
    {
        Unknown = 0,
        Ready = 1,
        Calling = 2,
        Authorized = 3,
        Started = 4,
        Paused = 5,
        Fuelling = 6,
        Stopping = 7,
        End = 8,
        Error = 9
    }

    public enum AlertSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    public sealed partial class PostRuntimeState : ObservableObject
    {
        [ObservableProperty]
        private int _postNumber = 1;

        public string DisplayName => $"Пост {PostNumber}";

        [ObservableProperty]
        private PostConnectionState _connection = PostConnectionState.Offline;

        [ObservableProperty]
        private PostOperationState _operation = PostOperationState.Unknown;

        [ObservableProperty]
        private string _fuelName = "—";

        [ObservableProperty]
        private decimal _price = 0m;

        [ObservableProperty]
        private double _volumeL = 0.0;

        [ObservableProperty]
        private decimal _amount = 0m;

        [ObservableProperty]
        private string _vehiclePlate = string.Empty;

        [ObservableProperty]
        private DateTime _lastUpdate = DateTime.MinValue;

        public string StatusTextRu
        {
            get
            {
                if (Connection == PostConnectionState.Offline) return "Нет связи";
                if (Connection == PostConnectionState.NoResponse) return "Нет ответа";
                if (Connection == PostConnectionState.Error) return "Ошибка связи";

                return Operation switch
                {
                    PostOperationState.Ready => "Готов",
                    PostOperationState.Calling => "Вызов",
                    PostOperationState.Authorized => "Авторизован",
                    PostOperationState.Started => "Старт",
                    PostOperationState.Paused => "Пауза",
                    PostOperationState.Fuelling => "Отпуск",
                    PostOperationState.Stopping => "Остановка",
                    PostOperationState.End => "Завершено",
                    PostOperationState.Error => "Ошибка",
                    _ => "Неизвестно"
                };
            }
        }

        partial void OnConnectionChanged(PostConnectionState value) => OnPropertyChanged(nameof(StatusTextRu));
        partial void OnOperationChanged(PostOperationState value) => OnPropertyChanged(nameof(StatusTextRu));
    }

    public sealed partial class TankRuntimeState : ObservableObject
    {
        [ObservableProperty]
        private int _number = 1;

        public string DisplayName => $"Резервуар {Number}";

        [ObservableProperty]
        private string _fuelName = "—";

        [ObservableProperty]
        private double _levelPercent = 0.0;

        [ObservableProperty]
        private double _minLevelPercent = 20.0;

        public string LevelText => $"{LevelPercent:0}%";

        partial void OnLevelPercentChanged(double value) => OnPropertyChanged(nameof(LevelText));
    }

    public sealed partial class AlertRuntimeItem : ObservableObject
    {
        [ObservableProperty]
        private DateTime _time = DateTime.Now;

        [ObservableProperty]
        private AlertSeverity _severity = AlertSeverity.Info;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _message = string.Empty;
    }

    public sealed partial class ShiftRuntimeState : ObservableObject
    {
        [ObservableProperty]
        private bool _isOpen = false;

        [ObservableProperty]
        private string _operatorLogin = string.Empty;

        [ObservableProperty]
        private DateTime? _openTime;

        [ObservableProperty]
        private DateTime? _closeTime;

        public string StatusTextRu => IsOpen ? "Смена: открыта" : "Смена: закрыта";

        partial void OnIsOpenChanged(bool value) => OnPropertyChanged(nameof(StatusTextRu));
    }
}

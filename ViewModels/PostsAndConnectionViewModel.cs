using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Посты и связь (UI-скелет конфигурации): COM-порт, протокол, адрес, тайминги.
    /// </summary>
    public partial class PostsAndConnectionViewModel : BaseViewModel
    {
        private readonly Action _onClose;

        public ObservableCollection<PostConnectionConfigItem> Posts { get; } = new();

        [ObservableProperty]
        private PostConnectionConfigItem? _selected;

        public PostsAndConnectionViewModel(Action onClose)
        {
            _onClose = onClose;

            for (int i = 1; i <= 8; i++)
            {
                Posts.Add(new PostConnectionConfigItem
                {
                    PostNumber = i,
                    ComPort = $"COM{i}",
                    Protocol = "GasKitLink v1.2",
                    DeviceId = 1,
                    Channel = 0,
                    PollingIntervalMs = 100,
                    ResponseTimeoutMs = 50
                });
            }

            if (Posts.Count > 0)
                Selected = Posts[0];
        }

        [RelayCommand]
        private void AddPost()
        {
            var next = Posts.Count + 1;
            Posts.Add(new PostConnectionConfigItem
            {
                PostNumber = next,
                ComPort = $"COM{next}",
                Protocol = "GasKitLink v1.2",
                DeviceId = 1,
                Channel = 0,
                PollingIntervalMs = 100,
                ResponseTimeoutMs = 50
            });
            Selected = Posts[^1];
        }

        [RelayCommand]
        private void RemoveSelected()
        {
            if (Selected == null) return;
            Posts.Remove(Selected);
            Selected = Posts.Count > 0 ? Posts[0] : null;
        }

        [RelayCommand]
        private void Save()
        {
            // UI-скелет: позже сохранить в SQLite/JSON.
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void Close()
        {
            _onClose?.Invoke();
        }
    }

    public sealed partial class PostConnectionConfigItem : ObservableObject
    {
        [ObservableProperty] private int _postNumber;
        [ObservableProperty] private string _comPort = "COM1";
        [ObservableProperty] private string _protocol = "GasKitLink v1.2";
        [ObservableProperty] private int _channel = 0;
        [ObservableProperty] private int _deviceId = 1;
        [ObservableProperty] private int _pollingIntervalMs = 100;
        [ObservableProperty] private int _responseTimeoutMs = 50;
        [ObservableProperty] private bool _isEmulator = true;
    }
}

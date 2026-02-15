using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Models.Runtime;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Панель: посты (1–8), резервуары, тревоги и детали выбранного поста.
    /// </summary>
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly RuntimeStateService _runtime;

        public ObservableCollection<PostRuntimeState> Posts => _runtime.Posts;
        public ObservableCollection<TankRuntimeState> Tanks => _runtime.Tanks;
        public ObservableCollection<AlertRuntimeItem> Alerts => _runtime.Alerts;

        [ObservableProperty]
        private PostRuntimeState? _selectedPost;

        public DashboardViewModel(DatabaseService _unusedDatabaseService, RuntimeStateService runtime)
        {
            _runtime = runtime;

            if (Posts.Count > 0)
                SelectedPost = Posts[0];
        }

        [RelayCommand]
        private void SelectPost(PostRuntimeState? post)
        {
            if (post != null)
                SelectedPost = post;
        }

        [RelayCommand]
        private void ClearAlerts()
        {
            // Очищаем только инфо-сообщения, критичные/предупреждения оставляем (скелет)
            for (int i = Alerts.Count - 1; i >= 0; i--)
            {
                if (Alerts[i].Severity == AlertSeverity.Info)
                    Alerts.RemoveAt(i);
            }
        }
    }
}

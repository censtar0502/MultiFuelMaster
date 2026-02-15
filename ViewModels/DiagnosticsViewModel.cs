using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MultiFuelMaster.Models.Runtime;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Диагностика связи/событий (UI-скелет).
    /// </summary>
    public partial class DiagnosticsViewModel : ObservableObject
    {
        private readonly RuntimeStateService _runtime;

        public ObservableCollection<PostRuntimeState> Posts => _runtime.Posts;
        public ObservableCollection<AlertRuntimeItem> Alerts => _runtime.Alerts;

        [ObservableProperty]
        private PostRuntimeState? _selectedPost;

        public DiagnosticsViewModel(RuntimeStateService runtime)
        {
            _runtime = runtime;
            if (Posts.Count > 0)
                SelectedPost = Posts[0];
        }
    }
}

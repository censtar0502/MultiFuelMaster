using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Base view model with property change notification
    /// </summary>
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isLoading;
        private string _errorMessage = string.Empty;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        protected void ClearError() => ErrorMessage = string.Empty;
    }
}
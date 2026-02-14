using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Models;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Transactions view model
    /// </summary>
    public partial class TransactionsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private List<Transaction> _transactions = new();

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today.AddDays(-7);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        public TransactionsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _ = LoadTransactionsAsync();
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                Transactions = await _databaseService.GetTransactionsByDateRangeAsync(StartDate, EndDate);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки транзакций: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshTransactions()
        {
            await LoadTransactionsAsync();
        }

        [RelayCommand]
        private async Task ApplyDateFilter()
        {
            if (StartDate > EndDate)
            {
                ErrorMessage = "Дата начала должна быть меньше или равна дате окончания";
                return;
            }
            await LoadTransactionsAsync();
        }
    }
}
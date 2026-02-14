using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiFuelMaster.Services;

namespace MultiFuelMaster.ViewModels
{
    /// <summary>
    /// Reports view model
    /// </summary>
    public partial class ReportsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private TransactionStatistics _statistics = new();

        [ObservableProperty]
        private DateTime _reportStartDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _reportEndDate = DateTime.Today;

        public ReportsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _ = GenerateReportAsync();
        }

        private async Task GenerateReportAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                Statistics = await _databaseService.GetTransactionStatisticsAsync(ReportStartDate, ReportEndDate);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка генерации отчета: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GenerateReport()
        {
            if (ReportStartDate > ReportEndDate)
            {
                ErrorMessage = "Дата начала должна быть меньше или равна дате окончания";
                return;
            }
            await GenerateReportAsync();
        }
    }
}
namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Уровни доступа к функциям системы
    /// </summary>
    public enum PermissionLevel
    {
        /// <summary>Нет доступа</summary>
        NoAccess = 0,

        /// <summary>Есть доступ (для действий-кнопок)</summary>
        HasAccess = 1,

        /// <summary>Просмотр</summary>
        View = 2,

        /// <summary>Редактирование</summary>
        Edit = 3,

        /// <summary>Редактирование с паролем</summary>
        EditWithPassword = 4,

        /// <summary>Смена владельца</summary>
        ChangeOwner = 5,

        /// <summary>Не задано (по умолчанию)</summary>
        NotSet = 6
    }

    /// <summary>
    /// Категории прав в системе
    /// </summary>
    public static class PermissionCategories
    {
        public const string General = "Общие";
        public const string Shift = "Смена";
        public const string Dispensers = "ТРК";
        public const string Transactions = "Транзакции";
        public const string Tanks = "Резервуары";
        public const string FuelAndPrices = "Топливо и цены";
        public const string Reports = "Отчёты";
        public const string Diagnostics = "Диагностика";
        public const string Configuration = "Конфигуратор";
    }

    /// <summary>
    /// Ключи прав (Permission Keys)
    /// </summary>
    public static class PermissionKeys
    {
        // Общие - Экраны/навигация
        public const string OpenPanel = "OpenPanel";
        public const string OpenTransactions = "OpenTransactions";
        public const string OpenShift = "OpenShift";
        public const string OpenReports = "OpenReports";
        public const string OpenDiagnostics = "OpenDiagnostics";

        // Общие - Системные действия
        public const string ViewEventLog = "ViewEventLog";
        public const string ViewSystemLog = "ViewSystemLog";
        public const string ExportData = "ExportData";
        public const string ArchiveDatabase = "ArchiveDatabase";
        public const string RestoreDatabase = "RestoreDatabase";
        public const string ManageLicense = "ManageLicense";
        public const string UpdateProgram = "UpdateProgram";

        // Смена
        public const string OpenShiftAction = "OpenShiftAction";
        public const string CloseShiftAction = "CloseShiftAction";
        public const string CloseOtherUserShift = "CloseOtherUserShift";
        public const string ViewCurrentShift = "ViewCurrentShift";
        public const string ViewArchivedShifts = "ViewArchivedShifts";
        public const string OpenCashTotals = "OpenCashTotals";
        public const string ExportCashTotals = "ExportCashTotals";

        // ТРК
        public const string StartTRK = "StartTRK";
        public const string StopTRK = "StopTRK";
        public const string PauseDispense = "PauseDispense";
        public const string ContinueDispense = "ContinueDispense";
        public const string StopAllDispense = "StopAllDispense";
        public const string ResetDispenserError = "ResetDispenserError";
        public const string TestDispenserConnection = "TestDispenserConnection";
        public const string ReconnectChannel = "ReconnectChannel";
        public const string ViewDispenserCard = "ViewDispenserCard";
        public const string ViewDispenserDiagnostics = "ViewDispenserDiagnostics";

        // Транзакции
        public const string ViewTransactionList = "ViewTransactionList";
        public const string ViewTransactionDetails = "ViewTransactionDetails";
        public const string CorrectTransaction = "CorrectTransaction";
        public const string CompleteTransactionWithoutReceipt = "CompleteTransactionWithoutReceipt";
        public const string PrintReceipt = "PrintReceipt";
        public const string PrintCorrectionReceipt = "PrintCorrectionReceipt";
        public const string ExportTransactions = "ExportTransactions";
        public const string ChangeTransactionOwner = "ChangeTransactionOwner";

        // Резервуары
        public const string OpenTanksScreen = "OpenTanksScreen";
        public const string ViewTankLevels = "ViewTankLevels";
        public const string ManualTankLevelCorrection = "ManualTankLevelCorrection";
        public const string FuelArrival = "FuelArrival";
        public const string FuelDeparture = "FuelDeparture";
        public const string SwitchTanks = "SwitchTanks";

        // Топливо и цены
        public const string OpenFuelTypesScreen = "OpenFuelTypesScreen";
        public const string ManageFuelTypes = "ManageFuelTypes";
        public const string OpenPricesScreen = "OpenPricesScreen";
        public const string ChangePrices = "ChangePrices";
        public const string ChangePricesWithPassword = "ChangePricesWithPassword";

        // Отчёты
        public const string OpenReportsScreen = "OpenReportsScreen";
        public const string ReportShiftX = "ReportShiftX";
        public const string ReportShiftZ = "ReportShiftZ";
        public const string ReportDispensers = "ReportDispensers";
        public const string ReportTanks = "ReportTanks";
        public const string ReportFuelTypes = "ReportFuelTypes";
        public const string ReportOperators = "ReportOperators";
        public const string PrintReports = "PrintReports";
        public const string ExportReports = "ExportReports";

        // Диагностика
        public const string OpenDiagnosticsScreen = "OpenDiagnosticsScreen";
        public const string ViewAllDispenserStatus = "ViewAllDispenserStatus";
        public const string ViewErrorsAlerts = "ViewErrorsAlerts";
        public const string ResetAlerts = "ResetAlerts";

        // Конфигуратор
        public const string OpenStationSettings = "OpenStationSettings";
        public const string OpenPostsAndConnection = "OpenPostsAndConnection";
        public const string OpenChannels = "OpenChannels";
        public const string OpenSalesParameters = "OpenSalesParameters";
        public const string OpenUsers = "OpenUsers";
        public const string OpenUserRoles = "OpenUserRoles";
    }

    /// <summary>
    /// Описание ключа права для отображения в UI
    /// </summary>
    public class PermissionKeyInfo
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}

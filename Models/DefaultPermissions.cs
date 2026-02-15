using System.Collections.Generic;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Default role permission templates
    /// </summary>
    public static class DefaultPermissions
    {
        public static Dictionary<string, PermissionLevel> GetSuperAdminPermissions()
        {
            return new Dictionary<string, PermissionLevel>
            {
                // General - screens
                { PermissionKeys.OpenPanel, PermissionLevel.Edit },
                { PermissionKeys.OpenTransactions, PermissionLevel.Edit },
                { PermissionKeys.OpenShift, PermissionLevel.Edit },
                { PermissionKeys.OpenReports, PermissionLevel.Edit },
                { PermissionKeys.OpenDiagnostics, PermissionLevel.Edit },
                // General - system
                { PermissionKeys.ViewEventLog, PermissionLevel.Edit },
                { PermissionKeys.ViewSystemLog, PermissionLevel.Edit },
                { PermissionKeys.ExportData, PermissionLevel.Edit },
                { PermissionKeys.ArchiveDatabase, PermissionLevel.Edit },
                { PermissionKeys.RestoreDatabase, PermissionLevel.Edit },
                { PermissionKeys.ManageLicense, PermissionLevel.Edit },
                { PermissionKeys.UpdateProgram, PermissionLevel.Edit },
                // Shift
                { PermissionKeys.OpenShiftAction, PermissionLevel.Edit },
                { PermissionKeys.CloseShiftAction, PermissionLevel.Edit },
                { PermissionKeys.CloseOtherUserShift, PermissionLevel.ChangeOwner },
                { PermissionKeys.ViewCurrentShift, PermissionLevel.Edit },
                { PermissionKeys.ViewArchivedShifts, PermissionLevel.Edit },
                { PermissionKeys.OpenCashTotals, PermissionLevel.Edit },
                { PermissionKeys.ExportCashTotals, PermissionLevel.Edit },
                // TRK
                { PermissionKeys.StartTRK, PermissionLevel.Edit },
                { PermissionKeys.StopTRK, PermissionLevel.Edit },
                { PermissionKeys.PauseDispense, PermissionLevel.Edit },
                { PermissionKeys.ContinueDispense, PermissionLevel.Edit },
                { PermissionKeys.StopAllDispense, PermissionLevel.Edit },
                { PermissionKeys.ResetDispenserError, PermissionLevel.Edit },
                { PermissionKeys.TestDispenserConnection, PermissionLevel.Edit },
                { PermissionKeys.ReconnectChannel, PermissionLevel.Edit },
                { PermissionKeys.ViewDispenserCard, PermissionLevel.Edit },
                { PermissionKeys.ViewDispenserDiagnostics, PermissionLevel.Edit },
                // Transactions
                { PermissionKeys.ViewTransactionList, PermissionLevel.Edit },
                { PermissionKeys.ViewTransactionDetails, PermissionLevel.Edit },
                { PermissionKeys.CorrectTransaction, PermissionLevel.Edit },
                { PermissionKeys.CompleteTransactionWithoutReceipt, PermissionLevel.Edit },
                { PermissionKeys.PrintReceipt, PermissionLevel.Edit },
                { PermissionKeys.PrintCorrectionReceipt, PermissionLevel.Edit },
                { PermissionKeys.ExportTransactions, PermissionLevel.Edit },
                { PermissionKeys.ChangeTransactionOwner, PermissionLevel.ChangeOwner },
                // Tanks
                { PermissionKeys.OpenTanksScreen, PermissionLevel.Edit },
                { PermissionKeys.ViewTankLevels, PermissionLevel.Edit },
                { PermissionKeys.ManualTankLevelCorrection, PermissionLevel.EditWithPassword },
                { PermissionKeys.FuelArrival, PermissionLevel.Edit },
                { PermissionKeys.FuelDeparture, PermissionLevel.Edit },
                { PermissionKeys.SwitchTanks, PermissionLevel.Edit },
                // Fuel and Prices
                { PermissionKeys.OpenFuelTypesScreen, PermissionLevel.Edit },
                { PermissionKeys.ManageFuelTypes, PermissionLevel.Edit },
                { PermissionKeys.OpenPricesScreen, PermissionLevel.Edit },
                { PermissionKeys.ChangePrices, PermissionLevel.Edit },
                { PermissionKeys.ChangePricesWithPassword, PermissionLevel.EditWithPassword },
                // Reports
                { PermissionKeys.OpenReportsScreen, PermissionLevel.Edit },
                { PermissionKeys.ReportShiftX, PermissionLevel.Edit },
                { PermissionKeys.ReportShiftZ, PermissionLevel.Edit },
                { PermissionKeys.ReportDispensers, PermissionLevel.Edit },
                { PermissionKeys.ReportTanks, PermissionLevel.Edit },
                { PermissionKeys.ReportFuelTypes, PermissionLevel.Edit },
                { PermissionKeys.ReportOperators, PermissionLevel.Edit },
                { PermissionKeys.PrintReports, PermissionLevel.Edit },
                { PermissionKeys.ExportReports, PermissionLevel.Edit },
                // Diagnostics
                { PermissionKeys.OpenDiagnosticsScreen, PermissionLevel.Edit },
                { PermissionKeys.ViewAllDispenserStatus, PermissionLevel.Edit },
                { PermissionKeys.ViewErrorsAlerts, PermissionLevel.Edit },
                { PermissionKeys.ResetAlerts, PermissionLevel.EditWithPassword },
                // Configuration
                { PermissionKeys.OpenStationSettings, PermissionLevel.Edit },
                { PermissionKeys.OpenPostsAndConnection, PermissionLevel.Edit },
                { PermissionKeys.OpenChannels, PermissionLevel.Edit },
                { PermissionKeys.OpenSalesParameters, PermissionLevel.Edit },
                { PermissionKeys.OpenUsers, PermissionLevel.Edit },
                { PermissionKeys.OpenUserRoles, PermissionLevel.Edit }
            };
        }

        public static Dictionary<string, PermissionLevel> GetAdministratorPermissions()
        {
            var perms = GetSuperAdminPermissions();
            // More strict for sensitive operations
            perms[PermissionKeys.ManageLicense] = PermissionLevel.EditWithPassword;
            perms[PermissionKeys.RestoreDatabase] = PermissionLevel.EditWithPassword;
            perms[PermissionKeys.UpdateProgram] = PermissionLevel.EditWithPassword;
            return perms;
        }

        public static Dictionary<string, PermissionLevel> GetConfiguratorPermissions()
        {
            var perms = new Dictionary<string, PermissionLevel>();
            // Full access to Configuration
            perms[PermissionKeys.OpenStationSettings] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenPostsAndConnection] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenChannels] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenSalesParameters] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenUsers] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenUserRoles] = PermissionLevel.Edit;
            // No access to TRK operations
            perms[PermissionKeys.StartTRK] = PermissionLevel.NoAccess;
            perms[PermissionKeys.StopTRK] = PermissionLevel.NoAccess;
            // View access to other sections
            perms[PermissionKeys.OpenPanel] = PermissionLevel.View;
            perms[PermissionKeys.OpenTransactions] = PermissionLevel.View;
            perms[PermissionKeys.OpenShift] = PermissionLevel.View;
            perms[PermissionKeys.OpenReports] = PermissionLevel.View;
            perms[PermissionKeys.OpenDiagnostics] = PermissionLevel.View;
            perms[PermissionKeys.OpenTanksScreen] = PermissionLevel.View;
            perms[PermissionKeys.OpenFuelTypesScreen] = PermissionLevel.View;
            perms[PermissionKeys.OpenPricesScreen] = PermissionLevel.View;
            return perms;
        }

        public static Dictionary<string, PermissionLevel> GetSeniorOperatorPermissions()
        {
            var perms = new Dictionary<string, PermissionLevel>();
            // Operations + Reports
            perms[PermissionKeys.OpenPanel] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenTransactions] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenShift] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenReports] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenDiagnostics] = PermissionLevel.View;
            // Shift actions
            perms[PermissionKeys.OpenShiftAction] = PermissionLevel.Edit;
            perms[PermissionKeys.CloseShiftAction] = PermissionLevel.Edit;
            perms[PermissionKeys.CloseOtherUserShift] = PermissionLevel.ChangeOwner;
            perms[PermissionKeys.ViewCurrentShift] = PermissionLevel.Edit;
            perms[PermissionKeys.ViewArchivedShifts] = PermissionLevel.Edit;
            // TRK
            perms[PermissionKeys.StartTRK] = PermissionLevel.HasAccess;
            perms[PermissionKeys.StopTRK] = PermissionLevel.HasAccess;
            perms[PermissionKeys.PauseDispense] = PermissionLevel.HasAccess;
            perms[PermissionKeys.ContinueDispense] = PermissionLevel.HasAccess;
            perms[PermissionKeys.StopAllDispense] = PermissionLevel.Edit;
            perms[PermissionKeys.ViewDispenserCard] = PermissionLevel.View;
            perms[PermissionKeys.ViewDispenserDiagnostics] = PermissionLevel.View;
            // Transactions
            perms[PermissionKeys.ViewTransactionList] = PermissionLevel.Edit;
            perms[PermissionKeys.ViewTransactionDetails] = PermissionLevel.Edit;
            perms[PermissionKeys.PrintReceipt] = PermissionLevel.Edit;
            // Tanks
            perms[PermissionKeys.OpenTanksScreen] = PermissionLevel.View;
            perms[PermissionKeys.ViewTankLevels] = PermissionLevel.View;
            // Fuel and Prices
            perms[PermissionKeys.OpenFuelTypesScreen] = PermissionLevel.View;
            perms[PermissionKeys.OpenPricesScreen] = PermissionLevel.View;
            perms[PermissionKeys.ChangePrices] = PermissionLevel.EditWithPassword;
            // Reports
            perms[PermissionKeys.ReportShiftX] = PermissionLevel.Edit;
            perms[PermissionKeys.ReportShiftZ] = PermissionLevel.Edit;
            perms[PermissionKeys.ReportDispensers] = PermissionLevel.Edit;
            perms[PermissionKeys.ReportTanks] = PermissionLevel.Edit;
            perms[PermissionKeys.ReportFuelTypes] = PermissionLevel.Edit;
            perms[PermissionKeys.ReportOperators] = PermissionLevel.Edit;
            // Diagnostics
            perms[PermissionKeys.OpenDiagnosticsScreen] = PermissionLevel.View;
            perms[PermissionKeys.ViewAllDispenserStatus] = PermissionLevel.View;
            perms[PermissionKeys.ViewErrorsAlerts] = PermissionLevel.View;
            // Configuration - view only
            perms[PermissionKeys.OpenStationSettings] = PermissionLevel.View;
            perms[PermissionKeys.OpenPostsAndConnection] = PermissionLevel.View;
            perms[PermissionKeys.OpenChannels] = PermissionLevel.View;
            perms[PermissionKeys.OpenSalesParameters] = PermissionLevel.View;
            perms[PermissionKeys.OpenUsers] = PermissionLevel.View;
            perms[PermissionKeys.OpenUserRoles] = PermissionLevel.View;
            return perms;
        }

        public static Dictionary<string, PermissionLevel> GetOperatorPermissions()
        {
            var perms = new Dictionary<string, PermissionLevel>();
            // Basic operations
            perms[PermissionKeys.OpenPanel] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenTransactions] = PermissionLevel.View;
            perms[PermissionKeys.OpenShift] = PermissionLevel.Edit;
            perms[PermissionKeys.OpenDiagnostics] = PermissionLevel.View;
            // Shift
            perms[PermissionKeys.OpenShiftAction] = PermissionLevel.Edit;
            perms[PermissionKeys.CloseShiftAction] = PermissionLevel.Edit;
            perms[PermissionKeys.ViewCurrentShift] = PermissionLevel.View;
            // TRK operations
            perms[PermissionKeys.StartTRK] = PermissionLevel.HasAccess;
            perms[PermissionKeys.StopTRK] = PermissionLevel.HasAccess;
            perms[PermissionKeys.PauseDispense] = PermissionLevel.HasAccess;
            perms[PermissionKeys.ContinueDispense] = PermissionLevel.HasAccess;
            perms[PermissionKeys.ViewDispenserCard] = PermissionLevel.View;
            // Transactions
            perms[PermissionKeys.ViewTransactionList] = PermissionLevel.View;
            perms[PermissionKeys.ViewTransactionDetails] = PermissionLevel.View;
            perms[PermissionKeys.PrintReceipt] = PermissionLevel.Edit;
            // Tanks - view only
            perms[PermissionKeys.OpenTanksScreen] = PermissionLevel.View;
            perms[PermissionKeys.ViewTankLevels] = PermissionLevel.View;
            // Reports
            perms[PermissionKeys.OpenReportsScreen] = PermissionLevel.View;
            perms[PermissionKeys.ReportShiftX] = PermissionLevel.View;
            // Diagnostics
            perms[PermissionKeys.OpenDiagnosticsScreen] = PermissionLevel.View;
            perms[PermissionKeys.ViewAllDispenserStatus] = PermissionLevel.View;
            perms[PermissionKeys.ViewErrorsAlerts] = PermissionLevel.View;
            // No configuration access
            return perms;
        }

        public static Dictionary<string, PermissionLevel> GetEmptyPermissions()
        {
            return new Dictionary<string, PermissionLevel>();
        }
    }
}

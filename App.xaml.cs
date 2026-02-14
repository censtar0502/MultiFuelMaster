using System;
using System.Windows;
using MultiFuelMaster.Services;
using MultiFuelMaster.Data;
using MultiFuelMaster.Views;
using MultiFuelMaster.Models;
using MultiFuelMaster.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MultiFuelMaster
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private User? _currentUser;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();

                var authService = _serviceProvider.GetRequiredService<AuthService>();
                bool hasAdmin = authService.HasAdminUserAsync().Result;

                if (!hasAdmin)
                {
                    var setupWindow = new SetupWindow(authService);
                    if (setupWindow.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                var loginWindow = new LoginWindow(authService);
                var loginResult = loginWindow.ShowDialog();
                
                if (loginResult != true)
                {
                    Shutdown();
                    return;
                }
                
                _currentUser = loginWindow.LoggedInUser;

                ShowMainWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска приложения: {ex.Message}\n\nStack: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ShowMainWindow()
        {
            try
            {
                var databaseService = _serviceProvider!.GetRequiredService<DatabaseService>();
                var stationSettingsService = _serviceProvider.GetRequiredService<StationSettingsService>();
                var fuelTypeService = _serviceProvider.GetRequiredService<FuelTypeService>();
                var tankService = _serviceProvider.GetRequiredService<TankService>();
                
                var mainViewModel = new MainViewModel(databaseService, stationSettingsService, fuelTypeService, tankService, _currentUser);
                var mainWindow = new MainWindow { DataContext = mainViewModel };
                
                mainWindow.Closed += (s, e) => {
                    this.Shutdown();
                };
                
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии главного окна: {ex.Message}\n\nStack: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>();
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<EncryptionService>();
            services.AddSingleton<StationSettingsService>();
            services.AddSingleton<FuelTypeService>();
            services.AddSingleton<TankService>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}

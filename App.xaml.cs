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
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private User? _currentUser;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // Configure services
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // Initialize database
                var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();

                // Get auth service
                var authService = _serviceProvider.GetRequiredService<AuthService>();

                // Check if admin exists
                bool hasAdmin = authService.HasAdminUserAsync().Result;

                if (!hasAdmin)
                {
                    // Show setup window first
                    var setupWindow = new SetupWindow(authService);
                    if (setupWindow.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                // Show login window
                Console.WriteLine("Showing login window...");
                var loginWindow = new LoginWindow(authService);
                var loginResult = loginWindow.ShowDialog();
                Console.WriteLine($"Login result: {loginResult}");
                
                if (loginResult != true)
                {
                    Shutdown();
                    return;
                }
                
                // Get logged in user
                _currentUser = loginWindow.LoggedInUser;
                Console.WriteLine($"User logged in: {_currentUser?.Login}, Role: {_currentUser?.Role}");

                // Show main window
                try
                {
                    Console.WriteLine("Creating main window...");
                    
                    // Get database service
                    var databaseService = _serviceProvider.GetRequiredService<DatabaseService>();
                    var stationSettingsService = _serviceProvider.GetRequiredService<StationSettingsService>();
                    var fuelTypeService = _serviceProvider.GetRequiredService<FuelTypeService>();
                    var tankService = _serviceProvider.GetRequiredService<TankService>();
                    
                    // Create main view model with user
                    var mainViewModel = new MainViewModel(databaseService, stationSettingsService, fuelTypeService, tankService, _currentUser);
                    
                    var mainWindow = new MainWindow { DataContext = mainViewModel };
                    
                    // Handle main window closed
                    mainWindow.Closed += (s, e) => {
                        Console.WriteLine("Main window closed, shutting down...");
                        this.Shutdown();
                    };
                    
                    Console.WriteLine("Showing main window...");
                    mainWindow.Show();
                    mainWindow.Activate();
                    Console.WriteLine("Main window shown...");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии главного окна: {ex.Message}\n\nStack: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска приложения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
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

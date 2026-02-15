using System;
using System.Threading.Tasks;
using System.Windows;
using MultiFuelMaster.Services;
using MultiFuelMaster.Data;
using MultiFuelMaster.Views;
using MultiFuelMaster.Models;
using MultiFuelMaster.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MultiFuelMaster
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private User? _currentUser;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // Инициализация логирования
                var logDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                LoggingService.Initialize(logDirectory);

                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using (var dbContext = await dbFactory.CreateDbContextAsync())
                {
                    await dbContext.Database.EnsureCreatedAsync();
                }

                var authService = _serviceProvider.GetRequiredService<AuthService>();
                bool hasAdmin = await authService.HasAdminUserAsync();

                if (!hasAdmin)
                {
                    // Автосоздание супер-админа при первом запуске (без дополнительных окон)
                    await authService.CreateSuperAdminAsync("admin", "admin", "Administrator");
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
                var userService = _serviceProvider.GetRequiredService<UserService>();
                var runtimeState = _serviceProvider.GetRequiredService<RuntimeStateService>();
                
                // Сохранить время входа в базу (без блокировки UI)
                if (_currentUser != null)
                {
                    _currentUser.LoginTime = DateTime.Now;

                    var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                    using var dbContext = dbFactory.CreateDbContext();
                    var userToUpdate = dbContext.Users.Find(_currentUser.Id);
                    if (userToUpdate != null)
                    {
                        userToUpdate.LoginTime = _currentUser.LoginTime;
                        dbContext.SaveChanges();
                    }
                }
                
                var mainViewModel = new MainViewModel(
                    databaseService, 
                    stationSettingsService, 
                    fuelTypeService, 
                    tankService,
                    userService,
                    runtimeState,
                    _currentUser,
                    () => {
                        // Сохранить время выхода перед закрытием
                        SaveLogoutTime();
                        this.Shutdown();
                    });
                var mainWindow = new MainWindow { DataContext = mainViewModel };
                
                mainWindow.Closed += (s, e) => {
                    SaveLogoutTime();
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
        
        private void SaveLogoutTime()
        {
            try
            {
                if (_currentUser != null && _serviceProvider != null)
                {
                    var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                    using var dbContext = dbFactory.CreateDbContext();
                    var userToUpdate = dbContext.Users.Find(_currentUser.Id);
                    if (userToUpdate != null)
                    {
                        userToUpdate.LogoutTime = DateTime.Now;
                        dbContext.SaveChanges();
                    }
                }
            }
            catch
            {
                // Игнорировать ошибки при сохранении времени выхода
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // DbContextFactory для создания контекста на каждую операцию
            services.AddDbContextFactory<AppDbContext>();
            
            // Сервисы - Transient (создаются на каждый запрос)
            services.AddTransient<DatabaseService>();
            services.AddTransient<AuthService>();
            services.AddSingleton<EncryptionService>();
            services.AddTransient<StationSettingsService>();
            services.AddTransient<FuelTypeService>();
            services.AddTransient<TankService>();
            services.AddTransient<UserService>();
            services.AddSingleton<RuntimeStateService>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}

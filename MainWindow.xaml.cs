using System.Windows;
using System.Windows.Threading;
using MultiFuelMaster.ViewModels;
using MultiFuelMaster.Services;
using MultiFuelMaster.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MultiFuelMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _clockTimer;

        public MainWindow()
        {
            Console.WriteLine("Starting application...");
            
            try
            {
                InitializeComponent();
                Console.WriteLine("UI initialized...");
                
                // Setup clock timer
                SetupClock();
                Console.WriteLine("Clock setup complete...");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}\n\nStack: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (sender, e) => 
            {
                ClockTextBlock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            };
            _clockTimer.Start();
            
            // Set initial time
            ClockTextBlock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // Register services
            services.AddDbContext<AppDbContext>();
            services.AddSingleton<DatabaseService>();
            
            // Register view models
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<StationsViewModel>();
            services.AddTransient<TransactionsViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<SettingsViewModel>();
            
            return services.BuildServiceProvider();
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
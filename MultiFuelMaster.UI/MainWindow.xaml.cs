using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MultiFuelMaster.UI
{
    public partial class MainWindow : Window
    {
        private readonly List<PostControl> _posts = new();
        private readonly DispatcherTimer   _clockTimer;
        private readonly int               _maxPanels;

        public MainWindow(int maxPanels)
        {
            InitializeComponent();
            _maxPanels = maxPanels;

            PostCountLabel.Text = $"  [{maxPanels} {PanelWord(maxPanels)}]";

            // Создаём PostControl для каждого поста
            for (int i = 1; i <= maxPanels; i++)
            {
                var post = new PostControl(i);
                _posts.Add(post);
                PostsPanel.Children.Add(post);
            }

            // Тикающие часы в статусной строке
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            Closing += MainWindow_Closing;

            // Подбираем размер окна под количество постов
            AdjustWindowSize(maxPanels);

            Title = $"MultiFuelMaster — {maxPanels} {PanelWord(maxPanels)}";
        }

        private void AdjustWindowSize(int count)
        {
            const int panelW = 392;   // ширина PostControl (380 + отступы)
            const int maxPerRow = 5;

            int cols = Math.Min(count, maxPerRow);
            int width  = cols * panelW + 24;
            int height = 720;

            Width  = Math.Max(width, 420);
            Height = height;
        }

        private static string PanelWord(int n) => n == 1 ? "пост" :
                                                   n <= 4 ? "поста" : "постов";

        // ===== ЗАГОЛОВОК — ПЕРЕТАСКИВАНИЕ =====

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }

        // ===== КНОПКИ ОКНА =====

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => ToggleMaximize();

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        // ===== МЕНЮ =====

        private void BtnLicense_Click(object sender, RoutedEventArgs e)
        {
            var lm = new LicenseManager();
            var info = lm.CheckLicense();
            var win = new LicenseWindow(info.DaysRemaining);
            win.Owner = this;
            win.ShowDialog();
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var win = new AboutWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        // ===== ЗАКРЫТИЕ =====

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _clockTimer.Stop();
            foreach (var post in _posts)
                post.Shutdown();
        }
    }
}

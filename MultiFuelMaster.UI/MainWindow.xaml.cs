using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
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

            PostCountLabel.Text = $"[{maxPanels} {PanelWord(maxPanels)}]";

            // Рассчитываем сетку: кол-во колонок
            int cols = CalculateColumns(maxPanels);
            PostsGrid.Columns = cols;

            // Создаём панели постов
            for (int i = 1; i <= maxPanels; i++)
            {
                var post = new PostControl(i);
                _posts.Add(post);
                PostsGrid.Children.Add(post);
            }

            // Часы в статусной строке
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            Closing += MainWindow_Closing;

            // Размер окна под количество постов
            AdjustWindowSize(maxPanels, cols);

            Title = $"MultiFuelMaster — {maxPanels} {PanelWord(maxPanels)}";
            ActiveCountLabel.Text = $"0 / {maxPanels} активно";
        }

        private static int CalculateColumns(int panelCount) => panelCount switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 2,
            5 or 6 => 3,
            7 or 8 => 4,
            _ => 5
        };

        private void AdjustWindowSize(int count, int cols)
        {
            int rows = (int)Math.Ceiling((double)count / cols);

            // Компактные панели: ~300px ширина, ~340px высота
            int width  = cols * 300 + 24;
            int height = rows * 340 + 80;

            Width  = Math.Max(width, 580);
            Height = Math.Min(Math.Max(height, 480), 900);

            // При 1 посте — окно уже, но не слишком
            if (count == 1) { Width = 400; Height = 500; }
        }

        private static string PanelWord(int n) => n == 1 ? "пост" :
                                                   n <= 4 ? "поста" : "постов";

        // ===== КНОПКИ ОКНА =====

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();

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

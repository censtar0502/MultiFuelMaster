using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace MultiFuelMaster.UI
{
    public partial class GlobalSettingsWindow : Window
    {
        private int _panelCount;
        public int PanelCount => _panelCount;

        public GlobalSettingsWindow(int currentPanelCount)
        {
            InitializeComponent();
            _panelCount = Math.Clamp(currentPanelCount, 1, 20);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            PanelCountDisplay.Text = _panelCount.ToString();
        }

        private void BtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (_panelCount < 20) { _panelCount++; UpdateDisplay(); }
        }

        private void BtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (_panelCount > 1) { _panelCount--; UpdateDisplay(); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MultiFuelMaster", "settings_global.json");

                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(
                    new { PanelCount = _panelCount },
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultiFuelMaster.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly int _postNumber;
        private bool _settingsLoaded = false;

        // Результирующие значения (после сохранения)
        public string SelectedPort    { get; private set; } = "COM3";
        public double PricePerLiter   { get; private set; } = 2233;
        public string FuelType        { get; private set; } = "АИ-95";

        public int  ResponseTimeoutMs   { get; private set; } = 80;
        public int  InterByteTimeoutMs  { get; private set; } = 20;
        public int  MaxRetries          { get; private set; } = 3;
        public int  InterCommandDelayMs { get; private set; } = 10;
        public int  IdlePollDelayMs     { get; private set; } = 450;
        public int  LinkLostPollMs      { get; private set; } = 350;
        public int  PostEndDelayMs      { get; private set; } = 800;
        public int  ErrorThreshold      { get; private set; } = 6;
        public bool ForceBufferClear    { get; private set; } = false;

        private class PostSettings
        {
            public string Port              { get; set; } = "COM3";
            public double PricePerLiter     { get; set; } = 2233;
            public string FuelType          { get; set; } = "АИ-95";
            public int    ResponseTimeoutMs  { get; set; } = 80;
            public int    InterByteTimeoutMs { get; set; } = 20;
            public int    MaxRetries         { get; set; } = 3;
            public int    InterCommandDelayMs { get; set; } = 10;
            public int    IdlePollDelayMs    { get; set; } = 450;
            public int    LinkLostPollMs     { get; set; } = 350;
            public int    PostEndDelayMs     { get; set; } = 800;
            public int    ErrorThreshold     { get; set; } = 6;
            public bool   ForceBufferClear   { get; set; } = false;
        }

        private string GetSettingsPath() =>
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiFuelMaster", $"settings_post{_postNumber}.json");

        public SettingsWindow(int postNumber)
        {
            InitializeComponent();
            _postNumber    = postNumber;
            TitleText.Text = $"Настройки Поста №{postNumber}";

            // COM-порты
            string[] ports = SerialPort.GetPortNames();
            ComPortCombo.Items.Clear();
            foreach (string p in ports)
                ComPortCombo.Items.Add(p);

            // Загружаем сохранённые настройки
            LoadSettings();
            _settingsLoaded = true;
        }

        private void LoadSettings()
        {
            try
            {
                string path = GetSettingsPath();
                PostSettings? s = null;

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    s = JsonSerializer.Deserialize<PostSettings>(json);
                }
                s ??= new PostSettings();

                // Применяем к UI
                PriceInput.Text = s.PricePerLiter.ToString("F0", CultureInfo.InvariantCulture);

                SelectComboByContent(ComPortCombo, s.Port);

                foreach (ComboBoxItem item in FuelTypeCombo.Items)
                    if (item.Tag?.ToString() == s.FuelType)
                    { FuelTypeCombo.SelectedItem = item; break; }

                // Обновляем публичные свойства
                SelectedPort    = s.Port;
                PricePerLiter   = s.PricePerLiter;
                FuelType        = s.FuelType;
                ResponseTimeoutMs   = s.ResponseTimeoutMs;
                InterByteTimeoutMs  = s.InterByteTimeoutMs;
                MaxRetries          = s.MaxRetries;
                InterCommandDelayMs = s.InterCommandDelayMs;
                IdlePollDelayMs     = s.IdlePollDelayMs;
                LinkLostPollMs      = s.LinkLostPollMs;
                PostEndDelayMs      = s.PostEndDelayMs;
                ErrorThreshold      = s.ErrorThreshold;
                ForceBufferClear    = s.ForceBufferClear;
            }
            catch { }
        }

        private static void SelectComboByContent(ComboBox combo, string value)
        {
            foreach (var item in combo.Items)
            {
                if (item?.ToString() == value)
                { combo.SelectedItem = item; return; }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void ComPortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_settingsLoaded) return;
            SelectedPort = ComPortCombo.SelectedItem?.ToString() ?? "";
        }

        private void FuelTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_settingsLoaded) return;
            if (FuelTypeCombo.SelectedItem is ComboBoxItem item)
                FuelType = item.Tag?.ToString() ?? "АИ-95";
        }

        private void PriceInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            char c = e.Text.Length > 0 ? e.Text[0] : '\0';
            e.Handled = !char.IsDigit(c);
        }

        private void PriceInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_settingsLoaded) return;
            if (double.TryParse(PriceInput.Text, out double price) && price > 0)
                PricePerLiter = price;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var s = new PostSettings
                {
                    Port              = SelectedPort,
                    PricePerLiter     = PricePerLiter,
                    FuelType          = FuelType,
                    ResponseTimeoutMs  = ResponseTimeoutMs,
                    InterByteTimeoutMs = InterByteTimeoutMs,
                    MaxRetries         = MaxRetries,
                    InterCommandDelayMs = InterCommandDelayMs,
                    IdlePollDelayMs    = IdlePollDelayMs,
                    LinkLostPollMs     = LinkLostPollMs,
                    PostEndDelayMs     = PostEndDelayMs,
                    ErrorThreshold     = ErrorThreshold,
                    ForceBufferClear   = ForceBufferClear
                };

                string path = GetSettingsPath();
                string? dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, JsonSerializer.Serialize(s,
                    new JsonSerializerOptions { WriteIndented = true }));

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
    }
}

// ============================================================
// PostControl.xaml.cs — Логика управления одним постом ТРК
// ============================================================
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FuelMasterInterop;

namespace MultiFuelMaster.UI
{
    public partial class PostControl : UserControl
    {
        private readonly int _postNumber;
        private DispenserBridge _bridge;
        private DispatcherTimer _pollTimer;
        private DateTime _connectTime = DateTime.MinValue;

        private bool _isPolling       = false;
        private bool _isDisconnecting = false;
        private bool _isUpdatingPreset = false;
        private bool _transactionShown = false;
        private int  _pollTickCount    = 0;

        private string _portName     = "";
        private string _fuelType     = "АИ-95";
        private double _pricePerLiter = 2233;

        private int  _timingResponseTimeout  = 80;
        private int  _timingInterByteTimeout = 20;
        private int  _timingMaxRetries       = 3;
        private int  _timingInterCommandDelay = 10;
        private int  _timingIdlePollDelay    = 450;
        private int  _timingLinkLostPoll     = 350;
        private int  _timingPostEndDelay     = 800;
        private int  _timingErrorThreshold   = 6;
        private bool _timingForceBufferClear = false;

        private string _lastLitersText  = "";
        private string _lastCostText    = "";
        private string _lastTotalText   = "";
        private string _lastStatusKey   = "";
        private ManagedDispenserState _lastPollState = ManagedDispenserState.Error;

        // Путь к лог-файлу
        private readonly string _uiLogPath;
        private static readonly object _logLock = new object();

        public PostControl(int postNumber)
        {
            InitializeComponent();
            _postNumber = postNumber;

            _uiLogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiFuelMaster", $"ui_post{postNumber}.log");

            PostNumberText.Text = postNumber.ToString();
            PostTitleText.Text  = $"Пост №{postNumber}";

            _bridge    = new DispenserBridge();
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _pollTimer.Tick += PollTimer_Tick;

            LoadSettings();
            ApplySettingsToUi();
            SetStatusCached("nolink");
        }

        // ===== НАСТРОЙКИ =====

        private string GetSettingsPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiFuelMaster", $"settings_post{_postNumber}.json");

        private void LoadSettings()
        {
            try
            {
                string path = GetSettingsPath();
                if (!File.Exists(path)) return;

                string json = File.ReadAllText(path);
                var s = JsonSerializer.Deserialize<PostSettings>(json);
                if (s == null) return;

                if (!string.IsNullOrWhiteSpace(s.Port))        _portName      = s.Port.Trim();
                if (s.PricePerLiter > 0)                        _pricePerLiter = s.PricePerLiter;
                if (!string.IsNullOrWhiteSpace(s.FuelType))    _fuelType      = s.FuelType.Trim();
                if (s.ResponseTimeoutMs   > 0) _timingResponseTimeout  = s.ResponseTimeoutMs;
                if (s.InterByteTimeoutMs  > 0) _timingInterByteTimeout = s.InterByteTimeoutMs;
                if (s.MaxRetries          > 0) _timingMaxRetries        = s.MaxRetries;
                if (s.InterCommandDelayMs > 0) _timingInterCommandDelay = s.InterCommandDelayMs;
                if (s.IdlePollDelayMs     > 0) _timingIdlePollDelay     = s.IdlePollDelayMs;
                if (s.LinkLostPollMs      > 0) _timingLinkLostPoll      = s.LinkLostPollMs;
                if (s.PostEndDelayMs      > 0) _timingPostEndDelay      = s.PostEndDelayMs;
                if (s.ErrorThreshold      > 0) _timingErrorThreshold    = s.ErrorThreshold;
                _timingForceBufferClear = s.ForceBufferClear;
            }
            catch { }
        }

        private void ApplySettingsToUi()
        {
            if (!string.IsNullOrWhiteSpace(_portName))
                ComPortInput.Text = _portName;
            FuelTypeDisplay.Text = _fuelType;
            PriceDisplay.Text    = ((int)_pricePerLiter).ToString(CultureInfo.InvariantCulture);
            SubtitleText.Text    = $"{_fuelType} — не подключено";
        }

        private class PostSettings
        {
            public string Port             { get; set; } = "COM3";
            public double PricePerLiter    { get; set; } = 2233;
            public string FuelType         { get; set; } = "АИ-95";
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

        // ===== ПОДКЛЮЧЕНИЕ =====

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_bridge.IsConnected)
            {
                SafeDisconnect();
                return;
            }

            string port = ComPortInput.Text.Trim();
            if (string.IsNullOrEmpty(port)) { SetStatusCached("error"); return; }

            try
            {
                _portName = port;
                _bridge.SetTimingParams(
                    _timingResponseTimeout, _timingInterByteTimeout, _timingMaxRetries,
                    _timingInterCommandDelay, _timingIdlePollDelay, _timingLinkLostPoll,
                    _timingPostEndDelay, _timingErrorThreshold, _timingForceBufferClear);

                bool ok = _bridge.Connect(_portName, "01");
                if (!ok) { SetStatusCached("error"); return; }

                _lastStatusKey    = "";
                _lastPollState    = ManagedDispenserState.Error;
                _transactionShown = false;
                _connectTime      = DateTime.UtcNow;
                _pollTickCount    = 0;

                SetStatusCached("connected");
                BtnConnect.Content  = "Отключить";
                BtnStart.IsEnabled  = false;
                BtnStop.IsEnabled   = false;
                SetInputsEnabled(true);
                SubtitleText.Text   = $"{_portName} — {_fuelType}";
                _pollTimer.Start();
            }
            catch { SetStatusCached("error"); }
        }

        private void SafeDisconnect()
        {
            _isDisconnecting = true;
            try
            {
                _pollTimer.Stop();
                try { _bridge.Disconnect(); } catch { }
                BtnConnect.Content  = "Подключить";
                BtnStart.IsEnabled  = false;
                BtnStop.IsEnabled   = false;
                SetStatusCached("nolink");
                SubtitleText.Text   = $"{_fuelType} — не подключено";
            }
            finally { _isDisconnecting = false; }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow(_postNumber);
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true)
            {
                _portName      = win.SelectedPort;
                _pricePerLiter = win.PricePerLiter;
                _fuelType      = win.FuelType;

                _timingResponseTimeout  = win.ResponseTimeoutMs;
                _timingInterByteTimeout = win.InterByteTimeoutMs;
                _timingMaxRetries       = win.MaxRetries;
                _timingInterCommandDelay = win.InterCommandDelayMs;
                _timingIdlePollDelay    = win.IdlePollDelayMs;
                _timingLinkLostPoll     = win.LinkLostPollMs;
                _timingPostEndDelay     = win.PostEndDelayMs;
                _timingErrorThreshold   = win.ErrorThreshold;
                _timingForceBufferClear = win.ForceBufferClear;

                ApplySettingsToUi();

                if (_bridge.IsConnected)
                {
                    _bridge.SetTimingParams(
                        _timingResponseTimeout, _timingInterByteTimeout, _timingMaxRetries,
                        _timingInterCommandDelay, _timingIdlePollDelay, _timingLinkLostPoll,
                        _timingPostEndDelay, _timingErrorThreshold, _timingForceBufferClear);
                }
            }
        }

        // ===== СТАРТ / СТОП =====

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!_bridge.IsConnected) return;
            _transactionShown = false;

            string litersText = PresetLitersInput.Text.Trim().Replace(',', '.');
            string moneyText  = PresetCostInput.Text.Trim().Replace(',', '.');

            if (!string.IsNullOrEmpty(litersText))
            {
                if (double.TryParse(litersText, NumberStyles.Any, CultureInfo.InvariantCulture, out double liters) && liters > 0)
                    _bridge.QueueVolumePreset(liters, (int)_pricePerLiter);
                else
                    SetStatusCached("error");
                return;
            }
            if (!string.IsNullOrEmpty(moneyText))
            {
                if (double.TryParse(moneyText, NumberStyles.Any, CultureInfo.InvariantCulture, out double money) && money > 0)
                    _bridge.QueueMoneyPreset((int)money, (int)_pricePerLiter);
                else
                    SetStatusCached("error");
                return;
            }
            // Без пресета — просто вызов
            SetStatusCached("calling");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_bridge.IsConnected) return;
            _bridge.QueueStop();
            SetStatusCached("waiting");
        }

        // ===== ПОЛЯ ВВОДА =====

        private void NumericInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            char c = e.Text.Length > 0 ? e.Text[0] : '\0';
            e.Handled = !(char.IsDigit(c) || c == '.' || c == ',');
        }

        private void PresetLiters_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPreset || PresetCostInput == null) return;
            _isUpdatingPreset = true;
            string text = PresetLitersInput.Text.Trim().Replace(',', '.');
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double liters) && liters > 0)
                PresetCostInput.Text = ((int)(liters * _pricePerLiter)).ToString();
            else
                PresetCostInput.Text = "";
            _isUpdatingPreset = false;
        }

        private void PresetCost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPreset || PresetLitersInput == null) return;
            _isUpdatingPreset = true;
            string text = PresetCostInput.Text.Trim().Replace(',', '.');
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double cost) && cost > 0)
                PresetLitersInput.Text = (cost / _pricePerLiter).ToString("F2");
            else
                PresetLitersInput.Text = "";
            _isUpdatingPreset = false;
        }

        private void SetInputsEnabled(bool enabled)
        {
            PresetLitersInput.IsEnabled = enabled;
            PresetCostInput.IsEnabled   = enabled;
        }

        // ===== ПОЛЛИНГ =====

        private void PollTimer_Tick(object? sender, EventArgs e)
        {
            if (_isPolling || _isDisconnecting) return;
            _pollTickCount++;

            bool portOpen; int errorCount;
            try { portOpen = _bridge.IsConnected; errorCount = _bridge.ErrorCount; }
            catch { portOpen = false; errorCount = 0; }

            bool inGrace = (DateTime.UtcNow - _connectTime).TotalMilliseconds < 1500;

            if (!portOpen || (!inGrace && errorCount >= _timingErrorThreshold))
            {
                _lastStatusKey = ""; _lastPollState = ManagedDispenserState.Error;
                SetStatusCached("nolink");
                BtnStart.IsEnabled = false; BtnStop.IsEnabled = false;
                SetInputsEnabled(true);
                SubtitleText.Text = $"{_fuelType} — нет связи";
                return;
            }

            _isPolling = true;
            try
            {
                var state  = _bridge.CurrentState;
                double liters = _bridge.CurrentLiters;
                double money  = _bridge.CurrentMoney;
                double total  = _bridge.TotalCounter;

                if (SubtitleText.Text.Contains("нет связи"))
                    SubtitleText.Text = $"{_portName} — {_fuelType}";

                if (!_transactionShown)
                {
                    string lt = liters.ToString("F2");
                    if (_lastLitersText != lt) { _lastLitersText = lt; LitersDisplay.Text = lt; }

                    string ct = ((int)money).ToString("N0");
                    if (_lastCostText != ct) { _lastCostText = ct; CostDisplay.Text = ct; }
                }

                string totText = FormatTot(total);
                if (_lastTotalText != totText) { _lastTotalText = totText; TotDisplay.Text = totText; }

                if (_lastPollState != state)
                {
                    _lastPollState = state;
                    switch (state)
                    {
                        case ManagedDispenserState.Idle:
                            BtnStart.IsEnabled = true; BtnStop.IsEnabled = false;
                            SetInputsEnabled(true); SetStatusCached("ready"); break;

                        case ManagedDispenserState.Calling:
                            BtnStart.IsEnabled = true; BtnStop.IsEnabled = false;
                            SetInputsEnabled(true); SetStatusCached("nozzle_up"); break;

                        case ManagedDispenserState.Authorized:
                        case ManagedDispenserState.Started:
                            BtnStart.IsEnabled = false; BtnStop.IsEnabled = true;
                            SetInputsEnabled(false); SetStatusCached("waiting"); break;

                        case ManagedDispenserState.Fuelling:
                            BtnStart.IsEnabled = false; BtnStop.IsEnabled = true;
                            SetInputsEnabled(false); SetStatusCached("fuelling"); break;

                        case ManagedDispenserState.Stopped:
                            BtnStart.IsEnabled = true; BtnStop.IsEnabled = false;
                            SetInputsEnabled(true); SetStatusCached("done"); break;

                        case ManagedDispenserState.EndOfTransaction:
                            LitersDisplay.Text = liters.ToString("F2");
                            CostDisplay.Text   = ((int)money).ToString("N0");
                            _lastLitersText = LitersDisplay.Text;
                            _lastCostText   = CostDisplay.Text;
                            _transactionShown = true;
                            BtnStart.IsEnabled = true; BtnStop.IsEnabled = false;
                            SetInputsEnabled(true); SetStatusCached("done"); break;

                        case ManagedDispenserState.Error:
                            BtnStart.IsEnabled = false; BtnStop.IsEnabled = false;
                            SetInputsEnabled(true); SetStatusCached("nolink"); break;
                    }
                }
            }
            catch
            {
                _lastStatusKey = ""; _lastPollState = ManagedDispenserState.Error;
                SetStatusCached("nolink");
            }
            finally { _isPolling = false; }
        }

        private static string FormatTot(double total)
        {
            if (double.IsNaN(total) || double.IsInfinity(total)) return "—";
            string s = total.ToString("0.##", CultureInfo.InvariantCulture);
            return s == "0" ? "0" : s;
        }

        // ===== СТАТУС =====

        private void SetStatusCached(string key)
        {
            if (_lastStatusKey == key) return;
            _lastStatusKey = key;

            string text; Brush dotBrush; string bgHex, borderHex;
            switch (key)
            {
                case "ready":
                    text = "Готова"; dotBrush = Brushes.LimeGreen;
                    bgHex = "#1A3A1A"; borderHex = "#2D5A2D"; break;
                case "connected":
                    text = "Подключена"; dotBrush = Brushes.LimeGreen;
                    bgHex = "#1A3A1A"; borderHex = "#2D5A2D"; break;
                case "nozzle_up":
                    text = "Пистолет поднят"; dotBrush = Brushes.Orange;
                    bgHex = "#3A351A"; borderHex = "#5A4D2D"; break;
                case "calling":
                    text = "Вызов"; dotBrush = Brushes.Orange;
                    bgHex = "#3A351A"; borderHex = "#5A4D2D"; break;
                case "waiting":
                    text = "Ожидание"; dotBrush = Brushes.CornflowerBlue;
                    bgHex = "#1A2A3A"; borderHex = "#2D4D5A"; break;
                case "fuelling":
                    text = "Отпуск"; dotBrush = Brushes.LimeGreen;
                    bgHex = "#1A3A1A"; borderHex = "#2D5A2D"; break;
                case "done":
                    text = "Завершено"; dotBrush = Brushes.CornflowerBlue;
                    bgHex = "#1A2A3A"; borderHex = "#2D4D5A"; break;
                case "nolink":
                    text = "Нет связи"; dotBrush = Brushes.Tomato;
                    bgHex = "#3A1A1A"; borderHex = "#5A2D2D"; break;
                default:
                    text = "Ошибка"; dotBrush = Brushes.Tomato;
                    bgHex = "#3A1A1A"; borderHex = "#5A2D2D"; break;
            }

            StatusText.Text       = text;
            StatusText.Foreground = dotBrush;
            StatusDot.Fill        = dotBrush;
            StatusBorder.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex));
            StatusBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex));
        }

        // ===== ОСВОБОЖДЕНИЕ РЕСУРСОВ =====

        public void Shutdown()
        {
            try { _pollTimer?.Stop(); } catch { }
            try { if (_bridge?.IsConnected == true) _bridge.Disconnect(); } catch { }
        }
    }
}

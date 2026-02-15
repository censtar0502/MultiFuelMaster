using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MultiFuelMaster.Models.Runtime;

namespace MultiFuelMaster.Services
{
    /// <summary>
    /// Runtime (UI) state for posts / tanks / shift / alerts.
    /// Сейчас работает как UI-симулятор, позже будет заполняться реальными данными протокола.
    /// </summary>
    public sealed partial class RuntimeStateService : ObservableObject
    {
        private readonly DispatcherTimer _uiTimer;
        private readonly Random _rng = new();

        public ObservableCollection<PostRuntimeState> Posts { get; } = new();
        public ObservableCollection<TankRuntimeState> Tanks { get; } = new();
        public ObservableCollection<AlertRuntimeItem> Alerts { get; } = new();

        public ShiftRuntimeState Shift { get; } = new();

        public RuntimeStateService()
        {
            // Инициализация 8 постов (UI-скелет)
            for (int i = 1; i <= 8; i++)
            {
                Posts.Add(new PostRuntimeState
                {
                    PostNumber = i,
                    Connection = PostConnectionState.Offline,
                    Operation = PostOperationState.Unknown,
                    FuelName = "—",
                    Price = 0m,
                    VolumeL = 0.0,
                    Amount = 0m,
                    LastUpdate = DateTime.MinValue
                });
            }

            // Пример 4 резервуаров
            for (int i = 1; i <= 4; i++)
            {
                Tanks.Add(new TankRuntimeState
                {
                    Number = i,
                    FuelName = i switch
                    {
                        1 => "А-92",
                        2 => "А-95",
                        3 => "ДТ",
                        _ => "А-80"
                    },
                    LevelPercent = 60 - (i * 8),
                    MinLevelPercent = 25
                });
            }

            // Пара демонстрационных событий
            Alerts.Add(new AlertRuntimeItem { Severity = AlertSeverity.Info, Title = "Система", Message = "Готово к работе" });

            // UI-таймер: имитация обновлений без блокировок (можно выключить в будущем)
            _uiTimer = new DispatcherTimer(DispatcherPriority.Background);
            _uiTimer.Interval = TimeSpan.FromMilliseconds(500);
            _uiTimer.Tick += (_, __) => TickSimulation();
            _uiTimer.Start();
        }

        public int OnlinePostsCount => Posts.Count(p => p.Connection == PostConnectionState.Online);
        public int TotalPostsCount => Posts.Count;
        public int ActiveAlertsCount => Alerts.Count(a => a.Severity != AlertSeverity.Info);

        public void OpenShift(string operatorLogin)
        {
            Shift.IsOpen = true;
            Shift.OperatorLogin = operatorLogin;
            Shift.OpenTime = DateTime.Now;
            Shift.CloseTime = null;

            Alerts.Insert(0, new AlertRuntimeItem { Severity = AlertSeverity.Info, Title = "Смена", Message = $"Открыта смена: {operatorLogin}" });
            TrimAlerts();
            NotifyCounters();
        }

        public void CloseShift(string operatorLogin)
        {
            Shift.IsOpen = false;
            Shift.OperatorLogin = operatorLogin;
            Shift.CloseTime = DateTime.Now;

            Alerts.Insert(0, new AlertRuntimeItem { Severity = AlertSeverity.Info, Title = "Смена", Message = $"Закрыта смена: {operatorLogin}" });
            TrimAlerts();
            NotifyCounters();
        }

        private void TickSimulation()
        {
            foreach (var p in Posts)
            {
                // 30% шанс стать онлайн для первых 4 постов
                if (p.PostNumber <= 4)
                {
                    if (p.Connection == PostConnectionState.Offline && _rng.NextDouble() < 0.15)
                    {
                        p.Connection = PostConnectionState.Online;
                        p.Operation = PostOperationState.Ready;
                        p.FuelName = p.PostNumber switch
                        {
                            1 => "А-92",
                            2 => "А-95",
                            3 => "ДТ",
                            _ => "А-80"
                        };
                        p.Price = p.FuelName switch
                        {
                            "А-92" => 8750m,
                            "А-95" => 9450m,
                            "ДТ" => 9800m,
                            _ => 8200m
                        };
                        p.LastUpdate = DateTime.Now;
                    }

                    // 10% шанс начать "отпуск"
                    if (p.Connection == PostConnectionState.Online && p.Operation == PostOperationState.Ready && _rng.NextDouble() < 0.08)
                    {
                        p.Operation = PostOperationState.Fuelling;
                        p.VolumeL = 0.0;
                        p.Amount = 0m;
                        p.LastUpdate = DateTime.Now;
                    }

                    if (p.Connection == PostConnectionState.Online && p.Operation == PostOperationState.Fuelling)
                    {
                        p.VolumeL += 0.5 + _rng.NextDouble();
                        p.Amount = (decimal)p.VolumeL * p.Price;
                        p.LastUpdate = DateTime.Now;

                        if (p.VolumeL > 15 && _rng.NextDouble() < 0.25)
                        {
                            p.Operation = PostOperationState.End;
                            Alerts.Insert(0, new AlertRuntimeItem { Severity = AlertSeverity.Info, Title = p.DisplayName, Message = $"Завершено: {p.VolumeL:0.00} л" });
                            TrimAlerts();
                        }
                    }

                    if (p.Operation == PostOperationState.End && _rng.NextDouble() < 0.20)
                    {
                        p.Operation = PostOperationState.Ready;
                        p.VolumeL = 0.0;
                        p.Amount = 0m;
                        p.LastUpdate = DateTime.Now;
                    }
                }
            }

            foreach (var t in Tanks)
            {
                if (_rng.NextDouble() < 0.15)
                    t.LevelPercent = Math.Max(0, t.LevelPercent - 0.2);
            }

            NotifyCounters();
        }

        private void NotifyCounters()
        {
            OnPropertyChanged(nameof(OnlinePostsCount));
            OnPropertyChanged(nameof(TotalPostsCount));
            OnPropertyChanged(nameof(ActiveAlertsCount));
        }

        private void TrimAlerts()
        {
            while (Alerts.Count > 200)
                Alerts.RemoveAt(Alerts.Count - 1);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFuelMaster.Services
{
    /// <summary>
    /// Logging service with queue-based asynchronous file writing
    /// </summary>
    public class LoggingService
    {
        private readonly string _logFilePath;
        private static LoggingService? _instance;
        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _writerTask;

        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("LoggingService не инициализирован");
                }
                return _instance;
            }
        }

        public static void Initialize(string logDirectory)
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            var fileName = "user_actions_" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            _instance = new LoggingService(Path.Combine(logDirectory, fileName));
        }

        private LoggingService(string logFilePath)
        {
            _logFilePath = logFilePath;
            // Запускаем фоновую задачу для записи логов
            _writerTask = Task.Run(ProcessLogQueueAsync);
        }

        private async Task ProcessLogQueueAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_logQueue.TryDequeue(out var logEntry))
                    {
                        await WriteToFileAsync(logEntry);
                    }
                    else
                    {
                        // Если очередь пуста, ждём немного
                        await Task.Delay(100, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Выход при отмене
                    break;
                }
                catch
                {
                    // Игнорируем ошибки записи, продолжаем работу
                }
            }

            // Дозапись оставшихся логов перед выходом
            while (_logQueue.TryDequeue(out var logEntry))
            {
                try
                {
                    await WriteToFileAsync(logEntry);
                }
                catch
                {
                    // Игнорируем
                }
            }
        }

        private async Task WriteToFileAsync(string logEntry)
        {
            try
            {
                await File.AppendAllTextAsync(_logFilePath, logEntry, Encoding.UTF8);
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }

        public void LogAction(string userLogin, string action, string? details = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = new StringBuilder();
                logEntry.AppendLine("[" + timestamp + "] ПОЛЬЗОВАТЕЛЬ: " + userLogin);
                logEntry.AppendLine("    ДЕЙСТВИЕ: " + action);
                if (!string.IsNullOrEmpty(details))
                {
                    logEntry.AppendLine("    ДЕТАЛИ: " + details);
                }
                logEntry.AppendLine(new string('-', 50));

                // Добавляем в очередь (неблокирующая операция)
                _logQueue.Enqueue(logEntry.ToString());
            }
            catch
            {
                // Игнорируем ошибки добавления в очередь
            }
        }

        public void LogLogin(string userLogin)
        {
            LogAction(userLogin, "ВХОД В СИСТЕМУ", "IP: localhost");
        }

        public void LogLogout(string userLogin)
        {
            LogAction(userLogin, "ВЫХОД ИЗ СИСТЕМУ", "Время сессии: " + DateTime.Now.ToString("HH:mm:ss"));
        }

        public static void Log(string userLogin, string action, string? details = null)
        {
            Instance.LogAction(userLogin, action, details);
        }

        /// <summary>
        /// Остановка сервиса логирования (вызывать при завершении приложения)
        /// </summary>
        public static void Shutdown()
        {
            if (_instance != null)
            {
                _instance._cts.Cancel();
                try
                {
                    _instance._writerTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    // Игнорируем
                }
            }
        }
    }
}

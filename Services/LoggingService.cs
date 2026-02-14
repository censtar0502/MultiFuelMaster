using System;
using System.IO;
using System.Text;

namespace MultiFuelMaster.Services
{
    public class LoggingService
    {
        private readonly string _logFilePath;
        private static LoggingService? _instance;
        private static readonly object _lock = new object();

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
        }

        public void LogAction(string userLogin, string action, string? details = null)
        {
            lock (_lock)
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
                    File.AppendAllText(_logFilePath, logEntry.ToString(), Encoding.UTF8);
                }
                catch
                {
                }
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
    }
}

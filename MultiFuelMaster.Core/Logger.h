// ============================================================
// Logger.h — Logging to file with rotation
// ============================================================

#pragma once

#include <string>
#include <fstream>
#include <mutex>
#include <memory>
#include <vector>
#include <chrono>
#include <sstream>
#include <iomanip>
#include <thread>
#include <cstdarg>
#include <algorithm>

namespace FuelMaster {

// Simple enum without class (with LVL_ prefix to avoid conflicts with Windows macros)
enum LogLevel
{
    LVL_TRACE = 0,
    LVL_INFO = 1,
    LVL_WARNING = 2,
    LVL_ERROR = 3
};

class Logger
{
public:
    // Get logger instance (singleton)
    static Logger& Instance()
    {
        static Logger instance;
        return instance;
    }

    // Initialize logger
    void Initialize(const std::string& logPath, int maxFileSizeMb = 10, int maxFiles = 5);
    
    // Auto-initialize
    void AutoInitialize();

    // Shutdown logger
    void Shutdown();

    // Main logging methods
    void Trace(const std::string& message);
    void Info(const std::string& message);
    void Warning(const std::string& message);
    void Error(const std::string& message);

    // Formatted logging
    void Log(LogLevel level, const char* format, ...);
    void Trace(const char* format, ...);
    void Info(const char* format, ...);
    void Warning(const char* format, ...);
    void Error(const char* format, ...);

    // Hex dump logging
    void LogHexDump(LogLevel level, const std::string& prefix, const std::vector<uint8_t>& data);

    // Set minimum level
    void SetMinLevel(LogLevel level) { m_minLevel = level; }

    // Check initialization
    bool IsInitialized() const { return m_initialized; }

private:
    Logger();
    ~Logger();
    Logger(const Logger&);
    Logger& operator=(const Logger&);

    void LogInternal(LogLevel level, const std::string& message);
    void CheckRotation();
    std::string GetTimestamp();
    std::string GetLevelString(LogLevel level);
    std::string GetThreadId();

    std::ofstream m_file;
    std::mutex m_mutex;
    std::string m_logPath;
    std::string m_currentLogPath;
    int m_maxFileSize;
    int m_maxFiles;
    bool m_initialized;
    LogLevel m_minLevel;
    size_t m_currentFileSize;
};

// Macros (with FM_ prefix to avoid conflicts with Windows)
// Using __VA_ARGS__ to support formatting
#define FM_LOG_TRACE(...) FuelMaster::Logger::Instance().Trace(__VA_ARGS__)
#define FM_LOG_INFO(...) FuelMaster::Logger::Instance().Info(__VA_ARGS__)
#define FM_LOG_WARNING(...) FuelMaster::Logger::Instance().Warning(__VA_ARGS__)
#define FM_LOG_ERROR(...) FuelMaster::Logger::Instance().Error(__VA_ARGS__)

#define FM_LOG_HEX_TRACE(prefix, data) \
    FuelMaster::Logger::Instance().LogHexDump(FuelMaster::LVL_TRACE, prefix, data)
#define FM_LOG_HEX_INFO(prefix, data) \
    FuelMaster::Logger::Instance().LogHexDump(FuelMaster::LVL_INFO, prefix, data)
#define FM_LOG_HEX_WARNING(prefix, data) \
    FuelMaster::Logger::Instance().LogHexDump(FuelMaster::LVL_WARNING, prefix, data)

} // namespace FuelMaster

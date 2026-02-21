// ============================================================
// Logger.cpp — Logging implementation
// ============================================================
#include <iomanip>
#include "pch.h"
#include "Logger.h"
#include <cstdarg>
#include <ctime>
#include <algorithm>

namespace FuelMaster {

// ============================================================
// Constructor / Destructor
// ============================================================

Logger::Logger()
    : m_maxFileSize(10 * 1024 * 1024)  // 10 MB by default
    , m_maxFiles(5)
    , m_initialized(false)
    , m_minLevel(LVL_TRACE)
    , m_currentFileSize(0)
{
}

Logger::~Logger()
{
    Shutdown();
}

// ============================================================
// Initialization
// ============================================================

void Logger::Initialize(const std::string& logPath, int maxFileSizeMb, int maxFiles)
{
    std::lock_guard<std::mutex> lock(m_mutex);

    if (m_initialized)
    {
        return;
    }

    m_logPath = logPath;
    m_maxFileSize = maxFileSizeMb * 1024 * 1024;
    m_maxFiles = maxFiles;

    // Create directory if it doesn't exist (Win32 API)
    size_t pos = logPath.find_last_of("/\\");
    if (pos != std::string::npos)
    {
        std::string dir = logPath.substr(0, pos);
        CreateDirectoryA(dir.c_str(), NULL);
    }

    // Open file
    m_currentLogPath = logPath;
    m_file.open(m_currentLogPath, std::ios::app | std::ios::binary);
    
    if (m_file.is_open())
    {
        m_initialized = true;
        
        // Write start string
        std::string startMsg = "===========================================\n";
        startMsg += "FuelMaster Logger Initialized\n";
        startMsg += "Log file: " + logPath + "\n";
        startMsg += "===========================================\n";
        
        m_file << startMsg << std::flush;
        m_currentFileSize = static_cast<size_t>(m_file.tellp());
        
        Info("Logger initialized successfully");
    }
}

// ============================================================
// Shutdown
// ============================================================

void Logger::Shutdown()
{
    std::lock_guard<std::mutex> lock(m_mutex);

    if (!m_initialized)
    {
        return;
    }

    Info("Logger shutting down");

    if (m_file.is_open())
    {
        m_file.flush();
        m_file.close();
    }

    m_initialized = false;
}

// ============================================================
// Auto-initialization
// ============================================================

void Logger::AutoInitialize()
{
    if (m_initialized)
    {
        return;
    }

    // Determine log file path
    std::string appDataPath;
    
#if defined(_WIN32)
    char* pathBuf = nullptr;
    size_t size = 0;
    if (_dupenv_s(&pathBuf, &size, "LOCALAPPDATA") == 0 && pathBuf != nullptr)
    {
        appDataPath = pathBuf;
        free(pathBuf);
    }
#else
    appDataPath = "/tmp";
#endif

    if (appDataPath.empty())
    {
        appDataPath = ".";
    }

    std::string logPath = appDataPath + "/FuelMaster/fuelmaster.log";
    
    // Initialize logger
    Initialize(logPath, 10, 3);
}

// ============================================================
// Main logging methods
// ============================================================

void Logger::LogInternal(LogLevel level, const std::string& message)
{
    // Auto-initialize on first log
    if (!m_initialized)
    {
        AutoInitialize();
    }

    if (!m_initialized)
    {
        return;
    }

    if (level < m_minLevel)
{
        return;
    }

    std::lock_guard<std::mutex> lock(m_mutex);

    // Check rotation
    CheckRotation();

    // Format log string
    std::ostringstream oss;
    oss << "[" << GetTimestamp() << "] ";
    oss << "[" << GetLevelString(level) << "] ";
    oss << "[Thread " << GetThreadId() << "] ";
    oss << message;
    oss << "\n";

    std::string logLine = oss.str();

    // Write to file
    if (m_file.is_open())
    {
        m_file << logLine << std::flush;
        m_currentFileSize += logLine.length();
    }
}

void Logger::Trace(const std::string& message)
{
    LogInternal(LVL_TRACE, message);
}

void Logger::Info(const std::string& message)
{
    LogInternal(LVL_INFO, message);
}

void Logger::Warning(const std::string& message)
{
    LogInternal(LVL_WARNING, message);
}

void Logger::Error(const std::string& message)
{
    LogInternal(LVL_ERROR, message);
}

// ============================================================
// Formatted logging
// ============================================================

#pragma managed(push, off)

void Logger::Log(LogLevel level, const char* format, ...)
{
    char buffer[4096];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    LogInternal(level, buffer);
}

void Logger::Trace(const char* format, ...)
{
    char buffer[4096];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    LogInternal(LVL_TRACE, buffer);
}

void Logger::Info(const char* format, ...)
{
    char buffer[4096];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    LogInternal(LVL_INFO, buffer);
}

void Logger::Warning(const char* format, ...)
{
    char buffer[4096];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    LogInternal(LVL_WARNING, buffer);
}

void Logger::Error(const char* format, ...)
{
    char buffer[4096];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);
    LogInternal(LVL_ERROR, buffer);
}

#pragma managed(pop)

// ============================================================
// Hex dump logging
// ============================================================

void Logger::LogHexDump(LogLevel level, const std::string& prefix, 
                        const std::vector<uint8_t>& data)
{
    if (!m_initialized || level < m_minLevel)
    {
        return;
    }

    std::ostringstream oss;
    oss << prefix << " (" << data.size() << " bytes): ";

    // Output up to 64 bytes in one line
    size_t maxBytes = (data.size() < 64) ? data.size() : 64;
    for (size_t i = 0; i < maxBytes; i++)
    {
        oss << std::uppercase << std::hex << std::setw(2) << std::setfill('0')
            << static_cast<int>(data[i]) << " ";
    }

    if (data.size() > 64)
    {
        oss << "... (" << (data.size() - 64) << " more bytes)";
    }

    LogInternal(level, oss.str());
}

// ============================================================
// Helper methods
// ============================================================

void Logger::CheckRotation()
{
    if (m_currentFileSize > static_cast<size_t>(m_maxFileSize) && m_maxFiles > 1)
    {
        // Close current file
        m_file.close();

        // Rename existing files
        // fuelmaster.log.4 -> fuelmaster.log.5, fuelmaster.log.3 -> fuelmaster.log.4, etc.
        for (int i = m_maxFiles - 2; i >= 0; i--)
        {
            std::string oldName, newName;
            
            if (i == 0)
            {
                oldName = m_logPath;
                newName = m_logPath + ".1";
            }
            else
            {
                oldName = m_logPath + "." + std::to_string(i);
                newName = m_logPath + "." + std::to_string(i + 1);
            }

            // Check existence and delete old file (Win32 API)
            if (GetFileAttributesA(newName.c_str()) != INVALID_FILE_ATTRIBUTES)
            {
                DeleteFileA(newName.c_str());
            }

            // Rename
            if (GetFileAttributesA(oldName.c_str()) != INVALID_FILE_ATTRIBUTES)
            {
                MoveFileExA(oldName.c_str(), newName.c_str(), MOVEFILE_REPLACE_EXISTING);
            }
        }

        // Open new file
        m_file.open(m_logPath, std::ios::app | std::ios::binary);
        m_currentFileSize = 0;

        if (m_file.is_open())
        {
            std::string rotateMsg = "--- Log rotated ---\n";
            m_file << rotateMsg << std::flush;
        }
    }
}

std::string Logger::GetTimestamp()
{
    auto now = std::chrono::system_clock::now();
    auto time = std::chrono::system_clock::to_time_t(now);
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
        now.time_since_epoch()) % 1000;

    std::tm tmBuf;
#if defined(_WIN32)
    localtime_s(&tmBuf, &time);
#else
    localtime_r(&time, &tmBuf);
#endif

    std::ostringstream oss;
    oss << std::put_time(&tmBuf, "%Y-%m-%d %H:%M:%S");
    oss << "." << std::setfill('0') << std::setw(3) << ms.count();
    
    return oss.str();
}

std::string Logger::GetLevelString(LogLevel level)
{
    switch (level)
    {
        case LVL_TRACE:   return "TRACE";
        case LVL_INFO:    return "INFO ";
        case LVL_WARNING: return "WARN ";
        case LVL_ERROR:   return "ERROR";
        default:          return "UNKN ";
    }
}

std::string Logger::GetThreadId()
{
    std::ostringstream oss;
    oss << std::this_thread::get_id();
    return oss.str();
}

} // namespace FuelMaster

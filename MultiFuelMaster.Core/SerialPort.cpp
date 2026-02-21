// ============================================================
// SerialPort.cpp — COM-port (v5 — improved frame reception)
// ============================================================
// Key fixes v5:
// 1. Increased interByteTimeout to 20ms (instead of 3ms) — Windows USB-UART
//    can group bytes into packets with 5-16ms delay between packets
// 2. Accumulate data in buffer and read until we receive valid frame
//    or total timeout expires
// 3. Removed dependency on inter-byte timeout as the only
//    end-of-frame criterion
// ============================================================

#include "pch.h"
#include "SerialPort.h"
#include <sstream>
#include <chrono>

namespace FuelMaster {

    SerialPort::SerialPort()
        : m_handle(INVALID_HANDLE_VALUE)
        , m_isOpen(false)
    {
    }

    SerialPort::~SerialPort()
    {
        Close();
    }

    bool SerialPort::Open(const std::string& portName, int baudRate)
    {
        if (m_isOpen) Close();
        m_portName = portName;

        std::string fullName = "\\\\.\\" + portName;

        m_handle = CreateFileA(
            fullName.c_str(),
            GENERIC_READ | GENERIC_WRITE,
            0, nullptr, OPEN_EXISTING, 0, nullptr);

        if (m_handle == INVALID_HANDLE_VALUE)
        {
            DWORD err = ::GetLastError();
            std::ostringstream oss;
            oss << "Cannot open " << portName << " (error " << err << ")";
            m_lastError = oss.str();
            return false;
        }

        if (!ConfigurePort(baudRate))
        {
            Close();
            return false;
        }

        PurgeComm(m_handle, PURGE_RXCLEAR | PURGE_TXCLEAR);
        m_isOpen = true;
        m_lastError = "";
        return true;
    }

    void SerialPort::Close()
    {
        if (m_handle != INVALID_HANDLE_VALUE)
        {
            CloseHandle(m_handle);
            m_handle = INVALID_HANDLE_VALUE;
        }
        m_isOpen = false;
    }

    bool SerialPort::IsOpen() const
    {
        return m_isOpen;
    }

    // ============================================================
    // Set read timeouts
    // ============================================================

    void SerialPort::SetReadTimeouts(int totalTimeoutMs, int interByteTimeoutMs)
    {
        COMMTIMEOUTS timeouts = {};
        timeouts.ReadIntervalTimeout = interByteTimeoutMs;
        timeouts.ReadTotalTimeoutMultiplier = 0;
        timeouts.ReadTotalTimeoutConstant = totalTimeoutMs;
        timeouts.WriteTotalTimeoutMultiplier = 0;
        timeouts.WriteTotalTimeoutConstant = 100;
        SetCommTimeouts(m_handle, &timeouts);
    }

    // ============================================================
    // SEND AND RECEIVE RESPONSE
    // ============================================================

    std::vector<uint8_t> SerialPort::SendAndReceive(
        const std::vector<uint8_t>& command,
        int responseTimeoutMs,
        int interByteTimeoutMs,
        bool forceBufferClear)
    {
        if (!m_isOpen) return {};

        // 1. Clear input buffer (if enabled)
        if (forceBufferClear)
            PurgeInput();

        // 2. Send command
        if (Write(command) == 0)
            return {};

        // 3. Read response - Windows itself waits for first byte up to responseTimeoutMs,
        //    then reads while pause < interByteTimeoutMs
        return ReadAvailable(64, responseTimeoutMs, interByteTimeoutMs);
    }

    // ============================================================
    // IMPROVED TIMEOUT READ (v5)
    // Key change: read data in chunks and accumulate in buffer,
    //until we get complete frame or total timeout expires.
    // This solves the packet fragmentation problem on USB-UART adapters.
    // ============================================================

    std::vector<uint8_t> SerialPort::ReadAvailable(int maxBytes, int totalTimeoutMs, int interByteTimeoutMs)
    {
        // Configure Windows timeouts:
        // ReadIntervalTimeout = interByteTimeoutMs - pause between bytes = "silence" indicator
        // ReadTotalTimeoutConstant = totalTimeoutMs - total wait timeout
        SetReadTimeouts(totalTimeoutMs, interByteTimeoutMs);

        std::vector<uint8_t> accumulatedBuffer;
        accumulatedBuffer.reserve(64); // Minimum size for typical frame

        auto startTime = std::chrono::steady_clock::now();

        while (true)
        {
            // Read available data
            std::vector<uint8_t> chunk(maxBytes);
            DWORD bytesRead = 0;

            BOOL success = ReadFile(
                m_handle,
                chunk.data(),
                (DWORD)maxBytes,
                &bytesRead,
                nullptr);

            if (success && bytesRead > 0)
            {
                // Add received data to buffer
                chunk.resize(bytesRead);
                accumulatedBuffer.insert(accumulatedBuffer.end(), chunk.begin(), chunk.end());

                // Check minimum frame size:
                // STX(1) + ADDR_HI(1) + ADDR_LO(1) + CMD(1) + DATA + CRC(1) = minimum ~7 bytes
                // For L/R commands (volume/sum) minimum 14-15 bytes
                // Wait at least 14 bytes to get complete response frame for LM/RS
                if (accumulatedBuffer.size() >= 14)
                {
                    // Enough data - return accumulated buffer
                    return accumulatedBuffer;
                }
            }

            // Check total timeout
            auto elapsed = std::chrono::steady_clock::now() - startTime;
            auto elapsedMs = std::chrono::duration_cast<std::chrono::milliseconds>(elapsed).count();

            if (elapsedMs >= totalTimeoutMs)
            {
                // Timeout expired - return what we have (may be empty)
                break;
            }

            // Small pause before next read attempt
            // This gives USB-UART driver time to collect data into packet
            Sleep(1);
        }

        // Return accumulated data (may be empty on timeout)
        return accumulatedBuffer;
    }

    // ============================================================
    // WRITE
    // ============================================================

    int SerialPort::Write(const std::vector<uint8_t>& data)
    {
        if (!m_isOpen || data.empty()) return 0;

        DWORD bytesWritten = 0;
        BOOL success = WriteFile(
            m_handle, data.data(), (DWORD)data.size(),
            &bytesWritten, nullptr);

        return success ? static_cast<int>(bytesWritten) : 0;
    }

    // ============================================================
    // CLEAR
    // ============================================================

    void SerialPort::PurgeInput()
    {
        if (m_isOpen)
            PurgeComm(m_handle, PURGE_RXCLEAR);
    }

    // ============================================================
    // PORT CONFIGURATION
    // ============================================================

    bool SerialPort::ConfigurePort(int baudRate)
    {
        DCB dcb = {};
        dcb.DCBlength = sizeof(DCB);

        if (!GetCommState(m_handle, &dcb))
        {
            m_lastError = "Cannot get port state";
            return false;
        }

        // GasKitLink: 9600, 8N1
        dcb.BaudRate = baudRate;
        dcb.ByteSize = 8;
        dcb.Parity = NOPARITY;
        dcb.StopBits = ONESTOPBIT;

        // Disable flow control
        dcb.fOutxCtsFlow = FALSE;
        dcb.fOutxDsrFlow = FALSE;
        dcb.fDtrControl = DTR_CONTROL_ENABLE;
        dcb.fRtsControl = RTS_CONTROL_ENABLE;
        dcb.fOutX = FALSE;
        dcb.fInX = FALSE;

        if (!SetCommState(m_handle, &dcb))
        {
            m_lastError = "Cannot set port state";
            return false;
        }

        return true;
    }

} // namespace FuelMaster

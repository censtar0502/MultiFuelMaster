// ============================================================
// SerialPort.h — COM-port (v4 — request-response model)
// ============================================================
#pragma once

#include <cstdint>
#include <vector>
#include <string>

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

namespace FuelMaster {

    class SerialPort
    {
    public:
        SerialPort();
        ~SerialPort();

        SerialPort(const SerialPort&) = delete;
        SerialPort& operator=(const SerialPort&) = delete;

        bool Open(const std::string& portName, int baudRate = 9600);
        void Close();
        bool IsOpen() const;

        /// Send command and get full response.
        /// Algorithm:
        /// 1. Clear input buffer (if forceBufferClear = true)
        /// 2. Send command
        /// 3. Wait for data with responseTimeoutMs timeout
        /// 4. After first byte - read remaining with interByteTimeoutMs
        /// 5. Return full frame (or empty vector if timeout)
        std::vector<uint8_t> SendAndReceive(const std::vector<uint8_t>& command,
            int responseTimeoutMs,
            int interByteTimeoutMs,
            bool forceBufferClear = true);

        /// Clear input buffer
        void PurgeInput();

        std::string GetPortName() const { return m_portName; }
        std::string GetLastError() const { return m_lastError; }

    private:
        HANDLE m_handle;
        std::string m_portName;
        std::string m_lastError;
        bool m_isOpen;

        bool ConfigurePort(int baudRate);
        int Write(const std::vector<uint8_t>& data);

        /// Set read timeouts
        void SetReadTimeouts(int totalTimeoutMs, int interByteTimeoutMs);

        /// Read available bytes (up to maxBytes)
        std::vector<uint8_t> ReadAvailable(int maxBytes, int totalTimeoutMs, int interByteTimeoutMs);
    };

} // namespace FuelMaster

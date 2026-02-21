// ============================================================
// DispenserController.h — Dispenser Controller
// ============================================================
// FSM is the single source of truth for state.
// Controller executes actions as directed by FSM.
// Separate statistics: CRC errors vs connection loss.
// ============================================================

#pragma once

#include "GasKitProtocol.h"
#include "SerialPort.h"
#include "DispenserFSM.h"
#include <functional>
#include <thread>
#include <mutex>
#include <atomic>
#include <queue>

namespace FuelMaster {

    using StatusCallback = std::function<void(Protocol::DispenserState state, int nozzle)>;
    using FuelDataCallback = std::function<void(double liters, double money)>;
    using TransactionCompleteCallback = std::function<void(double totalLiters, double totalMoney, int price)>;
    using ErrorCallback = std::function<void(const std::string& message)>;
    using LogCallback = std::function<void(const std::string& message, bool isSent)>;

    // ============================================================
    // Timing parameters
    // ============================================================
    struct TimingParams
    {
        int responseTimeoutMs;      // Response timeout (ms)
        int interByteTimeoutMs;      // Inter-byte timeout for USB-UART (ms)
        int maxRetries;              // Max retries on error
        int interCommandDelayMs;     // Delay between commands in transaction (LM->RS) (ms)
        int idlePollDelayMs;         // Idle state polling interval (ms)
        int linkLostPollMs;          // Polling interval on connection loss (ms)
        int postEndDelayMs;          // Delay after transaction completion (ms)
        int errorThreshold;          // Error threshold for connection loss state
        bool forceBufferClear;       // Force buffer clear before sending

        static TimingParams Default()
        {
            return TimingParams{
                80,     // responseTimeoutMs - enough for USB-UART
                20,     // interByteTimeoutMs - increased from 3ms to 20ms for USB-UART
                3,      // maxRetries - decreased from 5 to 3
                10,     // interCommandDelayMs - minimal delay between LM and RS in transaction
                450,    // idlePollDelayMs - SR interval in idle (matches reference ~500ms)
                350,    // linkLostPollMs - interval on connection loss
                800,    // postEndDelayMs - delay after NO command
                6,      // errorThreshold
                false   // forceBufferClear
            };
        }
    };

    // ============================================================
    // Maximum frame size per protocol (section 6.5)
    // ============================================================
    static constexpr int MAX_FRAME_SIZE = 27; // STX(1) + CH(1) + ID(1) + CMD(1) + DATA(22) + CRC(1)

    class DispenserController
    {
    public:
        DispenserController();
        ~DispenserController();

        DispenserController(const DispenserController&) = delete;
        DispenserController& operator=(const DispenserController&) = delete;

        // --- Connection ---
        bool Connect(const std::string& portName, const std::string& slaveAddress = "01");
        void Disconnect();
        bool IsConnected() const;

        // --- Control (non-blocking - queues command) ---
        void QueueStop();
        void QueueVolumePreset(double liters, int pricePerLiter);
        void QueueMoneyPreset(int money, int pricePerLiter);
        void QueueEndTransaction();

        // --- Data (from FSM - single source of truth) ---
        Protocol::DispenserState GetCurrentState() const;
        int GetCurrentNozzle() const;
        double GetCurrentLiters() const;
        double GetCurrentMoney() const;
        double GetTotalCounter() const;
        bool IsTransactionDataReady() const;

        // --- Error statistics (separate, section 6.6) ---
        int GetNoResponseCount() const;
        int GetCrcErrorCount() const;
        int GetErrorCount() const; // sum for compatibility

        // --- Callbacks ---
        void SetStatusCallback(StatusCallback cb) { m_onStatusChange = cb; }
        void SetFuelDataCallback(FuelDataCallback cb) { m_onFuelData = cb; }
        void SetTransactionCompleteCallback(TransactionCompleteCallback cb) { m_onTransactionComplete = cb; }
        void SetErrorCallback(ErrorCallback cb) { m_onError = cb; }
        void SetLogCallback(LogCallback cb) { m_onLog = cb; }

        // --- Timing parameter management ---
        TimingParams GetTimingParams() const;
        void SetTimingParams(const TimingParams& params);

    private:
        Protocol::GasKitProtocol m_protocol;
        SerialPort m_serialPort;
        DispenserFSM m_fsm;  // FSM - single source of truth

        // Dispense data (updated from LM/RS/TU)
        std::atomic<double> m_currentLiters;
        std::atomic<double> m_currentMoney;
        std::atomic<double> m_totalCounter;
        std::atomic<bool> m_transactionDataReady;

        std::thread m_pollingThread;
        std::atomic<bool> m_isRunning;
        std::mutex m_protocolMutex;

        StatusCallback m_onStatusChange;
        FuelDataCallback m_onFuelData;
        TransactionCompleteCallback m_onTransactionComplete;
        ErrorCallback m_onError;
        LogCallback m_onLog;

        // --- Separate error statistics (section 6.6) ---
        std::atomic<int> m_noResponseCount;  // no response / connection lost
        std::atomic<int> m_crcErrorCount;  // bad frame / CRC / resync

        // --- Priority command queue ---
        struct PendingCommand {
            std::vector<uint8_t> frame;
            std::string description;
        };
        std::queue<PendingCommand> m_commandQueue;
        std::mutex m_queueMutex;

        // --- Timing parameters ---
        TimingParams m_timingParams;

        void PollingLoop();

        // Send command with retry
        std::vector<uint8_t> SendWithRetry(const std::vector<uint8_t>& command,
            int maxRetries, int timeoutMs);

        // Process SR response through FSM
        void ProcessStatusAndAct(const std::vector<uint8_t>& response);

        // FSM actions
        void DoFuellingCycle();   // LM + RS
        void DoSendTU();
        void DoSendC0();
        void DoSendNO();
        void DoIdleC0();

        void ExecutePendingCommands();
        void Log(const std::string& message, bool isSent = true);
        std::string FrameToString(const std::vector<uint8_t>& frame);
        void NotifyError(const std::string& message);

        static void ParseAddress(const std::string& addr, uint8_t& hi, uint8_t& lo);
    };

} // namespace FuelMaster

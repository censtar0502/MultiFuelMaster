// ============================================================
// DispenserController.cpp — Implementation (FSM = source of truth)
// ============================================================

#include "pch.h"
#include "DispenserController.h"
#include "Logger.h"
#include "DispenserFSM.h"
#include <chrono>
#include <sstream>
#include <iomanip>
#include <windows.h>

namespace FuelMaster {

    // ============================================================
    // HELPER FUNCTIONS
    // ============================================================

    void DispenserController::ParseAddress(const std::string& addr, uint8_t& hi, uint8_t& lo)
    {
        int a = std::stoi(addr);
        if (a < 1) a = 1;
        if (a > 32) a = 32;
        hi = 0x00;
        lo = static_cast<uint8_t>(a);
    }

    // ============================================================
    // CONSTRUCTOR / DESTRUCTOR
    // ============================================================

    DispenserController::DispenserController()
        : m_protocol(0x00, 0x01),
        m_fsm(),
        m_currentLiters(0.0),
        m_currentMoney(0.0),
        m_totalCounter(0.0),
        m_transactionDataReady(false),
        m_isRunning(false),
        m_noResponseCount(0),
        m_crcErrorCount(0),
        m_timingParams(TimingParams::Default())
    {
    }

    DispenserController::~DispenserController()
    {
        Disconnect();
    }

    // ============================================================
    // CONNECTION
    // ============================================================

    bool DispenserController::Connect(const std::string& portName, const std::string& slaveAddress)
    {
        // Explicit Logger initialization before any FM_LOG calls.
        // AutoInitialize from variadic FM_LOG_INFO in C++/CLI context
        // can cause SEHException, so initialize here.
        try
        {
            if (!Logger::Instance().IsInitialized())
            {
                Logger::Instance().AutoInitialize();
            }
        }
        catch (...) { /* Logger init failed - continue without logs */ }

        FM_LOG_INFO("Connect() START: port=%s addr=%s", portName.c_str(), slaveAddress.c_str());

        // Prevent re-call
        if (m_isRunning.load())
        {
            FM_LOG_WARNING("Connect() called while already running, returning false");
            return false;
        }

        uint8_t hi, lo;
        ParseAddress(slaveAddress, hi, lo);
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            m_protocol.SetAddress(hi, lo);
        }

        if (!m_serialPort.Open(portName, 9600))
        {
            FM_LOG_ERROR("Connect() Open FAILED: %s", m_serialPort.GetLastError().c_str());
            NotifyError("Cannot open COM port: " + portName);
            return false;
        }

        if (!m_serialPort.IsOpen())
        {
            FM_LOG_ERROR("Connect() Port opened but IsOpen() = false");
            return false;
        }

        // Initialize FSM
        m_fsm.Reset();
        m_fsm.SetTransitionCallback([this](Protocol::DispenserState from,
                                           Protocol::DispenserState to) {
            FM_LOG_INFO("FSM transition: %s -> %s",
                DispenserFSM::StateToString(from).c_str(),
                DispenserFSM::StateToString(to).c_str());
        });

        m_isRunning.store(true);
        m_noResponseCount.store(0);
        m_crcErrorCount.store(0);
        m_transactionDataReady.store(false);
        m_currentLiters.store(0.0);
        m_currentMoney.store(0.0);

        m_pollingThread = std::thread(&DispenserController::PollingLoop, this);

        FM_LOG_INFO("Connect() SUCCESS: port=%s open, polling started", portName.c_str());
        Log("Connected to " + portName + " addr=" + slaveAddress, true);
        return true;
    }

    void DispenserController::Disconnect()
    {
        if (!m_isRunning.load())
            return;

        m_isRunning.store(false);

        if (m_pollingThread.joinable())
            m_pollingThread.join();

        // Give time for all port operations to complete
        Sleep(10);

        m_serialPort.Close();
        m_noResponseCount.store(0);
        m_crcErrorCount.store(0);

        Log("Disconnected", true);
    }

    bool DispenserController::IsConnected() const
    {
        return m_serialPort.IsOpen();
    }

    // ============================================================
    // COMMAND QUEUE (public)
    // ============================================================

    void DispenserController::QueueStop()
    {
        std::vector<uint8_t> cmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            cmd = m_protocol.BuildStop();
        }
        {
            std::lock_guard<std::mutex> lock(m_queueMutex);
            m_commandQueue.push({ cmd, "STOP(B)" });
        }
        Log("Queued: Stop", true);
    }

    void DispenserController::QueueVolumePreset(double liters, int pricePerLiter)
    {
        int centiliters = static_cast<int>(liters * 100.0);
        std::vector<uint8_t> cmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            cmd = m_protocol.BuildVolumePreset(1, centiliters, pricePerLiter);
        }
        {
            std::lock_guard<std::mutex> lock(m_queueMutex);
            m_commandQueue.push({ cmd, "VOLUME(V)" });
        }
        Log("Queued: Volume preset", true);
    }

    void DispenserController::QueueMoneyPreset(int money, int pricePerLiter)
    {
        std::vector<uint8_t> cmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            cmd = m_protocol.BuildMoneyPreset(1, money, pricePerLiter);
        }
        {
            std::lock_guard<std::mutex> lock(m_queueMutex);
            m_commandQueue.push({ cmd, "MONEY(M)" });
        }
        Log("Queued: Money preset", true);
    }

    void DispenserController::QueueEndTransaction()
    {
        std::vector<uint8_t> cmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            cmd = m_protocol.BuildEndTransaction();
        }
        {
            std::lock_guard<std::mutex> lock(m_queueMutex);
            m_commandQueue.push({ cmd, "END-TXN(N)" });
        }
        Log("Queued: End transaction", true);
    }

    // ============================================================
    // GETTERS — FSM is the single source of truth
    // ============================================================

    Protocol::DispenserState DispenserController::GetCurrentState() const
    {
        return m_fsm.GetState();
    }

    int DispenserController::GetCurrentNozzle() const
    {
        return m_fsm.GetNozzle();
    }

    double DispenserController::GetCurrentLiters() const { return m_currentLiters.load(); }
    double DispenserController::GetCurrentMoney() const { return m_currentMoney.load(); }
    double DispenserController::GetTotalCounter() const { return m_totalCounter.load(); }
    bool DispenserController::IsTransactionDataReady() const { return m_transactionDataReady.load(); }

    int DispenserController::GetNoResponseCount() const { return m_noResponseCount.load(); }
    int DispenserController::GetCrcErrorCount() const { return m_crcErrorCount.load(); }
    int DispenserController::GetErrorCount() const
    {
        return m_noResponseCount.load(); // UI uses for "no connection"
    }

    // ============================================================
    // COMMAND QUEUE EXECUTION
    // ============================================================

    void DispenserController::ExecutePendingCommands()
    {
        while (true)
        {
            PendingCommand cmd;
            {
                std::lock_guard<std::mutex> lock(m_queueMutex);
                if (m_commandQueue.empty()) return;
                cmd = m_commandQueue.front();
                m_commandQueue.pop();
            }

            Log("EXEC: " + cmd.description, true);
            auto resp = SendWithRetry(cmd.frame, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);

            if (!resp.empty())
            {
                // Response to user command - process as status
                ProcessStatusAndAct(resp);
            }
        }
    }

    // ============================================================
    // PROCESS SR RESPONSE THROUGH FSM
    // ============================================================

    void DispenserController::ProcessStatusAndAct(const std::vector<uint8_t>& response)
    {
        Protocol::StatusResponse s;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            s = m_protocol.ParseStatusResponse(response);
        }
        if (!s.valid) return;

        // Pass to FSM - it determines transition and returns action
        FSMAction action = m_fsm.ProcessHardwareStatus(
            static_cast<int>(s.state), s.nozzle);

        // Notify UI of state change
        if (m_onStatusChange)
            m_onStatusChange(m_fsm.GetState(), s.nozzle);

        // Execute FSM action
        switch (action)
        {
        case FSMAction::PollSR_LM_RS:
            DoFuellingCycle();
            break;
        case FSMAction::SendTU:
            DoSendTU();
            break;
        case FSMAction::SendC0:
            DoSendC0();
            break;
        case FSMAction::SendNO:
            DoSendNO();
            break;
        case FSMAction::IdlePollC0:
            DoIdleC0();
            break;
        case FSMAction::PollSR:
        case FSMAction::None:
        default:
            break;
        }
    }

    // ============================================================
    // FSM ACTIONS
    // ============================================================

    void DispenserController::DoFuellingCycle()
    {
        if (!m_isRunning.load()) return;

        // LM - volume request
        std::vector<uint8_t> lmCmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            lmCmd = m_protocol.BuildVolumeRequest();
        }
        auto lmResp = SendWithRetry(lmCmd, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);
        if (!lmResp.empty())
        {
            Protocol::VolumeResponse v;
            {
                std::lock_guard<std::mutex> lock(m_protocolMutex);
                v = m_protocol.ParseVolumeResponse(lmResp);
            }
            if (v.valid)
            {
                double liters = v.volumeCentiliters / 100.0;
                m_currentLiters.store(liters);
                if (m_onFuelData) m_onFuelData(liters, m_currentMoney.load());
            }
        }

        if (!m_isRunning.load()) return;

        // RS - money request
        std::vector<uint8_t> rsCmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            rsCmd = m_protocol.BuildMoneyRequest();
        }
        auto rsResp = SendWithRetry(rsCmd, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);
        if (!rsResp.empty())
        {
            Protocol::MoneyResponse r;
            {
                std::lock_guard<std::mutex> lock(m_protocolMutex);
                r = m_protocol.ParseMoneyResponse(rsResp);
            }
            if (r.valid)
            {
                double money = static_cast<double>(r.money);
                m_currentMoney.store(money);
                if (m_onFuelData) m_onFuelData(m_currentLiters.load(), money);
            }
        }
    }

    void DispenserController::DoSendTU()
    {
        m_fsm.MarkTUSent();

        std::vector<uint8_t> tuCmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            tuCmd = m_protocol.BuildTransactionRequest();
        }
        auto tuResp = SendWithRetry(tuCmd, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);
        if (!tuResp.empty())
        {
            Protocol::TransactionResponse td;
            {
                std::lock_guard<std::mutex> lock(m_protocolMutex);
                td = m_protocol.ParseTransactionResponse(tuResp);
            }
            if (td.valid)
            {
                double finalLiters = td.volumeCentiliters / 100.0;
                double finalMoney = static_cast<double>(td.money);
                m_currentMoney.store(finalMoney);
                m_currentLiters.store(finalLiters);
                m_transactionDataReady.store(true);

                if (m_onTransactionComplete)
                    m_onTransactionComplete(finalLiters, finalMoney, td.price);
            }
            else
            {
                FM_LOG_WARNING("TU response parse failed, will not retry (one-shot)");
            }
        }
        else
        {
            FM_LOG_WARNING("TU no response, will not retry (one-shot)");
        }
    }

    void DispenserController::DoSendC0()
    {
        m_fsm.MarkC0Sent();

        std::vector<uint8_t> totalCmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            totalCmd = m_protocol.BuildTotalCounterRequest(0);
        }
        auto totalResp = SendWithRetry(totalCmd, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);
        if (!totalResp.empty())
        {
            Protocol::TotalCounterResponse t;
            {
                std::lock_guard<std::mutex> lock(m_protocolMutex);
                t = m_protocol.ParseTotalCounterResponse(totalResp);
            }
            if (t.valid)
            {
                m_totalCounter.store(t.totalCentiliters / 100.0);
            }
            else
            {
                FM_LOG_WARNING("C0 response parse failed (one-shot)");
            }
        }
        else
        {
            FM_LOG_WARNING("C0 no response (one-shot)");
        }
    }

    void DispenserController::DoSendNO()
    {
        m_fsm.MarkNOSent();

        std::vector<uint8_t> noCmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            noCmd = m_protocol.BuildEndTransaction();
        }
        auto noResp = SendWithRetry(noCmd, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);
        if (!noResp.empty())
        {
            ProcessStatusAndAct(noResp);
        }

        Sleep(m_timingParams.postEndDelayMs);
    }

    void DispenserController::DoIdleC0()
    {
        m_fsm.MarkIdleC0Sent();

        std::vector<uint8_t> totalCmd;
        {
            std::lock_guard<std::mutex> lock(m_protocolMutex);
            totalCmd = m_protocol.BuildTotalCounterRequest(0);
        }
        auto totalResp = SendWithRetry(totalCmd, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);
        if (!totalResp.empty())
        {
            Protocol::TotalCounterResponse t;
            {
                std::lock_guard<std::mutex> lock(m_protocolMutex);
                t = m_protocol.ParseTotalCounterResponse(totalResp);
            }
            if (t.valid)
            {
                m_totalCounter.store(t.totalCentiliters / 100.0);
            }
        }
    }

    // ============================================================
    // POLLING LOOP — FSM controls logic
    // ============================================================

    void DispenserController::PollingLoop()
    {
        while (m_isRunning.load())
        {
            // 1) Execute user command queue
            ExecutePendingCommands();

            if (!m_isRunning.load()) break;

            // 2) SR status request
            std::vector<uint8_t> statusCmd;
            {
                std::lock_guard<std::mutex> lock(m_protocolMutex);
                statusCmd = m_protocol.BuildStatusRequest();
            }

            auto statusResp = SendWithRetry(statusCmd, m_timingParams.maxRetries, m_timingParams.responseTimeoutMs);

            if (statusResp.empty())
            {
                // All attempts failed - connection lost
                int noRespCnt = m_noResponseCount.load();
                if (noRespCnt % 10 == 0 && noRespCnt > 0)
                {
                    Log("No response. NoRespCount=" + std::to_string(noRespCnt) +
                        " CrcCount=" + std::to_string(m_crcErrorCount.load()), false);
                }

                Sleep(m_timingParams.linkLostPollMs);
                continue;
            }

            // Response received - reset noResponse counter
            m_noResponseCount.store(0);

            // 3) Process through FSM - it determines action
            ProcessStatusAndAct(statusResp);

            // 4) Adaptive delay between SR requests
            // In transaction state - minimal delay for fast UI update
            // In IDLE state - larger delay to reduce line load (matches reference ~500ms)
            Protocol::DispenserState currentState = m_fsm.GetState();
            if (currentState == Protocol::DispenserState::Idle || 
                currentState == Protocol::DispenserState::Error)
            {
                // IDLE state - slow polling
                Sleep(m_timingParams.idlePollDelayMs);
            }
            else
            {
                // Transaction - fast polling (use interCommandDelayMs as base interval)
                Sleep(m_timingParams.interCommandDelayMs);
            }
        }
    }

    // ============================================================
    // SEND WITH RETRY + separate error statistics
    // ============================================================

    namespace
    {
        static char ExpectedResponseCmd(char requestCmd)
        {
            switch (requestCmd)
            {
            case 'S': case 'L': case 'R': case 'T': case 'C':
                return requestCmd;
            default:
                return 'S';
            }
        }

        static size_t MinFrameLenByCmd(char responseCmd)
        {
            switch (responseCmd)
            {
            case 'S': return 7;
            case 'L': return 15;
            case 'R': return 15;
            case 'T': return 27;
            case 'C': return 16;
            default:  return 5;
            }
        }

        static bool TryExtractExpectedFrame(
            const std::vector<uint8_t>& raw,
            const std::vector<uint8_t>& requestFrame,
            char expectedResponseCmd,
            std::vector<uint8_t>& outFrame,
            Protocol::GasKitProtocol& protocol,
            std::mutex& protocolMutex)
        {
            outFrame.clear();

            if (raw.size() < 5 || requestFrame.size() < 3)
                return false;

            const uint8_t addrHi = requestFrame[1];
            const uint8_t addrLo = requestFrame[2];

            const size_t minLen = MinFrameLenByCmd(expectedResponseCmd);
            if (raw.size() < minLen)
                return false;

            for (size_t start = 0; start + minLen <= raw.size(); start++)
            {
                if (raw[start] != Protocol::STX) continue;
                if (start + 3 >= raw.size()) continue;

                if (raw[start + 1] != addrHi || raw[start + 2] != addrLo) continue;
                if (static_cast<char>(raw[start + 3]) != expectedResponseCmd) continue;

                // Limit search to maximum frame size (section 6.5)
                size_t maxEnd = (std::min)(start + MAX_FRAME_SIZE - 1, raw.size() - 1);
                for (size_t end = start + minLen - 1; end <= maxEnd; end++)
                {
                    std::vector<uint8_t> candidate(raw.begin() + start, raw.begin() + end + 1);

                    bool ok = false;
                    {
                        std::lock_guard<std::mutex> lock(protocolMutex);

                        if (!protocol.ValidateCRC(candidate))
                        {
                            ok = false;
                        }
                        else
                        {
                            switch (expectedResponseCmd)
                            {
                            case 'S': ok = protocol.ParseStatusResponse(candidate).valid; break;
                            case 'L': ok = protocol.ParseVolumeResponse(candidate).valid; break;
                            case 'R': ok = protocol.ParseMoneyResponse(candidate).valid; break;
                            case 'T': ok = protocol.ParseTransactionResponse(candidate).valid; break;
                            case 'C': ok = protocol.ParseTotalCounterResponse(candidate).valid; break;
                            default:  ok = true; break;
                            }
                        }
                    }

                    if (ok)
                    {
                        outFrame = std::move(candidate);
                        return true;
                    }
                }
            }

            return false;
        }
    } // anonymous namespace

    std::vector<uint8_t> DispenserController::SendWithRetry(
        const std::vector<uint8_t>& command, int maxRetries, int timeoutMs)
    {
        // Backoff delay between retry attempts (ms)
        // Gives USB-UART driver time to "deliver" remaining bytes
        const int retryBackoffMs = 150;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            if (!m_isRunning.load()) return {};  // Don't increment noResponse on shutdown

            Log("TX: " + FrameToString(command), true);

            auto response = m_serialPort.SendAndReceive(command, timeoutMs,
                m_timingParams.interByteTimeoutMs, m_timingParams.forceBufferClear);

            if (response.empty())
            {
                // Backoff before next attempt
                if (attempt < maxRetries - 1)
                {
                    Log("RETRY " + std::to_string(attempt + 1) + "/" +
                        std::to_string(maxRetries) + " (no response) - backoff " + std::to_string(retryBackoffMs) + "ms", false);
                    Sleep(retryBackoffMs);
                }
                continue;
            }

            // Check frame size exceeds maximum (section 6.5)
            if (response.size() > MAX_FRAME_SIZE)
            {
                FM_LOG_WARNING("Frame exceeds MAX_FRAME_SIZE(%d): got %zu bytes — possible frame merge",
                    MAX_FRAME_SIZE, response.size());
            }

            Log("RX(raw): " + FrameToString(response), false);

            bool crcValid;
            {
                std::lock_guard<std::mutex> lock(m_protocolMutex);
                crcValid = m_protocol.ValidateCRC(response);
            }

            if (crcValid && response.size() <= MAX_FRAME_SIZE)
            {
                Sleep(m_timingParams.interCommandDelayMs);
                return response;
            }

            // CRC mismatch or frame too long - resync (section 6.4)
            const char reqCmd = (command.size() >= 4) ? static_cast<char>(command[3]) : '?';
            const char expectedCmd = ExpectedResponseCmd(reqCmd);

            std::vector<uint8_t> fixed;
            if (TryExtractExpectedFrame(response, command, expectedCmd, fixed, m_protocol, m_protocolMutex))
            {
                Log("RX(resync): " + FrameToString(fixed), false);
                // CRC error (resync required) - increment crcError
                m_crcErrorCount.fetch_add(1);

                Sleep(m_timingParams.interCommandDelayMs);
                return fixed;
            }

            // Could not extract valid frame - CRC error
            Log("CRC ERROR! (no valid frame found) - backoff " + std::to_string(retryBackoffMs) + "ms", false);
            m_crcErrorCount.fetch_add(1);

            // Backoff before next attempt on CRC error
            if (attempt < maxRetries - 1)
            {
                Sleep(retryBackoffMs);
            }
        }

        // All attempts exhausted - one increment of noResponse for entire SR cycle
        m_noResponseCount.fetch_add(1);
        return {};
    }

    // ============================================================
    // LOGGING / ERRORS
    // ============================================================

    void DispenserController::Log(const std::string& message, bool isSent)
    {
        if (m_onLog)
            m_onLog(message, isSent);

        if (Logger::Instance().IsInitialized())
        {
            if (isSent)
                Logger::Instance().Trace("[TX] " + message);
            else
                Logger::Instance().Trace("[RX] " + message);
        }
    }

    void DispenserController::NotifyError(const std::string& message)
    {
        Log("ERROR: " + message, false);

        if (Logger::Instance().IsInitialized())
        {
            Logger::Instance().Error("[ERROR] " + message);
        }

        if (m_onError)
            m_onError(message);
    }

    std::string DispenserController::FrameToString(const std::vector<uint8_t>& frame)
    {
        std::ostringstream oss;
        for (size_t i = 0; i < frame.size(); i++)
        {
            oss << std::uppercase << std::hex << std::setw(2) << std::setfill('0')
                << static_cast<int>(frame[i]) << " ";
        }
        return oss.str();
    }

    // ============================================================
    // TIMING PARAMETER MANAGEMENT
    // ============================================================

    TimingParams DispenserController::GetTimingParams() const
    {
        return m_timingParams;
    }

    void DispenserController::SetTimingParams(const TimingParams& params)
    {
        m_timingParams = params;

        // Log only if Logger is already initialized.
        // On first call (before Connect) Logger is not ready -
        // AutoInitialize from C++/CLI context causes SEHException.
        if (Logger::Instance().IsInitialized())
        {
            FM_LOG_INFO("Timing params updated: responseTimeout=%dms, interByte=%dms, retries=%d, "
                       "interCmdDelay=%dms, bufferClear=%s",
                       params.responseTimeoutMs, params.interByteTimeoutMs, params.maxRetries,
                       params.interCommandDelayMs, params.forceBufferClear ? "ON" : "OFF");
        }
    }

} // namespace FuelMaster

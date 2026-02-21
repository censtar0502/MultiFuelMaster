// ============================================================
// DispenserFSM.h — FSM as the single source of truth
// ============================================================
// State is determined by SR responses from the device.
// FSM manages one-time latches and transition logic.
// ============================================================

#pragma once

#include "GasKitProtocol.h"
#include "Logger.h"
#include <mutex>
#include <atomic>
#include <string>
#include <functional>

namespace FuelMaster
{

// ============================================================
// Action that controller must perform after FSM update
// ============================================================

enum class FSMAction
{
    None,               // Continue normal polling
    PollSR,             // Continue polling SR
    PollSR_LM_RS,       // Dispense cycle: SR -> LM -> RS
    SendTU,             // Send TU (once)
    SendC0,             // Send C0 (once after TU)
    SendNO,             // Send NO (once)
    IdlePollC0          // Periodic C0 in idle mode
};

// ============================================================
// DispenserFSM class — source of truth
// ============================================================

class DispenserFSM
{
public:
    using State = Protocol::DispenserState;

    DispenserFSM();

    // --- Main method: process status from device ---
    // Returns action that controller must perform
    FSMAction ProcessHardwareStatus(int hwState, int nozzle);

    // --- Get current state (thread-safe) ---
    State GetState() const;
    int GetNozzle() const;

    // --- Latch management ---
    void MarkTUSent();
    void MarkC0Sent();
    void MarkNOSent();

    bool IsTUNeeded() const;
    bool IsC0Needed() const;
    bool IsNONeeded() const;

    // --- Reset ---
    void Reset();

    // --- Idle C0 polling ---
    bool IsIdleC0Due() const;
    void MarkIdleC0Sent();

    // --- Callback for transition logging ---
    using TransitionCallback = std::function<void(State from, State to)>;
    void SetTransitionCallback(TransitionCallback cb) { m_onTransition = cb; }

    // --- Utilities ---
    static std::string StateToString(State state);

private:
    mutable std::mutex m_mutex;
    std::atomic<State> m_currentState;
    std::atomic<int> m_currentNozzle;

    // One-time latches for S81 -> S90 -> S10
    std::atomic<bool> m_finalRequested;    // TU sent
    std::atomic<bool> m_totalsRequested;   // C0 sent (after TU)
    std::atomic<bool> m_noSent;            // NO sent

    // Periodic C0 in Idle
    std::atomic<int> m_idlePollCounter;
    static constexpr int IDLE_C0_INTERVAL = 20; // every ~20 SR cycles

    TransitionCallback m_onTransition;

    void TransitionTo(State newState);
    void ResetLatches();
};

} // namespace FuelMaster

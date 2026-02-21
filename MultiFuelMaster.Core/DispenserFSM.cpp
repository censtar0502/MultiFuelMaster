// ============================================================
// DispenserFSM.cpp — FSM as the single source of truth
// ============================================================

#include "pch.h"
#include "DispenserFSM.h"

namespace FuelMaster
{

// ============================================================
// Constructor
// ============================================================

DispenserFSM::DispenserFSM()
    : m_currentState(State::Error)
    , m_currentNozzle(0)
    , m_finalRequested(false)
    , m_totalsRequested(false)
    , m_noSent(false)
    , m_idlePollCounter(0)
{
}

// ============================================================
// Get state
// ============================================================

DispenserFSM::State DispenserFSM::GetState() const
{
    return m_currentState.load();
}

int DispenserFSM::GetNozzle() const
{
    return m_currentNozzle.load();
}

// ============================================================
// Main method: process hardware status
// Called after each successful SR response
// Returns action for controller
// ============================================================

FSMAction DispenserFSM::ProcessHardwareStatus(int hwState, int nozzle)
{
    std::lock_guard<std::mutex> lock(m_mutex);

    State oldState = m_currentState.load();
    State newState = static_cast<State>(hwState);

    // Update nozzle
    m_currentNozzle.store(nozzle);

    // State transition
    if (oldState != newState)
    {
        // On exit from Stopped - reset TU/C0 latches
        if (oldState == State::Stopped && newState != State::Stopped)
        {
            // Don't reset - they already did their job
        }

        // On exit from EndOfTransaction - reset NO
        if (oldState == State::EndOfTransaction && newState == State::Idle)
        {
            ResetLatches();
        }

        // On new transaction entry (from Idle to Authorized) - reset everything
        if (oldState == State::Idle &&
            (newState == State::Authorized || newState == State::Calling))
        {
            ResetLatches();
            m_idlePollCounter.store(0);
        }

        // Log transition
        FM_LOG_INFO("[FSM] %s -> %s (nozzle=%d)",
            StateToString(oldState).c_str(),
            StateToString(newState).c_str(),
            nozzle);

        m_currentState.store(newState);

        if (m_onTransition)
            m_onTransition(oldState, newState);
    }

    // Determine action based on current state
    switch (newState)
    {
    case State::Idle:
    {
        // Periodic C0 in idle mode
        int cnt = m_idlePollCounter.fetch_add(1);
        if (cnt >= IDLE_C0_INTERVAL)
        {
            m_idlePollCounter.store(0);
            return FSMAction::IdlePollC0;
        }
        return FSMAction::PollSR;
    }

    case State::Calling:
        // S21Q - nozzle lifted without transaction
        return FSMAction::PollSR;

    case State::Authorized:
    case State::Started:
        // Waiting for dispense start
        return FSMAction::PollSR;

    case State::Fuelling:
    case State::SuspendedFuelling:
    case State::SuspendedStarted:
        // Dispense cycle: SR -> LM -> RS
        return FSMAction::PollSR_LM_RS;

    case State::Stopped:
    {
        // S81: TU (once), then C0 (once), then only SR
        if (!m_finalRequested.load())
            return FSMAction::SendTU;
        if (!m_totalsRequested.load())
            return FSMAction::SendC0;
        return FSMAction::PollSR;
    }

    case State::EndOfTransaction:
    {
        // S90: NO (once), then SR until S10
        if (!m_noSent.load())
            return FSMAction::SendNO;
        return FSMAction::PollSR;
    }

    case State::Error:
    default:
        return FSMAction::PollSR;
    }
}

// ============================================================
// Latch management
// ============================================================

void DispenserFSM::MarkTUSent()
{
    m_finalRequested.store(true);
}

void DispenserFSM::MarkC0Sent()
{
    m_totalsRequested.store(true);
}

void DispenserFSM::MarkNOSent()
{
    m_noSent.store(true);
}

bool DispenserFSM::IsTUNeeded() const
{
    return m_currentState.load() == State::Stopped && !m_finalRequested.load();
}

bool DispenserFSM::IsC0Needed() const
{
    return m_currentState.load() == State::Stopped &&
           m_finalRequested.load() && !m_totalsRequested.load();
}

bool DispenserFSM::IsNONeeded() const
{
    return m_currentState.load() == State::EndOfTransaction && !m_noSent.load();
}

// ============================================================
// Idle C0 polling
// ============================================================

bool DispenserFSM::IsIdleC0Due() const
{
    return m_currentState.load() == State::Idle &&
           m_idlePollCounter.load() >= IDLE_C0_INTERVAL;
}

void DispenserFSM::MarkIdleC0Sent()
{
    m_idlePollCounter.store(0);
}

// ============================================================
// Reset
// ============================================================

void DispenserFSM::Reset()
{
    std::lock_guard<std::mutex> lock(m_mutex);
    m_currentState.store(State::Idle);
    m_currentNozzle.store(0);
    ResetLatches();
    m_idlePollCounter.store(0);
}

void DispenserFSM::ResetLatches()
{
    m_finalRequested.store(false);
    m_totalsRequested.store(false);
    m_noSent.store(false);
}

// ============================================================
// Utilities
// ============================================================

std::string DispenserFSM::StateToString(State state)
{
    switch (state)
    {
    case State::Error:             return "Error";
    case State::Idle:              return "Idle(1)";
    case State::Calling:           return "Calling(2)";
    case State::Authorized:        return "Authorized(3)";
    case State::Started:           return "Started(4)";
    case State::SuspendedStarted:  return "SuspendedStarted(5)";
    case State::Fuelling:          return "Fuelling(6)";
    case State::SuspendedFuelling: return "SuspendedFuelling(7)";
    case State::Stopped:           return "Stopped(8)";
    case State::EndOfTransaction: return "EndOfTransaction(9)";
    default:                       return "Unknown";
    }
}

} // namespace FuelMaster

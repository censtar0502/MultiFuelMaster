// ============================================================
// MultiFuelMaster.Interop/DispenserBridge.cpp
// C++/CLI Bridge Implementation
// ============================================================

#include "pch.h"
#include "DispenserBridge.h"
#include <msclr/marshal_cppstd.h>

namespace FuelMasterInterop {

    DispenserBridge::DispenserBridge()
        : m_controller(new FuelMaster::DispenserController()),
        m_disposed(false)
    {
    }

    DispenserBridge::~DispenserBridge()
    {
        Cleanup();
    }

    DispenserBridge::!DispenserBridge()
    {
        Cleanup();
    }

    void DispenserBridge::Cleanup()
    {
        if (m_disposed) return;
        m_disposed = true;

        if (m_controller)
        {
            m_controller->Disconnect();
            delete m_controller;
            m_controller = nullptr;
        }
    }

    bool DispenserBridge::Connect(String^ portName, String^ slaveAddress)
    {
        if (m_disposed || !m_controller) return false;

        std::string p = msclr::interop::marshal_as<std::string>(portName);
        std::string a = msclr::interop::marshal_as<std::string>(slaveAddress);

        return m_controller->Connect(p, a);
    }

    void DispenserBridge::Disconnect()
    {
        if (m_disposed || !m_controller) return;
        m_controller->Disconnect();
    }

    bool DispenserBridge::IsConnected::get()
    {
        if (m_disposed || !m_controller) return false;
        return m_controller->IsConnected();
    }

    // --- Commands (queue) ---

    void DispenserBridge::QueueVolumePreset(double liters, int pricePerLiter)
    {
        if (m_disposed || !m_controller) return;
        m_controller->QueueVolumePreset(liters, pricePerLiter);
    }

    void DispenserBridge::QueueMoneyPreset(int money, int pricePerLiter)
    {
        if (m_disposed || !m_controller) return;
        m_controller->QueueMoneyPreset(money, pricePerLiter);
    }

    void DispenserBridge::QueueStop()
    {
        if (m_disposed || !m_controller) return;
        m_controller->QueueStop();
    }

    void DispenserBridge::QueueEndTransaction()
    {
        if (m_disposed || !m_controller) return;
        m_controller->QueueEndTransaction();
    }

    // --- Properties (atomic read — instant) ---

    ManagedDispenserState DispenserBridge::CurrentState::get()
    {
        if (m_disposed || !m_controller) return ManagedDispenserState::Error;
        return static_cast<ManagedDispenserState>(static_cast<int>(m_controller->GetCurrentState()));
    }

    double DispenserBridge::CurrentLiters::get()
    {
        if (m_disposed || !m_controller) return 0.0;
        return m_controller->GetCurrentLiters();
    }

    double DispenserBridge::CurrentMoney::get()
    {
        if (m_disposed || !m_controller) return 0.0;
        return m_controller->GetCurrentMoney();
    }

    double DispenserBridge::TotalCounter::get()
    {
        if (m_disposed || !m_controller) return 0.0;
        return m_controller->GetTotalCounter();
    }

    bool DispenserBridge::IsTransactionDataReady::get()
    {
        if (m_disposed || !m_controller) return false;
        return m_controller->IsTransactionDataReady();
    }

    int DispenserBridge::ErrorCount::get()
    {
        if (m_disposed || !m_controller) return 0;
        return m_controller->GetErrorCount();
    }

    // --- Timing Parameters ---

    void DispenserBridge::SetTimingParams(int responseTimeoutMs, int interByteTimeoutMs, int maxRetries,
        int interCommandDelayMs, int idlePollDelayMs, int linkLostPollMs, int postEndDelayMs,
        int errorThreshold, bool forceBufferClear)
    {
        if (m_disposed || !m_controller) return;

        FuelMaster::TimingParams params;
        params.responseTimeoutMs = responseTimeoutMs;
        params.interByteTimeoutMs = interByteTimeoutMs;
        params.maxRetries = maxRetries;
        params.interCommandDelayMs = interCommandDelayMs;
        params.idlePollDelayMs = idlePollDelayMs;
        params.linkLostPollMs = linkLostPollMs;
        params.postEndDelayMs = postEndDelayMs;
        params.errorThreshold = errorThreshold;
        params.forceBufferClear = forceBufferClear;

        m_controller->SetTimingParams(params);
    }

    void DispenserBridge::GetTimingParams([Runtime::InteropServices::Out] int% responseTimeoutMs,
        [Runtime::InteropServices::Out] int% interByteTimeoutMs,
        [Runtime::InteropServices::Out] int% maxRetries,
        [Runtime::InteropServices::Out] int% interCommandDelayMs,
        [Runtime::InteropServices::Out] int% idlePollDelayMs,
        [Runtime::InteropServices::Out] int% linkLostPollMs,
        [Runtime::InteropServices::Out] int% postEndDelayMs,
        [Runtime::InteropServices::Out] int% errorThreshold,
        [Runtime::InteropServices::Out] bool% forceBufferClear)
    {
        if (m_disposed || !m_controller)
        {
            responseTimeoutMs = 80;
            interByteTimeoutMs = 20;
            maxRetries = 3;
            interCommandDelayMs = 10;
            idlePollDelayMs = 450;
            linkLostPollMs = 350;
            postEndDelayMs = 800;
            errorThreshold = 6;
            forceBufferClear = false;
            return;
        }

        auto params = m_controller->GetTimingParams();
        responseTimeoutMs = params.responseTimeoutMs;
        interByteTimeoutMs = params.interByteTimeoutMs;
        maxRetries = params.maxRetries;
        interCommandDelayMs = params.interCommandDelayMs;
        idlePollDelayMs = params.idlePollDelayMs;
        linkLostPollMs = params.linkLostPollMs;
        postEndDelayMs = params.postEndDelayMs;
        errorThreshold = params.errorThreshold;
        forceBufferClear = params.forceBufferClear;
    }
}

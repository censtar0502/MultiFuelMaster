// ============================================================
// MultiFuelMaster.Interop/DispenserBridge.h
// C++/CLI Bridge between Core and UI
// ============================================================

#pragma once

#include "../MultiFuelMaster.Core/DispenserController.h"
#include "../MultiFuelMaster.Core/GasKitProtocol.h"

using namespace System;

namespace FuelMasterInterop {

    public enum class ManagedDispenserState
    {
        Error = 0,
        Idle = 1,
        Calling = 2,
        Authorized = 3,
        Started = 4,
        SuspendedStarted = 5,
        Fuelling = 6,
        SuspendedFuelling = 7,
        Stopped = 8,
        EndOfTransaction = 9
    };

    public ref class DispenserBridge
    {
    public:
        DispenserBridge();
        ~DispenserBridge();
        !DispenserBridge();

        bool Connect(String^ portName, String^ slaveAddress);
        void Disconnect();
        property bool IsConnected{ bool get(); }

        // Non-blocking commands — queue for polling thread
        void QueueVolumePreset(double liters, int pricePerLiter);
        void QueueMoneyPreset(int money, int pricePerLiter);
        void QueueStop();
        void QueueEndTransaction();

        property ManagedDispenserState CurrentState{ ManagedDispenserState get(); }
        property double CurrentLiters{ double get(); }
        property double CurrentMoney{ double get(); }
        property double TotalCounter{ double get(); }
        property bool IsTransactionDataReady{ bool get(); }
        property int ErrorCount{ int get(); }

        // Timing parameters
        void SetTimingParams(int responseTimeoutMs, int interByteTimeoutMs, int maxRetries,
            int interCommandDelayMs, int idlePollDelayMs, int linkLostPollMs, int postEndDelayMs,
            int errorThreshold, bool forceBufferClear);
        void GetTimingParams([Runtime::InteropServices::Out] int% responseTimeoutMs,
            [Runtime::InteropServices::Out] int% interByteTimeoutMs,
            [Runtime::InteropServices::Out] int% maxRetries,
            [Runtime::InteropServices::Out] int% interCommandDelayMs,
            [Runtime::InteropServices::Out] int% idlePollDelayMs,
            [Runtime::InteropServices::Out] int% linkLostPollMs,
            [Runtime::InteropServices::Out] int% postEndDelayMs,
            [Runtime::InteropServices::Out] int% errorThreshold,
            [Runtime::InteropServices::Out] bool% forceBufferClear);

    private:
        FuelMaster::DispenserController* m_controller;
        bool m_disposed;
        void Cleanup();
    };
}

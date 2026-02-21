// ============================================================
// MultiFuelMasterCore.h — Export header for DLL
// ============================================================
// This file defines which classes and functions are available
// to other projects (Interop and UI) from our C++ DLL.
// ============================================================

#pragma once

// Macro for exporting/importing functions from DLL
#ifdef FUELMASTERCORE_EXPORTS
    #define FUELMASTER_API __declspec(dllexport)
#else
    #define FUELMASTER_API __declspec(dllimport)
#endif

// Include all public headers
#include "GasKitProtocol.h"
#include "SerialPort.h"
#include "DispenserController.h"

// ============================================================
// ConfigurationConstants.h — Protocol configuration constants
// ============================================================
// VALUES MOVED FROM ENGINEERING MENU
// Changes in runtime through UI ARE NO LONGER POSSIBLE
// ============================================================

#pragma once

#include <cstdint>

namespace FuelMaster {
namespace Core {
namespace Config {

    // ============================================================
    // PROTOCOL TIMINGS (in milliseconds)
    // ============================================================
    
    /// Response first byte timeout from dispenser
    constexpr int RESPONSE_TIMEOUT_MS = 80;

    /// Inter-byte timeout (end of frame)
    /// Increased from 3 to 20ms for USB-UART adapters
    constexpr int INTER_BYTE_TIMEOUT_MS = 20;

    /// Delay between LM and RS commands in transaction
    /// Reduced to 10ms for fast update
    constexpr int INTER_COMMAND_DELAY_MS = 10;

    /// SR polling interval in idle mode
    /// Matches reference program (~500ms)
    constexpr int IDLE_POLL_DELAY_MS = 450;

    /// Polling interval on connection loss
    constexpr int LINK_LOST_POLL_MS = 350;

    /// Delay after transaction completion
    constexpr int POST_END_DELAY_MS = 800;

    // ============================================================
    // RETRIES AND THRESHOLDS
    // ============================================================

    /// Number of command attempts on error
    /// Reduced from 5 to 3 for faster response
    constexpr int MAX_RETRIES = 3;

    /// Error threshold for "connection lost" state
    constexpr int ERROR_THRESHOLD = 6;

    // ============================================================
    // BUFFER SETTINGS
    // ============================================================

    /// Force Rx buffer clear before sending command
    constexpr bool FORCE_BUFFER_CLEAR = false;

    // ============================================================
    // DEFAULT PRICE AND FUEL
    // ============================================================

    /// Default price per liter (in sums)
    constexpr double DEFAULT_PRICE_PER_LITER = 2233.0;

    /// Default fuel type
    constexpr const char* DEFAULT_FUEL_TYPE = "AI-95";

    /// Default nozzle number
    constexpr int DEFAULT_NOZZLE = 1;

    // ============================================================
    // DEFAULT DISPENSER ADDRESS
    // ============================================================

    constexpr uint8_t DEFAULT_SLAVE_ADDR_HI = 0x00;
    constexpr uint8_t DEFAULT_SLAVE_ADDR_LO = 0x01;

} // namespace Config
} // namespace Core
} // namespace FuelMaster

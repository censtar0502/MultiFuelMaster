// ============================================================
// GasKitProtocol.h — GasKitLink protocol: frame building and parsing
// ============================================================
// FIXED: Slave address — 2 binary bytes (0x00, 0x01),
// NOT ASCII characters ('0', '1')
// ============================================================

#pragma once

#include <cstdint>
#include <vector>
#include <string>

namespace FuelMaster {
namespace Protocol {

    constexpr uint8_t STX = 0x02;

    /// Default Slave address: 2 binary bytes
    /// Dispenser #1 = {0x00, 0x01}
    constexpr uint8_t DEFAULT_SLAVE_ADDR_HI = 0x00;
    constexpr uint8_t DEFAULT_SLAVE_ADDR_LO = 0x01;

    constexpr char DEFAULT_NOZZLE = '1';

    // ============================================================
    // DISPENSER STATES
    // ============================================================

    enum class DispenserState : int
    {
        Error             = 0,
        Idle              = 1,
        Calling           = 2,
        Authorized        = 3,
        Started           = 4,
        SuspendedStarted  = 5,
        Fuelling          = 6,
        SuspendedFuelling = 7,
        Stopped           = 8,
        EndOfTransaction  = 9
    };

    // ============================================================
    // RESPONSE STRUCTURES
    // ============================================================

    struct StatusResponse
    {
        DispenserState state;
        int nozzle;
        bool valid;
    };

    struct VolumeResponse
    {
        int nozzle;
        char transactionId;
        DispenserState state;
        int volumeCentiliters;
        bool valid;
    };

    struct MoneyResponse
    {
        int nozzle;
        char transactionId;
        DispenserState state;
        int money;
        bool valid;
    };

    struct TransactionResponse
    {
        int nozzle;
        char transactionId;
        DispenserState state;
        int money;
        int volumeCentiliters;
        int price;
        bool valid;
    };

    struct TotalCounterResponse
    {
        int nozzle;
        long long totalCentiliters;
        bool valid;
    };

    // ============================================================
    // GasKitProtocol CLASS
    // ============================================================

    class GasKitProtocol
    {
    public:
        /// Constructor: addrHi, addrLo — 2 binary address bytes
        /// For dispenser #1: addrHi=0x00, addrLo=0x01
        GasKitProtocol(uint8_t addrHi = DEFAULT_SLAVE_ADDR_HI,
                       uint8_t addrLo = DEFAULT_SLAVE_ADDR_LO);

        /// Change slave address (after construction)
        void SetAddress(uint8_t addrHi, uint8_t addrLo)
        {
            m_addrHi = addrHi;
            m_addrLo = addrLo;
        }

        // --- Command building ---
        std::vector<uint8_t> BuildStatusRequest();
        std::vector<uint8_t> BuildVolumePreset(int nozzle, int volumeCentiliters, int price);
        std::vector<uint8_t> BuildMoneyPreset(int nozzle, int money, int price);
        std::vector<uint8_t> BuildStop();
        std::vector<uint8_t> BuildResume();
        std::vector<uint8_t> BuildVolumeRequest();
        std::vector<uint8_t> BuildMoneyRequest();
        std::vector<uint8_t> BuildTransactionRequest();
        std::vector<uint8_t> BuildTotalCounterRequest(int nozzle);
        std::vector<uint8_t> BuildEndTransaction();

        // --- Response parsing ---
        StatusResponse ParseStatusResponse(const std::vector<uint8_t>& frame);
        VolumeResponse ParseVolumeResponse(const std::vector<uint8_t>& frame);
MoneyResponse ParseMoneyResponse(const std::vector<uint8_t>& frame);
        TransactionResponse ParseTransactionResponse(const std::vector<uint8_t>& frame);
        TotalCounterResponse ParseTotalCounterResponse(const std::vector<uint8_t>& frame);

        // --- Utilities ---
        bool ValidateCRC(const std::vector<uint8_t>& frame);
        std::string ExtractPayload(const std::vector<uint8_t>& frame);

    private:
        uint8_t m_addrHi;   // Address high byte (0x00)
        uint8_t m_addrLo;   // Address low byte (0x01)

        std::vector<uint8_t> BuildFrame(const std::string& payload);
        uint8_t CalculateCRC(const std::vector<uint8_t>& data, size_t from, size_t to);
        std::string FormatNumber(int value, int width);
    };

} // namespace Protocol
} // namespace FuelMaster

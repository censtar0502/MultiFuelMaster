// ============================================================
// GasKitProtocol.cpp — Implementation (FIXED: binary address)
// ============================================================

#include "pch.h"
#include "GasKitProtocol.h"
#include <sstream>
#include <iomanip>

namespace FuelMaster {
namespace Protocol {

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    GasKitProtocol::GasKitProtocol(uint8_t addrHi, uint8_t addrLo)
        : m_addrHi(addrHi)
        , m_addrLo(addrLo)
    {
    }

    // ============================================================
    // COMMAND BUILDING
    // ============================================================

    std::vector<uint8_t> GasKitProtocol::BuildStatusRequest()
    {
        return BuildFrame("S");
    }

    std::vector<uint8_t> GasKitProtocol::BuildVolumePreset(int nozzle, int volumeCentiliters, int price)
    {
        std::string payload = "V";
        payload += std::to_string(nozzle);
        payload += ";";
        payload += FormatNumber(volumeCentiliters, 6);
        payload += ";";
        payload += FormatNumber(price, 4);
        return BuildFrame(payload);
    }

    std::vector<uint8_t> GasKitProtocol::BuildMoneyPreset(int nozzle, int money, int price)
    {
        std::string payload = "M";
        payload += std::to_string(nozzle);
        payload += ";";
        payload += FormatNumber(money, 6);
        payload += ";";
        payload += FormatNumber(price, 4);
        return BuildFrame(payload);
    }

    std::vector<uint8_t> GasKitProtocol::BuildStop()
    {
        return BuildFrame("B");
    }

    std::vector<uint8_t> GasKitProtocol::BuildResume()
    {
        return BuildFrame("G");
    }

    std::vector<uint8_t> GasKitProtocol::BuildVolumeRequest()
    {
        return BuildFrame("L");
    }

    std::vector<uint8_t> GasKitProtocol::BuildMoneyRequest()
    {
        return BuildFrame("R");
    }

    std::vector<uint8_t> GasKitProtocol::BuildTransactionRequest()
    {
        return BuildFrame("T");
    }

    std::vector<uint8_t> GasKitProtocol::BuildTotalCounterRequest(int nozzle)
    {
        std::string payload = "C";
        payload += std::to_string(nozzle);
        return BuildFrame(payload);
    }

    std::vector<uint8_t> GasKitProtocol::BuildEndTransaction()
    {
        return BuildFrame("N");
    }

    // ============================================================
    // RESPONSE PARSING
    // ============================================================

    StatusResponse GasKitProtocol::ParseStatusResponse(const std::vector<uint8_t>& frame)
    {
        StatusResponse result = {};
        result.valid = false;

        if (!ValidateCRC(frame)) return result;

        std::string payload = ExtractPayload(frame);

        // Format: Ssg (3 characters)
        if (payload.size() < 3 || payload[0] != 'S') return result;

        int state = payload[1] - '0';
        int nozzle = payload[2] - '0';

        if (state < 0 || state > 9) return result;
        if (nozzle < 0 || nozzle > 6) return result;

        result.state = static_cast<DispenserState>(state);
        result.nozzle = nozzle;
        result.valid = true;
        return result;
    }

    VolumeResponse GasKitProtocol::ParseVolumeResponse(const std::vector<uint8_t>& frame)
    {
        VolumeResponse result = {};
        result.valid = false;

        if (!ValidateCRC(frame)) return result;

        std::string payload = ExtractPayload(frame);

        // Format: Lgis;llllll (11 characters)
        if (payload.size() < 11 || payload[0] != 'L') return result;

        result.nozzle = payload[1] - '0';
        result.transactionId = payload[2];
        int state = payload[3] - '0';
        if (state < 0 || state > 9) return result;
        result.state = static_cast<DispenserState>(state);

        if (payload[4] != ';') return result;

        try {
            result.volumeCentiliters = std::stoi(payload.substr(5, 6));
        }
        catch (...) { return result; }

        result.valid = true;
        return result;
    }

    MoneyResponse GasKitProtocol::ParseMoneyResponse(const std::vector<uint8_t>& frame)
    {
        MoneyResponse result = {};
        result.valid = false;

        if (!ValidateCRC(frame)) return result;

        std::string payload = ExtractPayload(frame);

        if (payload.size() < 11 || payload[0] != 'R') return result;

        result.nozzle = payload[1] - '0';
        result.transactionId = payload[2];
        int state = payload[3] - '0';
        if (state < 0 || state > 9) return result;
        result.state = static_cast<DispenserState>(state);

        if (payload[4] != ';') return result;

        try {
            result.money = std::stoi(payload.substr(5, 6));
        }
        catch (...) { return result; }

        result.valid = true;
        return result;
    }

    TransactionResponse GasKitProtocol::ParseTransactionResponse(const std::vector<uint8_t>& frame)
    {
        TransactionResponse result = {};
        result.valid = false;

        if (!ValidateCRC(frame)) return result;

        std::string payload = ExtractPayload(frame);

        if (payload.size() < 23 || payload[0] != 'T') return result;

        result.nozzle = payload[1] - '0';
        result.transactionId = payload[2];
        int state = payload[3] - '0';
        if (state < 0 || state > 9) return result;
        result.state = static_cast<DispenserState>(state);

        if (payload[4] != ';') return result;

        try {
            result.money = std::stoi(payload.substr(5, 6));
            if (payload[11] != ';') return result;
            result.volumeCentiliters = std::stoi(payload.substr(12, 6));
            if (payload[18] != ';') return result;
            result.price = std::stoi(payload.substr(19, 4));
        }
        catch (...) { return result; }

        result.valid = true;
        return result;
    }

    TotalCounterResponse GasKitProtocol::ParseTotalCounterResponse(const std::vector<uint8_t>& frame)
    {
        TotalCounterResponse result = {};
        result.valid = false;

        if (!ValidateCRC(frame)) return result;

        std::string payload = ExtractPayload(frame);

        if (payload.size() < 12 || payload[0] != 'C') return result;

        result.nozzle = payload[1] - '0';
        if (payload[2] != ';') return result;

        try {
            result.totalCentiliters = std::stoll(payload.substr(3, 9));
        }
        catch (...) { return result; }

        result.valid = true;
        return result;
    }

    // ============================================================
    // UTILITIES
    // ============================================================

    bool GasKitProtocol::ValidateCRC(const std::vector<uint8_t>& frame)
    {
        // Minimum frame: STX(1) + addr(2) + cmd(1) + CRC(1) = 5 bytes
        if (frame.size() < 5) return false;
        if (frame[0] != STX) return false;

        uint8_t expectedCRC = CalculateCRC(frame, 1, frame.size() - 2);
        uint8_t actualCRC = frame[frame.size() - 1];

        // For debugging
        // Log("Expected CRC: " + std::to_string(expectedCRC) + ", Actual CRC: " + std::to_string(actualCRC));

        return expectedCRC == actualCRC;
    }

    std::string GasKitProtocol::ExtractPayload(const std::vector<uint8_t>& frame)
    {
        // Frame: [STX][addrHi][addrLo][payload...][CRC]
        if (frame.size() < 5) return "";

        std::string payload;
        for (size_t i = 3; i < frame.size() - 1; i++)
        {
            payload += static_cast<char>(frame[i]);
        }
        return payload;
    }

    std::vector<uint8_t> GasKitProtocol::BuildFrame(const std::string& payload)
    {
        std::vector<uint8_t> frame;

        // 1. STX
        frame.push_back(STX);

        // 2. Slave address — 2 BINARY bytes (NOT ASCII!)
        //    Dispenser #1: 0x00, 0x01
        frame.push_back(m_addrHi);
        frame.push_back(m_addrLo);

        // 3. Payload (command + data) — ASCII
        for (char c : payload)
        {
            frame.push_back(static_cast<uint8_t>(c));
        }

        // 4. CRC — XOR from position 1 to last payload byte
        uint8_t crc = CalculateCRC(frame, 1, frame.size() - 1);
        frame.push_back(crc);

        return frame;
    }

    uint8_t GasKitProtocol::CalculateCRC(const std::vector<uint8_t>& data, size_t from, size_t to)
    {
        uint8_t crc = 0;
        for (size_t i = from; i <= to && i < data.size(); i++)
        {
            crc ^= data[i];
        }
        return crc;
    }

    std::string GasKitProtocol::FormatNumber(int value, int width)
    {
        std::ostringstream oss;
        oss << std::setw(width) << std::setfill('0') << value;
        return oss.str();
    }

} // namespace Protocol
} // namespace FuelMaster

// ============================================================
// LicenseManager.h — License and trial period management
// ============================================================

#pragma once

#include <string>
#include <windows.h>

namespace FuelMaster {
namespace Core {

enum class LicenseStatus
{
    Valid,           // License is valid
    TrialExpired,    // Trial period expired
    Invalid,         // Invalid license
    HWIDMismatch,    // HWID does not match
    InstanceLimit,   // Instance limit reached
    TrialActive     // Trial period is active
};

struct LicenseInfo
{
    LicenseStatus status;
    int daysRemaining;      // Days remaining in trial (-1 if license)
    int maxInstances;       // Maximum windows (1 for trial)
    std::string licenseEmail;  // Email from license
    bool isActivated;       // Whether license is activated
};

class LicenseManager
{
public:
    LicenseManager();
    ~LicenseManager();

    // Get license status
    LicenseInfo CheckLicense();

    // Activate license by key
    bool ActivateLicense(const std::string& licenseKey);

    // Get machine HWID
    std::string GetHardwareID();

    // Check number of running instances
    bool CheckInstanceLimit();

    // Release slot on exit
    void ReleaseInstance();

private:
    HANDLE m_semaphore;
    bool m_hasSlot;

    // Embedded RSA-2048 public key (modulus)
    static const char* PUBLIC_KEY_N;
    static const char* PUBLIC_KEY_E;

    // Internal methods
    std::string GetCPUSerial();
    std::string GetMotherboardSerial();
    std::string GetDiskSerial();
    std::string SHA256(const std::string& input);
    bool VerifySignature(const std::string& payload, const std::string& signature);
    
    // Storage operations
    std::string GetTrialDataPath();
    std::string GetLicensePath();
    bool LoadTrialData(time_t& firstRun, int& checksum);
    bool SaveTrialData(time_t firstRun, int checksum);
    bool IsTrialDataValid();
};

} // namespace Core
} // namespace FuelMaster

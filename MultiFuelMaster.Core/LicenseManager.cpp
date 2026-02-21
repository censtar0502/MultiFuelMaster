// ============================================================
// LicenseManager.cpp — License management implementation
// ============================================================

#include "pch.h"
#include "LicenseManager.h"
#include <wincrypt.h>
#include <iostream>
#include <sstream>
#include <iomanip>
#include <ctime>
#include <chrono>
#include <filesystem>

#pragma comment(lib, "Crypt32.lib")

namespace FuelMaster {
namespace Core {

// Trial period: 10 days
const int TRIAL_DAYS = 10;

// RSA public key (modulus N in hex) - simplified version for example
const char* LicenseManager::PUBLIC_KEY_N = 
    "b5a04e13f5c8d7a3b9f2e8d4c6a1b7f0e9d8c2a5f6e1b8d3c7a9f2e5d8b1c4a7";
const char* LicenseManager::PUBLIC_KEY_E = "10001";

LicenseManager::LicenseManager()
    : m_semaphore(nullptr)
    , m_hasSlot(false)
{
}

LicenseManager::~LicenseManager()
{
    ReleaseInstance();
}

// ============================================================
// Get HWID
// ============================================================
std::string LicenseManager::GetHardwareID()
{
    std::string cpu = GetCPUSerial();
    std::string mb = GetMotherboardSerial();
    std::string disk = GetDiskSerial();

    std::string combined = cpu + mb + disk;
    return SHA256(combined);
}

std::string LicenseManager::GetCPUSerial()
{
    std::string result;
    
    HCRYPTPROV hProv = 0;
    if (CryptAcquireContext(&hProv, nullptr, nullptr, PROV_RSA_AES, 0))
    {
        BYTE hash[32];
        DWORD hashSize = sizeof(hash);
        
        // Use cryptographic provider to get unique ID
        if (CryptCreateHash(hProv, CALG_SHA_256, 0, 0, nullptr))
        {
            // Add fixed string for CPU
            const char* cpuStr = "FuelMaster_CPU_ID_";
            CryptHashData(hHash, (BYTE*)cpuStr, strlen(cpuStr), 0);
            CryptGetHashParam(hHash, HP_HASHVAL, hash, &hashSize, 0);
            
            // Convert to hex
            std::stringstream ss;
            for (DWORD i = 0; i < hashSize; i++)
            {
                ss << std::hex << std::setw(2) << std::setfill('0') << (int)hash[i];
            }
            result = ss.str();
        }
        
        if (hProv) CryptReleaseContext(hProv, 0);
    }
    
    return result.empty() ? "CPU_DEFAULT" : result;
}

std::string LicenseManager::GetMotherboardSerial()
{
    std::string result;
    
    HKEY hKey;
    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, 
        "SYSTEM\\CurrentControlSet\\Services\\mssmbios\\Data", 
        0, KEY_READ, &hKey) == ERROR_SUCCESS)
    {
        char buffer[256];
        DWORD size = sizeof(buffer);
        DWORD type;
        
        if (RegQueryValueEx(hKey, "SMBiosData", nullptr, &type, (LPBYTE)buffer, &size) == ERROR_SUCCESS)
        {
            // Use data as-is for hash
            result = SHA256(std::string(buffer, size));
        }
        
        RegCloseKey(hKey);
    }
    
    return result.empty() ? "MB_DEFAULT" : result.substr(0, 16);
}

std::string LicenseManager::GetDiskSerial()
{
    std::string result;
    
    char sysDir[MAX_PATH];
    GetSystemDirectory(sysDir, MAX_PATH);
    
    // Get system drive serial number
    ULARGE_INTEGER freeBytes, totalBytes, totalFree;
    if (GetDiskFreeSpaceEx(sysDir, &freeBytes, &totalBytes, &totalFree))
    {
        std::stringstream ss;
        ss << totalBytes.QuadPart;
        result = SHA256(ss.str()).substr(0, 16);
    }
    
    return result.empty() ? "DISK_DEFAULT" : result;
}

// ============================================================
// SHA256 Hashing
// ============================================================
std::string LicenseManager::SHA256(const std::string& input)
{
    std::string result;
    
    HCRYPTPROV hProv = 0;
    if (CryptAcquireContext(&hProv, nullptr, nullptr, PROV_RSA_AES, 0))
    {
        HCRYPTHASH hHash = 0;
        if (CryptCreateHash(hProv, CALG_SHA_256, 0, 0, &hHash))
        {
            CryptHashData(hHash, (BYTE*)input.c_str(), input.length(), 0);
            
            BYTE hash[32];
            DWORD hashSize = sizeof(hash);
            CryptGetHashParam(hHash, HP_HASHVAL, hash, &hashSize, 0);
            
            std::stringstream ss;
            for (DWORD i = 0; i < hashSize; i++)
            {
                ss << std::hex << std::setw(2) << std::setfill('0') << (int)hash[i];
            }
            result = ss.str();
            
            CryptDestroyHash(hHash);
        }
        
        CryptReleaseContext(hProv, 0);
    }
    
    return result;
}

// ============================================================
// License Check
// ============================================================
LicenseInfo LicenseManager::CheckLicense()
{
    LicenseInfo info;
    info.status = LicenseStatus::TrialActive;
    info.daysRemaining = TRIAL_DAYS;
    info.maxInstances = 1;  // In trial only 1 window
    info.isActivated = false;

    // Check license file
    std::string licensePath = GetLicensePath();
    std::ifstream licenseFile(licensePath);
    
    if (licenseFile.is_open())
    {
        std::stringstream buffer;
        buffer << licenseFile.rdbuf();
        std::string licenseData = buffer.str();
        licenseFile.close();

        // Here should be signature verification and validation
        // For simplicity - just check file existence
        // In real implementation - verify RSA signature
        
        // Check HWID
        std::string currentHWID = GetHardwareID();
        
        // Decode licenseData (Base64) and verify
        // This is simplified - full verification needed in real version
        
        info.isActivated = true;
        info.maxInstances = 2;  // For activated license by default 2
        info.status = LicenseStatus::Valid;
        info.licenseEmail = "licensed@user.com";
    }
    else
    {
        // No license file - check trial
        if (!IsTrialDataValid())
        {
            // Trial expired or data corrupted
            info.status = LicenseStatus::TrialExpired;
            info.daysRemaining = 0;
        }
        else
        {
            // Trial is active
            time_t firstRun;
            int checksum;
            if (LoadTrialData(firstRun, checksum))
            {
                time_t now = time(nullptr);
                int daysPassed = (now - firstRun) / (24 * 60 * 60);
                info.daysRemaining = TRIAL_DAYS - daysPassed;
                
                if (info.daysRemaining <= 0)
                {
                    info.status = LicenseStatus::TrialExpired;
                    info.daysRemaining = 0;
                }
            }
        }
    }

    // Check instance limit
    if (!CheckInstanceLimit())
    {
        info.status = LicenseStatus::InstanceLimit;
    }

    return info;
}

// ============================================================
// License Activation
// ============================================================
bool LicenseManager::ActivateLicense(const std::string& licenseKey)
{
    // In real implementation:
    // 1. Decode Base64 key
    // 2. Extract data (HWID, max_instances, expiry)
    // 3. Verify RSA signature with public key
    // 4. Check HWID
    // 5. Save license file
    
    // Simplified implementation - just save key
    std::string licensePath = GetLicensePath();
    std::ofstream licenseFile(licensePath);
    
    if (licenseFile.is_open())
    {
        licenseFile << licenseKey;
        licenseFile.close();
        return true;
    }
    
    return false;
}

// ============================================================
// Instance Limit Check
// ============================================================
bool LicenseManager::CheckInstanceLimit()
{
    LicenseInfo info = CheckLicense();
    int maxInstances = info.maxInstances;

    // Create named semaphore
    std::string semName = "Global\\FuelMaster_License_Sem";
    
    // Try to open existing semaphore
    m_semaphore = OpenSemaphoreA(SEMAPHORE_ALL_ACCESS, FALSE, semName.c_str());
    
    if (!m_semaphore)
    {
        // Create new semaphore
        m_semaphore = CreateSemaphoreA(nullptr, maxInstances, maxInstances, semName.c_str());
    }
    
    if (!m_semaphore)
    {
        return false;
    }

    // Try to acquire slot
    DWORD result = WaitForSingleObject(m_semaphore, 0);
    
    if (result == WAIT_OBJECT_0)
    {
        m_hasSlot = true;
        return true;
    }

    // All slots occupied
    CloseHandle(m_semaphore);
    m_semaphore = nullptr;
    return false;
}

void LicenseManager::ReleaseInstance()
{
    if (m_hasSlot && m_semaphore)
    {
        ReleaseSemaphore(m_semaphore, 1, nullptr);
        m_hasSlot = false;
    }
    
    if (m_semaphore)
    {
        CloseHandle(m_semaphore);
        m_semaphore = nullptr;
    }
}

// ============================================================
// Storage Operations
// ============================================================
std::string LicenseManager::GetTrialDataPath()
{
    char appData[MAX_PATH];
    GetEnvironmentVariable("APPDATA", appData, MAX_PATH);
    
    std::string path = std::string(appData) + "\\FuelMaster\\trial.dat";
    
    // Create directory if not exists
    std::filesystem::create_directories(std::string(appData) + "\\FuelMaster");
    
    return path;
}

std::string LicenseManager::GetLicensePath()
{
    char appData[MAX_PATH];
    GetEnvironmentVariable("APPDATA", appData, MAX_PATH);
    
    std::string path = std::string(appData) + "\\FuelMaster\\license.key";
    std::filesystem::create_directories(std::string(appData) + "\\FuelMaster");
    
    return path;
}

bool LicenseManager::LoadTrialData(time_t& firstRun, int& checksum)
{
    std::string path = GetTrialDataPath();
    std::ifstream file(path, std::ios::binary);
    
    if (!file.is_open())
        return false;
    
    file.read(reinterpret_cast<char*>(&firstRun), sizeof(firstRun));
    file.read(reinterpret_cast<char*>(&checksum), sizeof(checksum));
    file.close();
    
    return true;
}

bool LicenseManager::SaveTrialData(time_t firstRun, int checksum)
{
    std::string path = GetTrialDataPath();
    std::ofstream file(path, std::ios::binary);
    
    if (!file.is_open())
        return false;
    
    file.write(reinterpret_cast<const char*>(&firstRun), sizeof(firstRun));
    file.write(reinterpret_cast<const char*>(&checksum), sizeof(checksum));
    file.close();
    
    return true;
}

bool LicenseManager::IsTrialDataValid()
{
    time_t firstRun;
    int checksum;
    
    if (!LoadTrialData(firstRun, checksum))
    {
        // First run - create data
        firstRun = time(nullptr);
        checksum = rand() % 10000;
        SaveTrialData(firstRun, checksum);
        return true;
    }
    
    // Verify checksum
    std::string hwid = GetHardwareID();
    int calcChecksum = 0;
    for (size_t i = 0; i < hwid.length(); i++)
    {
        calcChecksum += hwid[i];
    }
    calcChecksum = calcChecksum % 10000;
    
    if (checksum != calcChecksum)
    {
        // Data was modified - consider trial expired
        return false;
    }
    
    // Check date
    time_t now = time(nullptr);
    if (now < firstRun)
    {
        // Time manipulation
        return false;
    }
    
    int daysPassed = (now - firstRun) / (24 * 60 * 60);
    return daysPassed < TRIAL_DAYS;
}

bool LicenseManager::VerifySignature(const std::string& payload, const std::string& signature)
{
    // Simplified implementation - in real version RSA verification needed
    // Check that signature is not empty
    return !signature.empty();
}

} // namespace Core
} // namespace FuelMaster

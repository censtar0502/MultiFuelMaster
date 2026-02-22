using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace MultiFuelMaster.UI
{
    public enum LicenseStatus
    {
        Valid,
        Trial,
        TrialExpired,
        Invalid,
        HWIDMismatch
    }

    public class LicenseInfo
    {
        public LicenseStatus Status { get; set; } = LicenseStatus.Invalid;
        public int DaysRemaining { get; set; }
        public int MaxPanels { get; set; } = 0;   // Макс. кол-во постов (= кол-во ключей)
        public bool IsActivated { get; set; }
        public int ActiveKeys { get; set; }
    }

    [SupportedOSPlatform("windows")]
    public class LicenseManager
    {
        private const int TrialDays = 10;
        private const int MaxSlots = 100;

        // Ключ собирается в рантайме из частей с XOR-маской
        private static readonly byte[] _p1 = { 0x17, 0x1C, 0x7C, 0x1D, 0x18, 0x12, 0x7C, 0x02 };
        private static readonly byte[] _p2 = { 0x14, 0x12, 0x03, 0x14, 0x05, 0x7C, 0x1A, 0x14 };
        private static readonly byte[] _p3 = { 0x08, 0x7C, 0x63, 0x61, 0x63, 0x64, 0x7C, 0x09 };
        private static readonly byte[] _p4 = { 0x68, 0x69, 0x66, 0x67, 0x64, 0x65, 0x62, 0x63 };
        private const byte _xm = 0x51;

        private static byte[] GetSecret()
        {
            var r = new byte[32];
            for (int i = 0; i < 8; i++)
            {
                r[i]      = (byte)(_p1[i] ^ _xm);
                r[i + 8]  = (byte)(_p2[i] ^ _xm);
                r[i + 16] = (byte)(_p3[i] ^ _xm);
                r[i + 24] = (byte)(_p4[i] ^ _xm);
            }
            return r;
        }

        // ===== HWID =====

        public string GetHardwareID()
        {
            string cpuId    = GetCPUId();
            string mbSerial = GetMotherboardSerial();
            string diskSerial = GetDiskSerial();
            return ComputeSHA256(cpuId + mbSerial + diskSerial);
        }

        private string GetCPUId()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                    return obj["ProcessorId"]?.ToString() ?? "CPU_DEFAULT";
            }
            catch { }
            return "CPU_DEFAULT";
        }

        private string GetMotherboardSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (var obj in searcher.Get())
                    return obj["SerialNumber"]?.ToString() ?? "MB_DEFAULT";
            }
            catch { }
            return "MB_DEFAULT";
        }

        private string GetDiskSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID='C:'");
                foreach (var obj in searcher.Get())
                    return obj["VolumeSerialNumber"]?.ToString() ?? "DISK_DEFAULT";
            }
            catch { }
            return "DISK_DEFAULT";
        }

        private static string ComputeSHA256(string input)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // ===== ЛИЦЕНЗИЯ: МНОЖЕСТВЕННЫЕ КЛЮЧИ =====

        public int GetValidKeyCount()
        {
            string licensePath = GetLicensePath();
            if (!File.Exists(licensePath))
                return 0;

            try
            {
                string hwid = GetHardwareID();
                string[] lines = File.ReadAllLines(licensePath);
                int count = 0;
                foreach (string line in lines)
                {
                    string key = line.Trim();
                    if (!string.IsNullOrEmpty(key) && ValidateSingleKey(key, hwid))
                        count++;
                }
                return count;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Проверяет статус лицензии. MaxPanels = кол-во валидных ключей.
        /// В MDI версии нет Semaphore — количество панелей задаётся один раз при запуске.
        /// </summary>
        public LicenseInfo CheckLicense()
        {
            var info = new LicenseInfo();

            int validKeys = GetValidKeyCount();
            info.ActiveKeys = validKeys;

            if (validKeys > 0)
            {
                info.Status       = LicenseStatus.Valid;
                info.IsActivated  = true;
                info.MaxPanels    = validKeys;
                info.DaysRemaining = 999;
                return info;
            }

            // Нет ключей — проверяем пробный период
            if (IsTrialDataValid())
            {
                int daysUsed = GetDaysUsed();
                info.DaysRemaining = Math.Max(0, TrialDays - daysUsed);

                if (info.DaysRemaining > 0)
                {
                    info.Status    = LicenseStatus.Trial;
                    info.MaxPanels = 1;   // триал = 1 пост
                    return info;
                }
            }

            info.Status    = LicenseStatus.TrialExpired;
            info.DaysRemaining = 0;
            info.MaxPanels = 0;
            return info;
        }

        // ===== ВАЛИДАЦИЯ =====

        private bool ValidateSingleKey(string licenseKey, string hwid)
        {
            string cleanKey = licenseKey.Replace("-", "").ToUpperInvariant();
            if (cleanKey.Length != 25) return false;

            foreach (char c in cleanKey)
                if (!((c >= 'A' && c <= 'Z') || (c >= '2' && c <= '7')))
                    return false;

            for (int slot = 1; slot <= MaxSlots; slot++)
            {
                if (string.Equals(cleanKey, GenerateExpectedKey(hwid, slot), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private int GetKeySlot(string licenseKey, string hwid)
        {
            string cleanKey = licenseKey.Replace("-", "").ToUpperInvariant();
            if (cleanKey.Length != 25) return 0;

            for (int slot = 1; slot <= MaxSlots; slot++)
            {
                if (string.Equals(cleanKey, GenerateExpectedKey(hwid, slot), StringComparison.OrdinalIgnoreCase))
                    return slot;
            }
            return 0;
        }

        private static string GenerateExpectedKey(string hwid, int slotNumber)
        {
            byte sm = 0x6F;
            char[] sep = { (char)(0x55 ^ sm), (char)(0x3C ^ sm), (char)(0x23 ^ sm),
                           (char)(0x20 ^ sm), (char)(0x3B ^ sm), (char)(0x55 ^ sm) };
            string input = hwid + new string(sep) + slotNumber.ToString();
            byte[] key = GetSecret();
            using var hmac = new HMACSHA256(key);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            Array.Clear(key, 0, key.Length);
            return ToBase32(hash, 16);
        }

        private static string ToBase32(byte[] data, int byteCount)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            int bits = 0, value = 0;

            for (int i = 0; i < byteCount && i < data.Length; i++)
            {
                value = (value << 8) | data[i];
                bits += 8;
                while (bits >= 5)
                {
                    bits -= 5;
                    result.Append(alphabet[(value >> bits) & 0x1F]);
                }
            }
            if (bits > 0)
                result.Append(alphabet[(value << (5 - bits)) & 0x1F]);

            return result.Length > 25 ? result.ToString().Substring(0, 25) : result.ToString();
        }

        // ===== АКТИВАЦИЯ =====

        public bool ActivateLicense(string licenseKey)
        {
            try
            {
                string hwid = GetHardwareID();
                int slot = GetKeySlot(licenseKey, hwid);
                if (slot == 0) return false;

                string licensePath = GetLicensePath();
                string? directory = Path.GetDirectoryName(licensePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string cleanNew = licenseKey.Replace("-", "").ToUpperInvariant();

                var existingKeys = new List<string>();
                if (File.Exists(licensePath))
                {
                    foreach (string line in File.ReadAllLines(licensePath))
                    {
                        string k = line.Trim();
                        if (!string.IsNullOrEmpty(k))
                        {
                            string cleanExisting = k.Replace("-", "").ToUpperInvariant();
                            if (string.Equals(cleanExisting, cleanNew, StringComparison.OrdinalIgnoreCase))
                                return false; // дубликат
                            int existingSlot = GetKeySlot(k, hwid);
                            if (existingSlot == slot)
                                return false; // слот занят
                            existingKeys.Add(k);
                        }
                    }
                }

                existingKeys.Add(FormatKey(cleanNew));
                File.WriteAllLines(licensePath, existingKeys);
                return true;
            }
            catch { return false; }
        }

        private static string FormatKey(string cleanKey)
        {
            var parts = new List<string>();
            for (int i = 0; i < cleanKey.Length; i += 5)
                parts.Add(cleanKey.Substring(i, Math.Min(5, cleanKey.Length - i)));
            return string.Join("-", parts);
        }

        // ===== ПУТИ =====

        private string GetLicensePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string path = Path.Combine(appData, "MultiFuelMaster", "license.key");
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return path;
        }

        private string GetTrialDataPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string path = Path.Combine(appData, "MultiFuelMaster", "trial.dat");
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return path;
        }

        // ===== ПРОБНЫЙ ПЕРИОД =====

        private bool IsTrialDataValid()
        {
            string path = GetTrialDataPath();
            if (!File.Exists(path))
            {
                SaveFirstRunDate();
                return true;
            }
            try
            {
                string hwid = GetHardwareID();
                int calcChecksum = 0;
                foreach (char c in hwid) calcChecksum += c;

                string[] data = File.ReadAllText(path).Split(',');
                if (data.Length < 2) return false;

                int storedChecksum = int.Parse(data[1]);
                if (Math.Abs(calcChecksum % 10000 - storedChecksum) > 1)
                    return false;

                long firstRun = long.Parse(data[0]);
                long now = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalDays;
                if (now < firstRun) return false;

                return (now - firstRun) < TrialDays;
            }
            catch { return false; }
        }

        private int GetDaysUsed()
        {
            string path = GetTrialDataPath();
            if (!File.Exists(path)) return 0;
            try
            {
                string[] data = File.ReadAllText(path).Split(',');
                long firstRun = long.Parse(data[0]);
                long now = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalDays;
                return (int)(now - firstRun);
            }
            catch { return 0; }
        }

        private void SaveFirstRunDate()
        {
            string path = GetTrialDataPath();
            long now = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalDays;
            string hwid = GetHardwareID();
            int checksum = 0;
            foreach (char c in hwid) checksum += c;
            checksum %= 10000;
            File.WriteAllText(path, $"{now},{checksum}");
        }
    }
}

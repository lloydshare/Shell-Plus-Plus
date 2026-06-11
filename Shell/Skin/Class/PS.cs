using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;

namespace CustomForm
{
    //todo: detecting screen-prints and screen recordings
    public static class PS
    {
        // Raw P/Invoke – no extra packages needed
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001; // old flag (shows on monitor only)
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011; // modern flag – black in captures

        public enum WDA_ATTRIBUTE
        {
            WDA_NONE = 0,
            WDA_MONITOR = 1,
            WDA_EXCLUDEFROMCAPTURE = 2
        }

        public static void ScreenHandler(bool protect, IntPtr HandleToProtect, WDA_ATTRIBUTE attr)
        {
            IntPtr hwnd = HandleToProtect;  // WinForms: this.Handle

            // For WPF you'd use: new WindowInteropHelper(this).Handle

            uint affinity = attr == WDA_ATTRIBUTE.WDA_MONITOR ? WDA_MONITOR : attr == WDA_ATTRIBUTE.WDA_EXCLUDEFROMCAPTURE ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;

            //uint affinity = protect ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE; //hidden
            //uint affinity =  protect ? WDA_MONITOR : WDA_NONE; //black box

            bool success = SetWindowDisplayAffinity(hwnd, affinity);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"SetWindowDisplayAffinity failed with error: {error}");
                // Common errors:
                // 87  = ERROR_INVALID_PARAMETER → wrong flag or OS too old
                // 1400 = ERROR_INVALID_WINDOW_HANDLE → HWND not valid yet
            }
            else
            {
                Console.WriteLine(protect ? "Screen capture protection ENABLED" : "Screen capture protection DISABLED");
            }
        }

        public class TpmInfo
        {
            public bool IsPresent { get; set; }
            public bool IsReady { get; set; }     // Ready = activated + owned + enabled
            public string SpecVersion { get; set; } // "2.0" etc.
        }
        public class BitLockerStatus
        {
            public string DriveLetter { get; set; }
            public bool IsEncrypted { get; set; }
            public bool IsProtectionOn { get; set; } // Actively protecting
            public string ConversionStatus { get; set; } // "FullyEncrypted", "EncryptionInProgress", etc.
        }

        public static class SystemInfo
        {
            // Helper: safe property getter (returns null if property doesn't exist)
            private static object GetWmiPropertySafe(ManagementBaseObject mo, string name)
            {
                if (mo == null) return null;
                var pd = mo.Properties.Cast<PropertyData>()
                            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                return pd?.Value;
            }

            // Diagnostic helper to list properties (call manually on target machine)
            public static void DumpWmiProperties(string query = "SELECT * FROM Win32_ComputerSystem")
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(query);
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var names = obj.Properties.Cast<PropertyData>().Select(p => p.Name);
                        Console.WriteLine($"WMI object properties: {string.Join(", ", names)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DumpWmiProperties failed: {ex.Message}");
                }
            }

            public static TpmInfo GetTpmStatus()
            {
                var info = new TpmInfo();

                try
                {
                    using var searcher = new ManagementObjectSearcher("root\\CIMV2\\Security\\MicrosoftTpm", "SELECT * FROM Win32_Tpm");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.IsPresent = true;

                        var isReadyVal = GetWmiPropertySafe(obj, "IsReady");
                        info.IsReady = isReadyVal is bool b && b;

                        var specVal = GetWmiPropertySafe(obj, "SpecVersion");
                        info.SpecVersion = specVal?.ToString() ?? "Unknown";

                        break; // Usually only one TPM
                    }
                }
                catch
                {
                    // TPM class not found → no TPM or WMI access issue
                }

                return info;
            }

            public static string GetSecureBootState()
            {
                // Fast path: msinfo32-style registry check (works on most systems)
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
                if (key != null)
                {
                    var uefi = key.GetValue("UEFISecureBootEnabled") as int?;
                    if (uefi == 1) return "Enabled";
                }

                using var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    var pd = obj.Properties.Cast<PropertyData>().FirstOrDefault(p => string.Equals(p.Name, "SecureBootEnabled", StringComparison.OrdinalIgnoreCase));
                    if (pd != null)
                    {
                        var val = pd.Value?.ToString();
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                }

                return "Unknown / Unsupported";
            }
           
            private static bool IsElevated()
            {
                var id = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(id);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }

            public static List<BitLockerStatus> GetBitLockerDriveStatus()
            {
                var results = new List<BitLockerStatus>();
                if (!IsElevated())
                {
                    //Console.WriteLine("Skipping BitLocker check: process is not running elevated.");
                    var status = new BitLockerStatus
                    {
                        //DriveLetter = driveLetter,
                       // IsProtectionOn = protVal == 1, // 1 = protection on
                        ConversionStatus = GetConversionStatusName(6),
                       // IsEncrypted = convVal == 1 // 1 = fully encrypted
                    };

                    results.Add(status);
                    return results;
                }

                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        "root\\CIMV2\\Security\\MicrosoftVolumeEncryption",
                        "SELECT * FROM Win32_EncryptableVolume");

                    foreach (ManagementObject vol in searcher.Get())
                    {
                        var driveLetterVal = GetWmiPropertySafe(vol, "DriveLetter");
                        string driveLetter = driveLetterVal?.ToString() ?? "Unknown";

                        uint protVal = 0;
                        var protObj = GetWmiPropertySafe(vol, "ProtectionStatus");
                        if (protObj is uint pu) protVal = pu;
                        else if (protObj is int pi) protVal = (uint)pi;
                        else if (protObj is short ps) protVal = (uint)ps;

                        uint convVal = 0;
                        var convObj = GetWmiPropertySafe(vol, "ConversionStatus");
                        if (convObj is uint cu) convVal = cu;
                        else if (convObj is int ci) convVal = (uint)ci;
                        else if (convObj is short cs) convVal = (uint)cs;

                        var status = new BitLockerStatus
                        {
                            DriveLetter = driveLetter,
                            IsProtectionOn = protVal == 1, // 1 = protection on
                            ConversionStatus = GetConversionStatusName(convVal),
                            IsEncrypted = convVal == 1 // 1 = fully encrypted
                        };

                        results.Add(status);
                    }
                }
                catch (System.Management.ManagementException mex)
                {
                    Console.WriteLine($"BitLocker WMI failed: {mex.ErrorCode} - {mex.Message}");
                }

                return results;
            }

            private static string GetConversionStatusName(uint status)
            {
                return status switch
                {
                    0 => "FullyDecrypted",
                    1 => "FullyEncrypted",
                    2 => "EncryptionInProgress",
                    3 => "DecryptionInProgress",
                    4 => "EncryptionPaused",
                    5 => "DecryptionPaused",
                    6 => "Skipping BitLocker check: process is not running elevated.",
                    _ => $"Unknown ({status})"
                };
            }
        }
    }
}

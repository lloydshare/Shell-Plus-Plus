using Core;
using CustomForm;
using Editor.Controls;
using Microsoft.Win32;
using System.Reflection;

#region XML Format

public class settings
{
    public System machine;
    public SystemTemp system;
    public class System
    {
        public string MachineGuid;
        public int wHeight;
        public int wWidth;
        public Point Position;
        public DateTime LastRun;
    }

    public class SystemTemp
    {
        public byte Theme;
        public FormWindowState wState;
        public string ScreenDeviceName;

        public bool ThemeButton;
        public bool ShowDropShadow;
        public bool RoundedCorners;
    }
}

#endregion

public static class Persistor
{
    private static settings settings;
    private static string configPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath));
    private static string configFile = string.Format("{0}\\{1}.config", configPath, Application.ProductName);
        private static SkinManager SM = CustomForm.SkinManager.Instance;

    private static string GetMachineGuid()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return key?.GetValue("MachineGuid")?.ToString() ?? "UNKNOWN";
    }

    private static string Sanitize(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            throw new ArgumentException("Path cannot be null or empty.", nameof(fullPath));

        string directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        string fileName = Path.GetFileName(fullPath);

        var invalidChars = Path.GetInvalidFileNameChars();
        string safeFileName = string.Concat(fileName.Select(c => invalidChars.Contains(c) ? '_' : c));

        return Path.Combine(directory, safeFileName);
    }

    private static class Prv_Persistor
    {
        public static void SaveAsXml(object SerializeObj, string fileName)
        {
            string error = XML.Serialize(SerializeObj, Sanitize(fileName), new[] { BindingFlags.Public, BindingFlags.Instance });
            if (error != null)
                FileSystem.WriteError("Persistor", "XML.Serialize", error);
            return;
        }

        public static object LoadObjectFromXml(Type objType, string fileName)
        {
            var (deserialized, error) = XML.DeSerialize(objType, Sanitize(fileName), new[] { BindingFlags.Public, BindingFlags.Instance });
            if (error != null)
                FileSystem.WriteError("Persistor", "XML.DeSerialize", error);
            return deserialized;
        }
    }

    public static void SaveAsXml(object SerializeObj, string fileName)
    {
        Prv_Persistor.SaveAsXml(SerializeObj, fileName);
    }

    public static object LoadFromXml(Type objType, string fileName)
    {
        return Prv_Persistor.LoadObjectFromXml(objType, fileName);
    }

    private static void Clean()
    {
        settings.system = null;
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(configFile))
            {
                return;
            }
            else
            {
                settings = new settings();
                settings = (settings)LoadFromXml(settings.GetType(), configFile);
            }

            //get the main form, from the skin manager
            Skin MainForm = SM.GetMainForm();

            //set form props
            MainForm.Width = settings.machine.wWidth;
            MainForm.Height = settings.machine.wHeight;
            MainForm.WindowState = settings.system.wState;

            MainForm.StartPosition = FormStartPosition.Manual;

            //// Find the target screen by DeviceName
            Screen? targetScreen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == settings.system.ScreenDeviceName);

            if (targetScreen != null)
            {
                // Check if the saved position is within the target screen's working area
                if (targetScreen.WorkingArea.Contains(settings.machine.Position))
                {
                    MainForm.Location = settings.machine.Position;  // Use saved position if valid
                }
                else
                {
                    MainForm.Location = targetScreen.WorkingArea.Location; // Default to top-left of screen
                }
            }
            else
            {
                // Fallback to primary screen if saved screen not found
                MainForm.Location = Screen.PrimaryScreen.WorkingArea.Location;
            }

            //save theme and other settings
            SM.Theme = (CustomForm.SkinManager.Themes)settings.system.Theme;

            MainForm.ThemeButton = settings.system.ThemeButton;
            MainForm.ShowDropShadow = settings.system.ShowDropShadow;
            MainForm.RoundedCorners = settings.system.RoundedCorners;
            


            //clean-up duplicated resources
            Clean();
        }
        catch (Exception ex)
        {
            FileSystem.WriteError("Persistor", "Load()", ex.Message);
        }
    }

    public static void Save()
    {
        try
        {
            //get the main form, from the skin manager
            Skin MainForm = SM.GetMainForm();

            settings XMLData = new settings();

            XMLData.machine = new settings.System();
            XMLData.system = new settings.SystemTemp();

            //more efficent this way, don't have to monitor every form resize
            if (MainForm.Maximized) //save previous settings
            {
                if (settings != null)
                {
                    XMLData.machine.wWidth = settings.machine.wWidth;
                    XMLData.machine.wHeight = settings.machine.wHeight;
                    XMLData.machine.Position = settings.machine.Position;

                }
                else //default window size for first time - to catch first time running, causes an logic error
                {
                    XMLData.machine.wWidth = 1024;
                    XMLData.machine.wHeight = 800;
                    XMLData.machine.Position = MainForm.Location;
                }
            }
            else
            {
                XMLData.machine.wWidth = MainForm.Width;
                XMLData.machine.wHeight = MainForm.Height;
                XMLData.machine.Position = MainForm.Location;
            }

            XMLData.system.ThemeButton = MainForm.ThemeButton;
            XMLData.system.ShowDropShadow = MainForm.ShowDropShadow;
            XMLData.system.RoundedCorners = MainForm.RoundedCorners;

            XMLData.system.wState = MainForm.WindowState;
            XMLData.system.Theme = (byte)SM.Theme;
            XMLData.system.ScreenDeviceName = Screen.FromControl(MainForm).DeviceName;
            XMLData.machine.LastRun = DateTime.Now;

            // XMLData.machine.LastRun = DateTime.Now;
            XMLData.machine.MachineGuid = GetMachineGuid();

            SaveAsXml(XMLData, configFile);
        }
        catch (Exception ex)
        {
            FileSystem.WriteError("Persistor", "Save()", ex.Message);
        }
    }

    /// <summary>
    /// Loads shell content from a file into the shellbox using streaming (low memory usage).
    /// Throws helpful exceptions on failure.
    /// </summary>
    /// <param name="rtb">The ShellBox to load into</param>
    /// <param name="filePath">Full path to the .rtf file</param>
    /// <exception cref="FileNotFoundException">If the file doesn't exist</exception>
    /// <exception cref="IOException">Other file/stream errors</exception>
    /// <exception cref="ArgumentException">Invalid RTF format or other parse errors</exception>
    public static void LoadShellFromFile(ShellBox rtb, string ID)
    {
        try
        {
            string filePath = string.Format("{0}\\{1}.shell", configPath, ID);

            if (rtb == null) throw new ArgumentNullException(nameof(rtb));

            if (!File.Exists(filePath))
            {
                //throw new FileNotFoundException("shell file not found", filePath);
                return;
            }

            // Stream-based load — best for large files
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                rtb.LoadFile(stream, RichTextBoxStreamType.RichText);
            }

            //Console.WriteLine(); //newline
            // Optional: Force refresh / repaint in case of UI quirks
            //rtb.Refresh();
        }
        catch (Exception ex)
        {
            FileSystem.WriteError("Persistor", "LoadShellFromFile", ex.Message);
        }
    }

    public static void SaveShellToFile(ShellBox rtb, string ID)
    {
        try
        {
            if (rtb == null) throw new ArgumentNullException(nameof(rtb));

            string filePath = Path.Combine(configPath, $"{ID}.shell");

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            //// Find the very last \par
            //int lastParIndex = rtb.Rtf.LastIndexOf("\\par", StringComparison.Ordinal);

            //// Find the second-to-last \par
            //int secondLastParIndex = rtb.Rtf.LastIndexOf("\\par", lastParIndex - 1, StringComparison.Ordinal);

            //string trimmedRtf;

            //if (secondLastParIndex >= 0)
            //{
            //    // Keep: everything up to (and including) the second-last \par
            //    // Then skip the second-last \par itself
            //    // Then append from the last \par to the end (including the last \par)
            //    trimmedRtf = rtb.Rtf.Substring(0, secondLastParIndex)
            //               + rtb.Rtf.Substring(lastParIndex);
            //}
            //else
            //{
            //    // Only one \par exists → can't remove second-last → keep original
            //    trimmedRtf = rtb.Rtf;
            //}

            //// Final cleanup: make sure it ends properly with }
            //trimmedRtf = trimmedRtf.TrimEnd();
            //if (!trimmedRtf.EndsWith("}"))
            //{
            //    trimmedRtf += "\r\n}";
            //}

            //// Optional: remove any accidental multiple trailing \par before }
            //while (trimmedRtf.EndsWith("\\par}"))
            //{
            //    trimmedRtf = trimmedRtf.Substring(0, trimmedRtf.Length - "\\par}".Length) + "}";
            //}

           // rtb.Rtf.Trim()

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(rtb.Rtf);
            }
        }
        catch (Exception ex)
        {
            FileSystem.WriteError("Persistor", "SaveShellToFile", ex.Message);
        }
    }
}


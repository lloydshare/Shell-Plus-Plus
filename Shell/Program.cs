using Core;
using Editor.Controls;
using System.Diagnostics;
using System.Text;

namespace Shell
{
    internal static class Program
    {
        public static ShellFrm MainForm;
        private static string s_terminalWorkDirectory = GlobalVariables.terminalWorkDirectory;
        public static TextBoxWriter ConsoleWriter;

        // Deletes current directory temp file ? what is this
        private static void DeleteCDFIle()
        {

            // Creating the xTerminal directory under current user Appdata/Local.
            if (!Directory.Exists(s_terminalWorkDirectory))
                Directory.CreateDirectory(s_terminalWorkDirectory);

            var getFiles = Directory.GetFiles(GlobalVariables.terminalWorkDirectory);
            var listFilesID = new List<string>();
            var listProcessID = new List<string>();
            foreach (var file in getFiles)
            {
                if (file.EndsWith("cDir.t"))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    var fileCDir = fileInfo.Name.Replace("cDir.t", "");
                    listFilesID.Add(fileCDir);
                }
            }
            foreach (var process in Process.GetProcessesByName("xTerminal"))
            {
                listProcessID.Add(process.Id.ToString());
            }

            var finalListID = listFilesID.Except(listProcessID).ToList();

            foreach (var file in getFiles)
            {
                foreach (var item in finalListID)
                {
                    if (file.EndsWith("cDir.t") && file.Contains(item))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        File.Delete(fileInfo.FullName);
                    }
                }
            }

            if (File.Exists(GlobalVariables.currentDirectory))
                File.Delete(GlobalVariables.currentDirectory);
        }

        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            MainForm = new ShellFrm(args);

            DeleteCDFIle();

            ConsoleWriter = new TextBoxWriter(MainForm.shellBoxWithScrollBar1);

            Console.SetOut(ConsoleWriter);
            Console.SetError(ConsoleWriter); // redirect stderr

            Application.Run(MainForm);
        }
    }

    public class TextBoxWriter : TextWriter
    {
        private readonly RichTextBox _textBox;

        public TextBoxWriter(Editor.Controls.ShellBoxWithScrollBar SBox)
        {
            _textBox = SBox.ShellBox;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine()
        {
            WriteLine("");
        }
        public override void WriteLine(string value)
        {

            // Replace lone \n (not preceded by \r) with \r\n
            string normalized = System.Text.RegularExpressions.Regex.Replace(
                value,
                @"(?<!\r)\n",
                "\r\n"
            );

            Write(Environment.NewLine + normalized);

        }

        public override void Write(string value)
        {
            if (_textBox.InvokeRequired)
            {
                _textBox.Invoke(new Action(() => _textBox.AppendText(value)));
            }
            else
            {
                _textBox.AppendText(value);
            }
        }

    }

}
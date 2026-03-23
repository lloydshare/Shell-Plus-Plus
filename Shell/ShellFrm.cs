using CustomForm;
using Editor.Controls;
using System.Runtime.InteropServices;

namespace Shell
{
    public partial class ShellFrm : Skin, ISkin
    {
        //todo:           
        //-_--Commands------//
        //make Control C CNTL-X work //local to the app keyboard hook and (keylogger (off by default))
        //picview (change to view cmd) cmd -- needs more options
        //fix snake
        //fix wtop
        //fix ping and trace output
        //custom top?
        //cmd and ps modes
        //webbrowser - wget
        //"w" for listing users
        //documention and help - man cmd and GUI
        //sudo cmd -- elivated priv
        //fastfetch\about system cmd
        //lspci cmd
        //usb cmds
        //GNU
        //qemu? no graphics output
        //AI intergration
        //vlc & windows video player?
        //ssh/telnet/vtt - ?
        //-------------------//

        //------Others-------//
        //embed properties window into new tab or main lexicon?
        //security options - (Special compile options)
        //memory options
        //plugin loader (unsigned & signed) for custom commands and control/forms
        //script extentions/file association for scripts and option to disable and global right click menu options
        //multi-threading
        // multi-lingrual?
        // profile & performace benchmarking
        // Icon design
        //-------------------//

        //------Design-------//
        //Themes?
        //scroll bar preview view
        //powershell & cmd colors and theme
        //properties - copy & paste
        //multi-terms + ALT f1, f2, etc, tabbed windows - with a "+" and "x" symbol and simple open and close animation
        //carets - no system defaults etc. - animations like ollma & grok prompt
        //more custom title bars? - orginial\system theme options
        //different prompt styles & colors & themes
        //properties form on dark theme - renders badly
        //-------------------//

        //--------Bugs-------//
        //config to save in reg and themed error message
        //Persistance needs to save & load the shell window
        //Searchable History and lexicon (with intellisense)
        //fix autocomplete junk
        //prompt to start from top? or bottom of screen  -- Compute is running to fast
        //changing theme removes all color formattings
        //background image doesn't work
        //--fix selection of text - work like powershell
        //rounded corners still looks junky
        //scroll bar needs to stop moving at mouse location when holding down
        //dwm is going crashing and gone strange, whats happened there?
        //maximize on title bar doesn't toggle
        //move on title bar doesnt drag move - mouse hook?
        //make autocomplete and intellisense work
        //with a clearned shell, running cmds like ping wont stream
        //-------------------//

        private string[] _args;
        public Shell shell = new Shell();

        private readonly SkinManager SkinManager;

        public ShellFrm(string[] args)
        {
            _args = args;
            InitializeComponent();

            SkinManager = SkinManager.Apply(this);
            CreateActions();

            var rtb = shellBoxWithScrollBar1.ShellBox;
            rtb.AcceptsTab = true;

            rtb.KeyDown += InnerTextBox_KeyDown;
            rtb.MouseDown += InnerTextBox_MouseEvent;
            rtb.MouseUp += InnerTextBox_MouseEvent;

            new CaretStyle
            {
                Width = 7,
                Height = 5,
                Color = Color.Purple,
                VAlign = CaretVerticalAlign.Bottom,
                EnableBlink = true,
                BlinkIntervalMs = SystemInformation.CaretBlinkTime,//500,
                CornerRadius = 0,
                UseGradient = false,
                GradientStartColor = Color.LightGreen,
                GradientEndColor = Color.DarkGreen,
                GradientMode = CaretGradientMode.Vertical,
                UseGlow = false,
                GlowColor = Color.FromArgb(160, Color.Gray),
                GlowRadius = 3,
                GlowIntensity = 1,
                UseFillStyle = true,
                FillStyle = FillStyle.BlinkingOutline,
                OutlinePenWidth = 3,

            }.ApplyTo((ICaretStylable)shellBoxWithScrollBar1.ShellBox);

            shellBoxWithScrollBar1.ShellBox.AutoSizeControlsToBox = false;
        }

        private void InnerTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            Shell.KeyDown(e);

            ConsoleKeyInfo keyInfo = MapKeyEventToConsoleKeyInfo(e);

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                shell.ResetHistoryIndex();
                shell.Run();
                
            }
            else if (e.KeyCode == Keys.Back || e.KeyCode == Keys.RButton || e.KeyCode == Keys.Left) //set a margin
            {
                int caretIndex = shellBoxWithScrollBar1.ShellBox.SelectionStart;
                int currentLine = shellBoxWithScrollBar1.ShellBox.GetLineFromCharIndex(caretIndex);
                int firstCharInLine = shellBoxWithScrollBar1.ShellBox.GetFirstCharIndexFromLine(currentLine);
                int columnIndex = caretIndex - firstCharInLine;

                if (columnIndex <= shell.prompt_length)
                {
                    e.SuppressKeyPress = true;
                }

                if (e.KeyCode == Keys.Back)
                {
                    shell.keyHandler.Handle(keyInfo);
                }
            }
            else
            {
                e.SuppressKeyPress = true;
                shell.keyHandler.Handle(keyInfo);
            }
        }

        #region Old
        //public static ConsoleKeyInfo MapKeyEventToConsoleKeyInfo(KeyEventArgs e)
        //{
        //    char keyChar = '\0';
        //    ConsoleKey consoleKey = ConsoleKey.None;

        //    // Map common keys
        //    switch (e.KeyCode)
        //    {
        //        case Keys.Enter: keyChar = '\r'; consoleKey = ConsoleKey.Enter; break;
        //        case Keys.Tab: keyChar = '\t'; consoleKey = ConsoleKey.Tab; break;
        //        case Keys.Back: keyChar = '\b'; consoleKey = ConsoleKey.Backspace; break;
        //        case Keys.Escape: keyChar = (char)27; consoleKey = ConsoleKey.Escape; break;
        //        case Keys.Space: keyChar = ' '; consoleKey = ConsoleKey.Spacebar; break;
        //        case Keys.Left: consoleKey = ConsoleKey.LeftArrow; break;
        //        case Keys.Right: consoleKey = ConsoleKey.RightArrow; break;
        //        case Keys.Up: consoleKey = ConsoleKey.UpArrow; break;
        //        case Keys.Down: consoleKey = ConsoleKey.DownArrow; break;
        //        case Keys.Delete: consoleKey = ConsoleKey.Delete; break;
        //        case Keys.Home: consoleKey = ConsoleKey.Home; break;
        //        case Keys.End: consoleKey = ConsoleKey.End; break;
        //        case Keys.PageUp: consoleKey = ConsoleKey.PageUp; break;
        //        case Keys.PageDown: consoleKey = ConsoleKey.PageDown; break;
        //        case Keys.Insert: consoleKey = ConsoleKey.Insert; break;
        //        default:
        //            // Letters
        //            if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
        //            {
        //                keyChar = (char)(e.KeyCode - Keys.A + 'A');
        //                if (!e.Shift) keyChar = char.ToLower(keyChar);
        //                consoleKey = (ConsoleKey)(e.KeyCode - Keys.A + (int)ConsoleKey.A);
        //            }
        //            // Digits (top row)
        //            else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
        //            {
        //                string normal = "0123456789";
        //                string shifted = ")!@#$%^&*(";
        //                int idx = e.KeyCode - Keys.D0;
        //                keyChar = e.Shift ? shifted[idx] : normal[idx];
        //                consoleKey = (ConsoleKey)(e.KeyCode - Keys.D0 + (int)ConsoleKey.D0);
        //            }
        //            // Numpad digits
        //            else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
        //            {
        //                keyChar = (char)(e.KeyCode - Keys.NumPad0 + '0');
        //                consoleKey = (ConsoleKey)(e.KeyCode - Keys.NumPad0 + (int)ConsoleKey.NumPad0);
        //            }
        //            // Punctuation and symbols
        //            else
        //            {
        //                // Map Oem keys
        //                keyChar = e.KeyCode switch
        //                {
        //                    Keys.OemPeriod => e.Shift ? '>' : '.',
        //                    Keys.Oemcomma => e.Shift ? '<' : ',',
        //                    Keys.OemMinus => e.Shift ? '_' : '-',
        //                    Keys.Oemplus => e.Shift ? '+' : '=',
        //                    Keys.Oem1 => e.Shift ? ':' : ';',         // ;:
        //                    Keys.Oem7 => e.Shift ? '"' : '\'',        // '"
        //                    Keys.OemQuestion => e.Shift ? '?' : '/',  // /?
        //                    Keys.Oem5 => e.Shift ? '|' : '\\',        // \|
        //                    Keys.OemOpenBrackets => e.Shift ? '{' : '[', // [{
        //                    Keys.Oem6 => e.Shift ? '}' : ']',         // ]}
        //                    Keys.Oemtilde => e.Shift ? '~' : '`',     // `~
        //                    _ => '\0'
        //                };
        //                // Optionally, set consoleKey for these as well if needed
        //            }
        //            break;
        //    }

        //    // Set modifiers
        //    ConsoleModifiers modifiers = 0;
        //    if (e.Control) modifiers |= ConsoleModifiers.Control;
        //    if (e.Shift) modifiers |= ConsoleModifiers.Shift;
        //    if (e.Alt) modifiers |= ConsoleModifiers.Alt;

        //    return new ConsoleKeyInfo(keyChar, consoleKey, e.Shift, e.Alt, e.Control);
        //}
        #endregion

        public static ConsoleKeyInfo MapKeyEventToConsoleKeyInfo(KeyEventArgs e)
        {
            // 1. Determine ConsoleKey from Keys (this part is mostly layout-independent)
            ConsoleKey consoleKey = MapToConsoleKey(e.KeyCode);

            // 2. Get the actual character Windows would produce (layout-aware)
            char keyChar = GetCharacterFromKey(e);

            // 3. Modifiers
            ConsoleModifiers modifiers = 0;
            if (e.Control) modifiers |= ConsoleModifiers.Control;
            if (e.Shift) modifiers |= ConsoleModifiers.Shift;
            if (e.Alt) modifiers |= ConsoleModifiers.Alt;

            return new ConsoleKeyInfo(keyChar, consoleKey, e.Shift, e.Alt, e.Control);
        }

        private static char GetCharacterFromKey(KeyEventArgs e)
        {
            byte[] keyboardState = new byte[256];
            if (e.Shift) keyboardState[(int)Keys.ShiftKey] = 0x80;
            if (e.Control) keyboardState[(int)Keys.ControlKey] = 0x80;
            if (e.Alt) keyboardState[(int)Keys.Menu] = 0x80;           // Alt

            var sb = new System.Text.StringBuilder(5);

            int result = ToUnicode(
                (uint)e.KeyCode,
                0,                          // scan code = 0 is usually fine here
                keyboardState,
                sb,
                sb.Capacity,
                0);

            if (result > 0 && sb.Length > 0)
                return sb[0];

            // Fallback for control characters, arrows, function keys etc.
            return '\0';
        }

        private static ConsoleKey MapToConsoleKey(Keys keyCode)
        {
            // Same mapping you already have for arrows, enter, etc.
            return keyCode switch
            {
                Keys.Enter => ConsoleKey.Enter,
                Keys.Tab => ConsoleKey.Tab,
                Keys.Back => ConsoleKey.Backspace,
                Keys.Escape => ConsoleKey.Escape,
                Keys.Space => ConsoleKey.Spacebar,
                Keys.Left => ConsoleKey.LeftArrow,
                Keys.Right => ConsoleKey.RightArrow,
                Keys.Up => ConsoleKey.UpArrow,
                Keys.Down => ConsoleKey.DownArrow,
                Keys.Delete => ConsoleKey.Delete,
                Keys.Home => ConsoleKey.Home,
                Keys.End => ConsoleKey.End,
                Keys.PageUp => ConsoleKey.PageUp,
                Keys.PageDown => ConsoleKey.PageDown,
                Keys.Insert => ConsoleKey.Insert,

                // A–Z (case insensitive)
                _ when keyCode >= Keys.A && keyCode <= Keys.Z =>
                    (ConsoleKey)(keyCode - Keys.A + (int)ConsoleKey.A),

                // D0–D9
                _ when keyCode >= Keys.D0 && keyCode <= Keys.D9 =>
                    (ConsoleKey)(keyCode - Keys.D0 + (int)ConsoleKey.D0),

                // NumPad0–9
                _ when keyCode >= Keys.NumPad0 && keyCode <= Keys.NumPad9 =>
                    (ConsoleKey)(keyCode - Keys.NumPad0 + (int)ConsoleKey.NumPad0),

                _ => ConsoleKey.None
            };
        }

        private void InnerTextBox_MouseEvent(object? sender, MouseEventArgs e)
        {
            var textBox = shellBoxWithScrollBar1.ShellBox;
            int promptLength = shell.prompt_length;

            int caretPosition = textBox.TextLength;

            if (caretPosition < promptLength)
            {
                caretPosition = promptLength;
            }

            textBox.SelectionStart = caretPosition;
            textBox.SelectionLength = 0;
        }

        private void CreateActions()
        {
            #region Global Console Actions

            AppDomain.CurrentDomain.SetData("WindowWidth", (Func<int>)(() =>
            {
                return shellBoxWithScrollBar1.ShellBox.Invoke((Func<int>)(() => Width));

            }));

            //AppDomain.CurrentDomain.SetData("WindowHeight", (Func<int>)(() =>
            //{
            //    if (shellBoxWithScrollBar1.ShellBox.InvokeRequired)
            //    {
            //        var tcs = new TaskCompletionSource<int>();
            //        shellBoxWithScrollBar1.ShellBox.InvokeAsync(() => tcs.SetResult(Height));
            //        return tcs.Task.GetAwaiter().GetResult();  // still sync, but safer in some cases
            //    }
            //    return Height;
            //}));

            AppDomain.CurrentDomain.SetData("WindowHeight", (Func<int>)(() =>
            {
                return shellBoxWithScrollBar1.ShellBox.Invoke((Func<int>)(() => Height));

            }));

            AppDomain.CurrentDomain.SetData("WindowWidth", (Func<int>)(() =>
            {
                return shellBoxWithScrollBar1.ShellBox.Invoke((Func<int>)(() => Width));

            }));

            AppDomain.CurrentDomain.SetData("AddControl", (Action<Control>)(control =>
            {
                Invoke((Action)(() =>
                {
                    Console.WriteLine(); //newline
                    var tb = shellBoxWithScrollBar1.ShellBox;
                    tb.AddControl(control);
                }));

            }));

            AppDomain.CurrentDomain.SetData("ClearConsole", (Action)(() =>
            {
                Invoke((Action)(() => shellBoxWithScrollBar1.ShellBox.Clear()));

            }));

            AppDomain.CurrentDomain.SetData("SetConsoleTextColor", (Action<Color>)(color =>
            {
                Invoke((Action)(() =>
                {
                    var tb = shellBoxWithScrollBar1.ShellBox;
                    tb.SelectionStart = tb.TextLength;
                    tb.SelectionLength = 0;
                    tb.SelectionColor = color;
                }));

            }));

            AppDomain.CurrentDomain.SetData("GetConsoleTextColor", (Func<Color>)(() =>
            {
                return shellBoxWithScrollBar1.ShellBox.Invoke((Func<Color>)(() => shellBoxWithScrollBar1.ShellBox.SelectionColor));

            }));

            AppDomain.CurrentDomain.SetData("ClearLine", (Action)(() =>
            {
                Invoke((Action)(() =>
                {
                    //clear prompt line
                    var tb = shellBoxWithScrollBar1.ShellBox;
                    // Get the current line index
                    int lineIndex = tb.GetLineFromCharIndex(tb.SelectionStart);
                    // int lineIndex = tb.GetLineCount();
                    int lineStart = tb.GetFirstCharIndexFromLine(lineIndex);

                    // int TotalLines = tb.Lines.Length;

                    // //string TestL = tb.Lines[lineIndex];
                    // // Get the full line text
                    // string lineText = tb.Lines.Length > lineIndex ? tb.Lines[lineIndex] : string.Empty;

                    string lineText = tb.Lines[tb.Lines.Length - 1];

                    // Only clear after the prompt
                    int inputStart = lineStart + Math.Min(shell.prompt_length, lineText.Length);
                    int inputLength = Math.Max(0, lineText.Length - shell.prompt_length);

                    tb.SelectionStart = inputStart;
                    tb.SelectionLength = inputLength;
                    tb.SelectedText = string.Empty;

                }));

            }));

            #endregion

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            shell.Start(_args, shellBoxWithScrollBar1, this); //Start the shell
        }

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags);
    }
}

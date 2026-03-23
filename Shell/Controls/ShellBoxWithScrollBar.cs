using CustomForm;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Editor.Controls
{
    public class ShellBoxWithScrollBar : UserControl
    {
        public ShellBox ShellBox { get; }
        private DarkScrollBar vScrollBar;

        private const int EM_GETLINECOUNT = 0xBA;
        private const int EM_LINESCROLL = 0xB6;
        private const int EM_GETFIRSTVISIBLELINE = 0xCE;
 
        private const int EM_POSFROMCHAR = 0xD6;
        private const int EM_LINEINDEX = 0xBB;

        private int _cachedLineHeight = 0;

        //private const int EM_POSFROMCHAR = 0x00D6;
        

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        //private static extern IntPtr SendMessage2(HandleRef hWnd, int msg, IntPtr wParam, ref RECT lParam);


        public ShellBoxWithScrollBar()
        {
            ShellBox = new ShellBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                BackColor = Color.FromArgb(30, 30, 30), //load this 
                ForeColor = Color.White,                //and this
                Multiline = true
            };

            vScrollBar = new DarkScrollBar
            {
                Dock = DockStyle.Right,
                Width = 12,
                Visible = false
            };

            Controls.Add(vScrollBar);
            Controls.Add(ShellBox);

            ShellBox.TextChanged += (s, e) => UpdateScrollBar();
            ShellBox.Resize += (s, e) => UpdateScrollBar();
            ShellBox.FontChanged += (s, e) => UpdateScrollBar();
            ShellBox.MouseWheel += ShellBox_MouseWheel;
            vScrollBar.ValueChanged += VScrollBar_ValueChanged;

        }

        private void UpdateScrollBar()
        {
            int lineCount = SendMessage(ShellBox.Handle, EM_GETLINECOUNT, 0, 0);
            int visibleLines = GetVisibleLinesCount();

            //int visibleLines = ShellBox.ClientSize.Height / ShellBox.Font.Height;

            //Debug.WriteLine(string.Format("linecount:{0} visablelines:{1}",
             //   lineCount, visibleLines));

            if (lineCount > visibleLines)
            {
                vScrollBar.Minimum = 0;
                vScrollBar.Maximum = lineCount - 1;
                vScrollBar.LargeChange = visibleLines;
                vScrollBar.SmallChange = 1;
                vScrollBar.Visible = true;
                SyncScrollBar();
            }
            else
            {
                vScrollBar.Visible = false;
            }
        }

        public int GetVisibleLinesCount()
        {
            // 1. Get the Y-coordinate of the first line (character index 0)
            int pos1 = (int)SendMessage(ShellBox.Handle, EM_POSFROMCHAR, 0, 0);
            int y1 = (pos1 >> 16) & 0xFFFF; // Extract HighWord (Y)

            // 2. Get the Y-coordinate of the start of the second line
            // We use the character index of the start of line 1
            int charIndexLine2 = (int)SendMessage(ShellBox.Handle, 0xBB, 1, 0); // EM_LINEINDEX for line 1
            int pos2 = (int)SendMessage(ShellBox.Handle, EM_POSFROMCHAR, charIndexLine2, 0);
            int y2 = (pos2 >> 16) & 0xFFFF;

            int lineHeight = y2 - y1;

            // 3. Fallback if the box is empty or has only 1 line
            if (lineHeight <= 0)
            {
                // Use a safe estimate that usually matches RTB's tightest packing
                lineHeight = ShellBox.Font.Height;
            }

            // 4. Divide Client height by actual rendered line height
            return ShellBox.ClientSize.Height / lineHeight;
        }

        //public int GetVisibleLinesCount()
        //{
        //    // Only calculate if the font changed or it's the first run
        //    if (_cachedLineHeight <= 0)
        //    {
        //        // Get Y of line 0
        //        int pos1 = (int)SendMessage(ShellBox.Handle, EM_POSFROMCHAR, 0, 0);

        //        // Get index of first char in line 1
        //        int charIndexLine2 = (int)SendMessage(ShellBox.Handle, EM_LINEINDEX, 1, 0);

        //        if (charIndexLine2 != -1)
        //        {
        //            int pos2 = (int)SendMessage(ShellBox.Handle, EM_POSFROMCHAR, charIndexLine2, 0);
        //            // HighWord is Y. (pos >> 16) is faster than Math functions.
        //            _cachedLineHeight = ((pos2 >> 16) & 0xFFFF) - ((pos1 >> 16) & 0xFFFF);
        //        }

        //        // Fallback if box is empty/single line
        //        if (_cachedLineHeight <= 0)
        //            _cachedLineHeight = ShellBox.Font.Height;
        //    }

        //    // ClientSize.Height is a property call; for extreme speed, 
        //    // cache this too during the Resize event.
        //    return ShellBox.ClientSize.Height / _cachedLineHeight;
        //}

        private void SyncScrollBar()
        {
            int currentTop = SendMessage(ShellBox.Handle, EM_GETFIRSTVISIBLELINE, 0, 0);
            vScrollBar.Value = Math.Min(currentTop, vScrollBar.Maximum - vScrollBar.LargeChange + 1);
        }

        private void VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            int currentTop = SendMessage(ShellBox.Handle, EM_GETFIRSTVISIBLELINE, 0, 0);
            int delta = vScrollBar.Value - currentTop;
            SendMessage(ShellBox.Handle, EM_LINESCROLL, 0, delta);
        }

        private void ShellBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!vScrollBar.Visible) return;
            int delta = -Math.Sign(e.Delta) * vScrollBar.SmallChange * 3;
            int newValue = vScrollBar.Value + delta;
            newValue = Math.Max(vScrollBar.Minimum, Math.Min(newValue, vScrollBar.Maximum - vScrollBar.LargeChange + 1));
            vScrollBar.Value = newValue;
        }
    }
}
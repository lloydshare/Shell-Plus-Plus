// *****************************************************************************
//  Copyright 2004, Stanley Teo Songling
//  You are free to use or modify it. The author will not be responsible for any 
//	damage caused by this code.
//  
//  I will feel very hornered if my name can appear in your software/source code 
//  Any question or feedback, please email zhangsongling@hotmail.com, thanks 
// 
//  Visual Studio.Net style status bar
// *****************************************************************************

// *****************************************************************************
//  Updated Lloyd Share
// *****************************************************************************

using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using CustomForm;

namespace StanleyTeo.CustomGUI
{
    [ToolboxItem(true)]
    public class DotNetStatusBar : StatusStrip
    {
        public ToolStripStatusLabel _MessageLabel;
        private ToolStripStatusLabel _PosLabel;
        private ToolStripStatusLabel _CapsLockLabel;
        private ToolStripStatusLabel _NumLockLabel;
        private ToolStripStatusLabel _InsertLabel;

        // defaults 
        public Color NormalBorderColor;
        public Color HighlightBorderColor;
        public Color HighlightBackColor;

        public void ReBackColorControls()
        {
            _PosLabel.BackColor = BackColor;
            _CapsLockLabel.BackColor = BackColor;
            _NumLockLabel.BackColor = BackColor;
            _InsertLabel.BackColor = BackColor;
        }

        private PointF _Pos;

        public PointF Pos
        {
            get => _Pos;
            set
            {
                if (_Pos != value)
                {
                    _Pos = value;
                    UpdatePosLabel();
                }
            }
        }

        public string Message
        {
            get => _MessageLabel.Text;
            set => _MessageLabel.Text = value;
        }

        public DotNetStatusBar()
        {
            _MessageLabel = new ToolStripStatusLabel { Text = "Ready", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _PosLabel = new HighlightableStatusLabel { TextAlign = ContentAlignment.MiddleLeft, Width = FontHeight * 12 };
            _CapsLockLabel = new HighlightableStatusLabel { TextAlign = ContentAlignment.MiddleCenter };
            _NumLockLabel = new HighlightableStatusLabel { TextAlign = ContentAlignment.MiddleCenter };
            _InsertLabel = new HighlightableStatusLabel { TextAlign = ContentAlignment.MiddleCenter };

            Items.AddRange(new ToolStripItem[] { _MessageLabel, _PosLabel, _CapsLockLabel, _NumLockLabel, _InsertLabel });

            //SizingGrip = false;
            Size = new Size(292, FontHeight + 5);

            UpdatePanelWidths();

            Pos = new PointF(0f, 0f);

            Application.Idle += OnIdle;
        }

        private void UpdatePanelWidths()
        {
            using var g = CreateGraphics();
            string[] aStr = { "CAP", "INS", "OVR", "NUM" };
            float fMax = 0f;
            foreach (string str in aStr)
            {
                SizeF size = g.MeasureString(str, Font);
                if (size.Width > fMax)
                    fMax = size.Width;
            }
            int width = Convert.ToInt32(fMax + 2.5f);

            _CapsLockLabel.Width = width;
            _NumLockLabel.Width = width;
            _InsertLabel.Width = width;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        private void OnIdle(object sender, EventArgs e)
        {
            bool CapsLock = (((ushort)GetKeyState(0x14)) & 0xffff) != 0;
            bool NumLock = (((ushort)GetKeyState(0x90)) & 0xffff) != 0;
            bool Insert = (((ushort)GetKeyState(0x2D)) & 0xffff) != 0;

            _CapsLockLabel.Text = CapsLock ? "CAP" : "";
            _NumLockLabel.Text = NumLock ? "NUM" : "";
            _InsertLabel.Text = Insert ? "INS" : "OVR";
        }

        private void UpdatePosLabel()
        {
            _PosLabel.Text = $"X {_Pos.X:F3}  Y {_Pos.Y:F3}";
        }
        
        public class HighlightableStatusLabel : ToolStripStatusLabel
        {
            private bool _highlighted = false;

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                if (!_highlighted)
                {
                    var ownerBar = Owner as DotNetStatusBar;
                    // Always use the current BackColor as the "original" before highlighting
                    BackColor = ownerBar?.HighlightBackColor ?? BackColor;
                    _highlighted = true;
                    Invalidate(); // Redraw to update border color
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_highlighted)
                {
                    // Restore to the current owner's originalBackColor (in case it changed)
                    var ownerBar = Owner as DotNetStatusBar;
                    BackColor = ownerBar?.BackColor ?? SystemColors.Control;
                    _highlighted = false;
                    Invalidate(); // Redraw to update border color
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var ownerBar = Owner as DotNetStatusBar;
                var borderColor = _highlighted
                    ? ownerBar?.HighlightBorderColor ?? Color.Transparent
                    : ownerBar?.NormalBorderColor ?? Color.Transparent;
                using var pen = new Pen(borderColor, 1);
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
    }
}

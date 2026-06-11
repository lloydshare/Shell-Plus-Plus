/***************************************************************************
 * File:        ShellBox.cs
 * Author:      Lloyd Share
 * Created:     11/08/2025   
 * 
 * Description:
 *   Provides the ShellBox control, a custom RichTextBox with advanced caret
 *   styling, embedded control support, and resource management features.
 *   Includes support for gradient, glow, fill styles, and custom caret rendering.
 * 
 * Copyright:   © 2025 Lloyd Share, England. All rights reserved.
 * License:     
 * 
 * References:
 *   For adding controls - TRichTextBox - Copyright@Trestan Chen,Canada. 2010.
 * 
 * Version:     1a
 ***************************************************************************/

namespace Editor.Controls
{

    #region Using
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing.Drawing2D;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using static System.Windows.Forms.LinkLabel;
    #endregion

    #region Timing Notes
    //•	Windows default is ~530 ms.Your 500 ms is fine.
    //•	Reasonable range: 200–1200 ms(faster to slower).
    //•	Typical choices:
    //•	300–400 ms: snappy
    //•	450–550 ms: standard
    //•	600–800 ms: slower / high - contrast
    #endregion

    #region Shell Properties Interface
    // Exposes key shell properties for configuration and testing
    public interface IShellProperties
    {
        int MaximumTextLength { get; set; }
        int TextLengthToBeRemoved { get; set; }
        int MaximumNoOfControl { get; set; }
        int NoOfControlToBeRemoved { get; set; }
        bool KeepShort { get; set; }
    }
    #endregion

    #region Caret Styling Property Classes
    public enum CaretVerticalAlign { Top, Middle, Bottom }
    public enum CaretGradientMode { None, Horizontal, Vertical, ForwardDiagonal, BackwardDiagonal }
    public enum FillStyle
    {
        Solid,
        Dotted,
        Striped,
        Checkerboard,
        Gradient,
        Hatch,
        Outline,
        Noise,
        Texture,
        BlinkingOutline // New style
    }
    public interface ICaretStylable
    {
        int CaretWidth { get; set; }
        int CaretHeight { get; set; }
        Color CaretColor { get; set; }
        CaretVerticalAlign CaretVAlign { get; set; }
        bool EnableBlink { get; set; }
        int BlinkIntervalMs { get; set; }
        int CaretCornerRadius { get; set; }

        bool UseGradient { get; set; }
        Color GradientStartColor { get; set; }
        Color GradientEndColor { get; set; }
        CaretGradientMode GradientMode { get; set; }

        bool UseGlow { get; set; }
        Color GlowColor { get; set; }
        int GlowRadius { get; set; }
        int GlowIntensity { get; set; }

        bool UseFillStyle { get; set; }
        FillStyle CaretFillStyle { get; set; }
        int CaretDotSpacing { get; set; } // pixels between dots
        int CaretDotRadius { get; set; }  // dot radius
        int CaretOutlinePenWidth { get; set; } // outline pen width
    }
    public sealed class CaretStyle
    {
        public int? Width { get; set; }
        public int? Height { get; set; }
        public Color? Color { get; set; }
        public CaretVerticalAlign? VAlign { get; set; }
        public bool? EnableBlink { get; set; }
        public int? BlinkIntervalMs { get; set; }
        public int? CornerRadius { get; set; }
        public bool? UseGradient { get; set; }
        public Color? GradientStartColor { get; set; }
        public Color? GradientEndColor { get; set; }
        public CaretGradientMode? GradientMode { get; set; }
        public bool? UseGlow { get; set; }
        public Color? GlowColor { get; set; }
        public int? GlowRadius { get; set; }
        public int? GlowIntensity { get; set; }
        public bool? UseFillStyle { get; set; }
        public FillStyle? FillStyle { get; set; }
        public int? DotSpacing { get; set; } // pixels between dots
        public int? DotRadius { get; set; }  // dot radius
        public int? OutlinePenWidth { get; set; }

        public void ApplyTo(ICaretStylable target)
        {
            if (Width.HasValue) target.CaretWidth = Math.Max(1, Width.Value);
            if (Height.HasValue) target.CaretHeight = Math.Max(1, Height.Value);
            if (Color.HasValue) target.CaretColor = Color.Value;
            if (VAlign.HasValue) target.CaretVAlign = VAlign.Value;
            if (EnableBlink.HasValue) target.EnableBlink = EnableBlink.Value;
            if (BlinkIntervalMs.HasValue) target.BlinkIntervalMs = Math.Max(50, BlinkIntervalMs.Value);
            if (CornerRadius.HasValue) target.CaretCornerRadius = Math.Max(0, CornerRadius.Value);
            if (UseGradient.HasValue) target.UseGradient = UseGradient.Value;
            if (GradientStartColor.HasValue) target.GradientStartColor = GradientStartColor.Value;
            if (GradientEndColor.HasValue) target.GradientEndColor = GradientEndColor.Value;
            if (GradientMode.HasValue) target.GradientMode = GradientMode.Value;
            if (UseGlow.HasValue) target.UseGlow = UseGlow.Value;
            if (GlowColor.HasValue) target.GlowColor = GlowColor.Value;
            if (GlowRadius.HasValue) target.GlowRadius = Math.Max(1, GlowRadius.Value);
            if (GlowIntensity.HasValue) target.GlowIntensity = Math.Max(1, GlowIntensity.Value);
            if (UseFillStyle.HasValue) target.UseFillStyle = UseFillStyle.Value;
            if (FillStyle.HasValue) target.CaretFillStyle = FillStyle.Value;
            if (DotSpacing.HasValue) target.CaretDotSpacing = Math.Max(1, DotSpacing.Value);
            if (DotRadius.HasValue) target.CaretDotRadius = Math.Max(1, DotRadius.Value);
            if (OutlinePenWidth.HasValue) target.CaretOutlinePenWidth = Math.Max(1, Math.Min(2, OutlinePenWidth.Value));

            if (target is Control c) c.Invalidate();
        }
    }
    #endregion

    #region Controls Property Class
    //Contains the initial position data of a control relative to the text content.
    internal class MetaInfo
    {
        int charIndex;
        public int CharIndex
        {
            get { return charIndex; }
            set { charIndex = value; }
        }

        int deltaY;
        public int DeltaY
        {
            get { return deltaY; }
            set { deltaY = value; }
        }

        Control theControl;
        public Control TheControl
        {
            get { return theControl; }
            set { theControl = value; }
        }

        public MetaInfo(Control theControl)
        {
            this.theControl = theControl;
        }
    }
    #endregion

    #region ShellBox  
    public sealed class ShellBox : RichTextBox, ICaretStylable, IShellProperties
    {
        #region Public Properties
        /// <summary>
        /// If true, added controls will automatically size to match the ShellBox client area.
        /// </summary>
        public bool AutoSizeControlsToBox { get; set; } = false;
        public int MaximumTextLength { get; set; } = 10000;
        public int TextLengthToBeRemoved { get; set; } = 3000;
        public int MaximumNoOfControl { get; set; } = 50;
        public int NoOfControlToBeRemoved { get; set; } = 20;
        public bool KeepShort { get; set; } = true;

        public int CaretWidth { get; set; } = 10;
        public int CaretHeight { get; set; } = 4;
        public Color CaretColor { get; set; } = Color.Red;
        public CaretVerticalAlign CaretVAlign { get; set; } = CaretVerticalAlign.Bottom;
        public int CaretCornerRadius { get; set; } = 0;

        public bool UseGradient { get; set; } = false;
        public Color GradientStartColor { get; set; } = Color.Red;
        public Color GradientEndColor { get; set; } = Color.DarkRed;
        public CaretGradientMode GradientMode { get; set; } = CaretGradientMode.Horizontal;

        public bool UseGlow { get; set; } = false;
        public Color GlowColor { get; set; } = Color.FromArgb(160, Color.Cyan);
        public int GlowRadius { get; set; } = 6;
        public int GlowIntensity { get; set; } = 3;

        public bool UseFillStyle { get; set; } = false;
        public FillStyle CaretFillStyle { get; set; } = FillStyle.Solid;
        public int CaretDotSpacing { get; set; } = 2; // pixels between dots
        public int CaretDotRadius { get; set; } = 1;  // dot radius
        public int CaretOutlinePenWidth
        {
            get => _caretOutlinePenWidth;
            set => _caretOutlinePenWidth = Math.Max(1, Math.Min(2, value));
        }
        private int _caretOutlinePenWidth = 2;

        public bool EnableBlink
        {
            get => _enableBlink;
            set
            {
                _enableBlink = value;
                if (Focused)
                {
                    if (_enableBlink) _blink.Start();
                    else { _blink.Stop(); _visible = true; InvalidateCaret(); }
                }
            }
        }

        public int BlinkIntervalMs
        {
            get => _blink.Interval;
            set => _blink.Interval = Math.Max(50, value);
        }

        public bool PromptPinnedToBottom { get; set; } = false;
        public bool Persistant { get; set; } = true;

       // public bool TestMode { get; set; } = false;

        /// <summary>
        /// If true, the caret will be hidden (no blinking or drawing).
        /// </summary>
        public bool CursorVisible
        {
            get => _cursorVisible;
            set
            {
                if (_cursorVisible != value)
                {
                    _cursorVisible = value;
                    if (value)
                    {
                        if (EnableBlink && CaretFillStyle != FillStyle.BlinkingOutline) _blink.Start();
                        if (CaretFillStyle == FillStyle.BlinkingOutline) _outlineBlink.Start();
                        _visible = true;
                        InvalidateCaret();
                    }
                    else
                    {
                        _blink.Stop();
                        _outlineBlink.Stop();
                        _visible = false;
                        InvalidateCaret();
                    }
                }
            }
        }
        private bool _cursorVisible = true;

        private bool _lockedResize = true;
        public bool LockedResize
        {
            get => _lockedResize;
            set => _lockedResize = value;
        }
        #endregion

        #region Private Objects
        private List<MetaInfo> ControlList = new List<MetaInfo>();
        private const int EM_GETSCROLLPOS = 0x0400 + 221;
        private const int EM_SETSCROLLPOS = 0x0400 + 222;
        private readonly System.Windows.Forms.Timer _blink;
        private readonly System.Windows.Forms.Timer _suppress;
        private readonly System.Windows.Forms.Timer _outlineBlink; //timer for BlinkingOutline
        private bool _visible = true;
        private bool _outlineThick = false; // Toggles between false (1) and true (2)

        private bool _enableBlink = true;
        private Rectangle _lastOuter = Rectangle.Empty;
        private Rectangle _lastInner = Rectangle.Empty;

        // cache handler so we can unsubscribe correctly
        private readonly Microsoft.Win32.UserPreferenceChangedEventHandler _userPrefChangedHandler;

        // --- Caret anchoring fields ---
        private int _caretAnchorIndex = 0;
        private bool _isSelectingWithMouse = false;
        #endregion

        #region Constructor
        public ShellBox()
        {
            _blink = new System.Windows.Forms.Timer
            {
                Interval = SystemInformation.CaretBlinkTime > 0 ? SystemInformation.CaretBlinkTime : 500
            };

            _blink.Tick += (_, __) =>
            {
                if (!EnableBlink) return;
                if (CaretFillStyle == FillStyle.BlinkingOutline)
                    return; // Don't toggle _visible for BlinkingOutline
                _visible = !_visible;
                InvalidateCaret(true);
            };

            _outlineBlink = new System.Windows.Forms.Timer { Interval = 500 };
            _outlineBlink.Tick += (_, __) =>
            {
                if (CaretFillStyle != FillStyle.BlinkingOutline) return;
                _outlineThick = !_outlineThick;
                InvalidateCaret(true);
            };

            _suppress = new System.Windows.Forms.Timer { Interval = 15 };
            _suppress.Tick += (_, __) => SuppressSystemCaret();

            GotFocus += (_, __) =>
            {
                _visible = true;
                if (EnableBlink && CaretFillStyle != FillStyle.BlinkingOutline) _blink.Start();
                else _blink.Stop();
                if (CaretFillStyle == FillStyle.BlinkingOutline) _outlineBlink.Start();
                else _outlineBlink.Stop();
                _suppress.Start();
                SuppressSystemCaret();
                BeginInvoke((Action)SuppressSystemCaret);
                InvalidateCaret();

                //if (Persistant)
                //    Persistor.LoadShellFromFile(this, "1");

                //if (PromptPinnedToBottom) //wrong place to call this
                //{
                //    PromptPinnedToBottom = false;
                //    InitializeWithBottomPrompt();
                //}
            };

            
                LostFocus += (_, __) =>
                {
                    //if (!TestMode)
                    //{
                    _blink.Stop();
                    _outlineBlink.Stop();
                    _suppress.Stop();
                    _visible = false;
                    InvalidateCaret();
                    //}
                    //else
                    //{
                    //    _visible = true;
                    //    CursorVisible = true;
                    //}
                    //if (Persistant)
                    //{
                    //    // Persistor.SaveShellToFile(this, "1");

                    //    if (ControlList.Count > 0)
                    //    {
                    //        Persistor.SaveAsXml(ControlList, "1.xml"); //emtpy file for some reason
                    //    }
                    //}
                };
            

            KeyDown += (s, e) =>
            {
                // If navigation or typing, update anchor
                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                {
                    _caretAnchorIndex = SelectionStart;
                }
                BeginInvoke((Action)SuppressSystemCaret); InvalidateCaret();
            };
            KeyUp += (_, __) => { BeginInvoke((Action)SuppressSystemCaret); InvalidateCaret(); };

            MouseDown += (s, e) =>
            {
                _caretAnchorIndex = SelectionStart;
                _isSelectingWithMouse = true;
                BeginInvoke((Action)SuppressSystemCaret); InvalidateCaret();
            };
            MouseUp += (s, e) =>
            {
                _isSelectingWithMouse = false;
                // Do NOT update _caretAnchorIndex here!
                BeginInvoke((Action)SuppressSystemCaret); InvalidateCaret();
            };
            MouseMove += (_, __) =>
            {
                if (!IsHandleCreated || !Focused || !_visible) return;
                // Only repaint if caret location/size changed.
                var current = GetCaretRect();
                if (UseGlow && !current.IsEmpty)
                {
                    var currentOuter = Rectangle.Inflate(current, GlowRadius, GlowRadius);
                    if (current == _lastInner && currentOuter == _lastOuter) return;
                }
                else
                {
                    if (current == _lastInner) return;
                }
                InvalidateCaret();
            };

            VScroll += (_, __) =>
            {

                BeginInvoke((Action)SuppressSystemCaret); InvalidateCaret();

                Point pt = new Point();
                SendMessage(this.Handle, EM_GETSCROLLPOS, 0, ref pt);

                foreach (MetaInfo one in ControlList)
                {
                    one.TheControl.Location = new Point(one.TheControl.Location.X, -pt.Y - one.DeltaY);
                }

            };
            HScroll += (_, __) => { BeginInvoke((Action)SuppressSystemCaret); InvalidateCaret(); };
            TextChanged += (_, __) => { BeginInvoke((Action)SuppressSystemCaret); InvalidateCaret(); };
            FontChanged += (_, __) => InvalidateCaret();
            SizeChanged += (_, __) => { CalculateDelta(); };

            _userPrefChangedHandler = (_, __) =>
            {
                int t = SystemInformation.CaretBlinkTime;
                _blink.Interval = (t > 0) ? t : _blink.Interval;
            };
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += _userPrefChangedHandler;

        }
        #endregion

        //private int CalculateVisibleLineCount()
        //{
        //    if (!IsHandleCreated) return 0; // fallback if called too early

        //    // Get the actual drawable client area height (excludes borders, scrollbars if present)
        //    int clientHeight = ClientRectangle.Height;

        //    // Font.Height is the line height (includes leading/descent)
        //    int lineHeight = Font.Height;

        //    if (lineHeight <= 0) return 0; // prevent div by zero 

        //    int visibleLines = (clientHeight / lineHeight) - 1;

        //    return visibleLines;
        //}

        private void InitializeWithBottomPrompt()
        {
            int linesToFill = CalculateVisibleLineCount();
            //linesToFill++;
            //Debug.WriteLine(string.Format("FillLines:{0}", linesToFill));

            string prompt = this.Text;
            string filler = new string('\n', linesToFill);
            this.Text = filler + prompt;

            this.SelectionStart = this.TextLength;
            this.ScrollToCaret();
        }

        #region Private Rendering Methods
        private void SuppressSystemCaret()
        {
            if (!IsHandleCreated || !Focused) return;
            if (_cursorVisible)
                HideCaret(Handle);
        }

        public int CalculateVisibleLineCount()
        {
            // 1. Get the Y-coordinate of the first line (character index 0)
            int pos1 = (int)SendMessage(Handle, EM_POSFROMCHAR, 0, 0);
            int y1 = (pos1 >> 16) & 0xFFFF; // Extract HighWord (Y)

            // 2. Get the Y-coordinate of the start of the second line
            // We use the character index of the start of line 1
            int charIndexLine2 = (int)SendMessage(Handle, 0xBB, 1, 0); // EM_LINEINDEX for line 1
            int pos2 = (int)SendMessage(Handle, EM_POSFROMCHAR, charIndexLine2, 0);
            int y2 = (pos2 >> 16) & 0xFFFF;

            int lineHeight = y2 - y1;

            // 3. Fallback if the box is empty or has only 1 line
            if (lineHeight <= 0)
            {
                // Use a safe estimate that usually matches RTB's tightest packing
                lineHeight = Font.Height;
            }

            // 4. Divide Client height by actual rendered line height
            return ClientSize.Height / lineHeight;

            //int lineIndex = GetLineFromCharIndex(SelectionStart);

            //return lineIndex;

        }

        private const int EM_GETLINECOUNT = 0xBA;

        public int GetLineCount()
        {
            return (SendMessage(Handle, EM_GETLINECOUNT, 0, 0)) - 1;
        }

        private Rectangle GetCaretRect()
        {
            int caretIndex = (SelectionLength != 0) ? _caretAnchorIndex : SelectionStart;
            Point pt = GetPositionFromCharIndex(caretIndex);
            if (pt.X == int.MaxValue || pt.Y == int.MaxValue) return Rectangle.Empty;
            int full = Font.Height;
            int h = CaretHeight > 0 ? Math.Min(CaretHeight, full) : full;
            int y = CaretVAlign switch
            {
                CaretVerticalAlign.Bottom => pt.Y + full - h,
                CaretVerticalAlign.Middle => pt.Y + (full - h) / 2,
                _ => pt.Y
            };
            return new Rectangle(pt.X, y, CaretWidth, h);
        }

        private void InvalidateCaret(bool force = false)
        {
            if (!IsHandleCreated) return;

            var inner = GetCaretRect();
            Rectangle outer = inner;
            if (UseGlow && !inner.IsEmpty)
                outer = Rectangle.Inflate(inner, GlowRadius, GlowRadius);

            // Always invalidate even if rect unchanged; required to prevent ghosting when underlying text repaints
            if (!_lastOuter.IsEmpty) Invalidate(_lastOuter);
            if (!outer.IsEmpty) Invalidate(outer);
            _lastInner = inner;
            _lastOuter = outer;
        }

        private void DrawCaretOverlay()
        {
            var rect = _lastInner.IsEmpty ? GetCaretRect() : _lastInner;
            if (rect.IsEmpty) return;
            int radius = Math.Min(CaretCornerRadius, Math.Min(rect.Width, rect.Height) / 2);
            using var g = Graphics.FromHwnd(Handle);
            RenderCaretShape(g, rect, radius);
        }

        private void RenderCaretShape(Graphics g, Rectangle r, int radius)
        {
            if (UseGlow)
                RenderGlow(g, r, radius);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (UseFillStyle)
            {
                switch (CaretFillStyle)
                {
                    case FillStyle.Dotted:
                        RenderDottedCaret(g, r, radius);
                        return;
                    case FillStyle.Striped:
                        RenderStripedCaret(g, r, radius);
                        return;
                    case FillStyle.Checkerboard:
                        RenderCheckerboardCaret(g, r, radius);
                        return;
                    case FillStyle.Gradient:
                        RenderGradientCaret(g, r, radius);
                        return;
                    case FillStyle.Hatch:
                        RenderHatchCaret(g, r, radius);
                        return;
                    case FillStyle.Outline:
                        RenderOutlineCaret(g, r, radius);
                        return;
                    case FillStyle.BlinkingOutline:
                        RenderBlinkingOutlineCaret(g, r, radius);
                        return;
                    case FillStyle.Noise:
                        RenderNoiseCaret(g, r, radius);
                        return;
                    case FillStyle.Texture:
                        RenderTextureCaret(g, r, radius);
                        return;
                }
            }

            using var brush = CreateCaretBrush(r);
            if (radius <= 0)
                g.FillRectangle(brush, r);
            else
            {
                using var path = BuildRoundedRect(r, radius);
                g.FillPath(brush, path);
            }
        }

        private void RenderBlinkingOutlineCaret(Graphics g, Rectangle r, int radius)
        {
            var outlineRect = Rectangle.Inflate(r, -1, -1);
            //using var path = BuildRoundedRect(r, radius);
            using var path = BuildRoundedRect(outlineRect, radius > 0 ? radius - 1 : 0);
            int penWidth = _outlineThick ? 2 : 1;
            using var pen = new Pen(CaretColor, penWidth);
            g.DrawPath(pen, path);
        }

        private void RenderGlow(Graphics g, Rectangle r, int radius)
        {
            int inflate = GlowRadius;
            Rectangle outer = Rectangle.Inflate(r, inflate, inflate);
            int outerRadius = Math.Min(radius + inflate, Math.Min(outer.Width, outer.Height) / 2);

            using var outerPath = BuildRoundedRect(outer, outerRadius);
            using var pgb = new PathGradientBrush(outerPath)
            {
                CenterPoint = new PointF(r.Left + r.Width / 2f, r.Top + r.Height / 2f),
                CenterColor = GlowColor,
                SurroundColors = new[] { Color.FromArgb(0, GlowColor) }
            };

            g.FillPath(pgb, outerPath);

            for (int i = 1; i < GlowIntensity; i++)
                g.FillPath(pgb, outerPath);
        }

        private void RenderDottedCaret(Graphics g, Rectangle r, int radius)
        {
            using var dotBrush = new SolidBrush(CaretColor);
            using var path = BuildRoundedRect(r, radius);
            for (int y = r.Top; y < r.Bottom; y += CaretDotSpacing)
                for (int x = r.Left; x < r.Right; x += CaretDotSpacing)
                {
                    var center = new PointF(x + CaretDotRadius, y + CaretDotRadius);
                    if (path.IsVisible(center))
                        g.FillEllipse(dotBrush, x, y, CaretDotRadius * 2, CaretDotRadius * 2);
                }
        }

        private void RenderStripedCaret(Graphics g, Rectangle r, int radius)
        {
            using var path = BuildRoundedRect(r, radius);
            g.SetClip(path);
            int stripeHeight = 3;
            bool alt = false;
            for (int y = r.Top; y < r.Bottom; y += stripeHeight)
            {
                if (alt)
                    g.FillRectangle(new SolidBrush(CaretColor), r.Left, y, r.Width, stripeHeight);
                alt = !alt;
            }
            g.ResetClip();
        }

        private void RenderCheckerboardCaret(Graphics g, Rectangle r, int radius)
        {
            using var path = BuildRoundedRect(r, radius);
            g.SetClip(path);
            int size = 4;
            for (int y = r.Top; y < r.Bottom; y += size)
                for (int x = r.Left; x < r.Right; x += size)
                {
                    if (((x / size) + (y / size)) % 2 == 0)
                        g.FillRectangle(new SolidBrush(CaretColor), x, y, size, size);
                }
            g.ResetClip();
        }

        private void RenderGradientCaret(Graphics g, Rectangle r, int radius)
        {
            using var path = BuildRoundedRect(r, radius);
            using var brush = new LinearGradientBrush(r, GradientStartColor, GradientEndColor, LinearGradientMode.Vertical);
            g.FillPath(brush, path);
        }

        private void RenderHatchCaret(Graphics g, Rectangle r, int radius)
        {
            using var path = BuildRoundedRect(r, radius);
            using var hatchBrush = new HatchBrush(HatchStyle.ForwardDiagonal, CaretColor, Color.Transparent);
            g.FillPath(hatchBrush, path);
        }

        private void RenderOutlineCaret(Graphics g, Rectangle r, int radius)
        {
            var outlineRect = Rectangle.Inflate(r, -1, -1);
            using var path = BuildRoundedRect(outlineRect, radius > 0 ? radius - 1 : 0);
            using var pen = new Pen(CaretColor, CaretOutlinePenWidth);
            g.DrawPath(pen, path);
        }

        private void RenderNoiseCaret(Graphics g, Rectangle r, int radius)
        {
            using var path = BuildRoundedRect(r, radius);
            var rand = new Random();
            using var dotBrush = new SolidBrush(CaretColor);
            for (int i = 0; i < 100; i++)
            {
                int x = rand.Next(r.Left, r.Right);
                int y = rand.Next(r.Top, r.Bottom);
                var center = new PointF(x, y);
                if (path.IsVisible(center))
                    g.FillEllipse(dotBrush, x, y, 1, 1);
            }
        }

        private void RenderTextureCaret(Graphics g, Rectangle r, int radius)
        {
            using var path = BuildRoundedRect(r, radius);

            // Option 1: Use a subtle built-in hatch style
            using var textureBrush = new HatchBrush(HatchStyle.LightUpwardDiagonal, CaretColor, Color.Transparent);
            g.FillPath(textureBrush, path);
        }

        private Brush CreateCaretBrush(Rectangle r)
        {
            if (!UseGradient || r.Width <= 1 || r.Height <= 1)
                return new SolidBrush(CaretColor);

            LinearGradientMode mode = GradientMode switch
            {
                CaretGradientMode.Horizontal => LinearGradientMode.Horizontal,
                CaretGradientMode.Vertical => LinearGradientMode.Vertical,
                CaretGradientMode.ForwardDiagonal => LinearGradientMode.ForwardDiagonal,
                CaretGradientMode.BackwardDiagonal => LinearGradientMode.BackwardDiagonal,
                _ => LinearGradientMode.Horizontal
            };
            try
            {
                return new LinearGradientBrush(r, GradientStartColor, GradientEndColor, mode);
            }
            catch
            {
                return new SolidBrush(CaretColor);
            }
        }

        private static GraphicsPath BuildRoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(r);
                return path;
            }

            int d = radius * 2;
            int arcW = Math.Min(d, r.Width);
            int arcH = Math.Min(d, r.Height);

            // Top-left arc
            path.AddArc(r.Left, r.Top, arcW, arcH, 180, 90);
            // Top-right arc
            path.AddArc(r.Right - arcW, r.Top, arcW, arcH, 270, 90);
            // Bottom-right arc
            path.AddArc(r.Right - arcW, r.Bottom - arcH, arcW, arcH, 0, 90);
            // Bottom-left arc
            path.AddArc(r.Left, r.Bottom - arcH, arcW, arcH, 90, 90);

            path.CloseFigure();
            return path;

            //var path = new GraphicsPath();
            //if (radius <= 0)
            //{
            //    path.AddRectangle(r);
            //    return path;
            //}

            //int d = radius * 2;
            //int arcW = Math.Min(d, r.Width);
            //int arcH = Math.Min(d, r.Height);

            //if (arcH >= r.Height && r.Width > r.Height)
            //{
            //    int h = r.Height;
            //    path.AddArc(r.Left, r.Top, h, h, 90, 180);
            //    path.AddArc(r.Right - h, r.Top, h, h, 270, 180);
            //    path.CloseFigure();
            //    return path;
            //}
            //if (arcW >= r.Width && r.Height > r.Width)
            //{
            //    int w = r.Width;
            //    path.AddArc(r.Left, r.Top, w, w, 180, 180);
            //    path.AddArc(r.Left, r.Bottom - w, w, w, 0, 180);
            //    path.CloseFigure();
            //    return path;
            //}

            //Rectangle tl = new Rectangle(r.Left, r.Top, arcW, arcH);
            //Rectangle tr = new Rectangle(r.Right - arcW, r.Top, arcW, arcH);
            //Rectangle br = new Rectangle(r.Right - arcW, r.Bottom - arcH, arcW, arcH);
            //Rectangle bl = new Rectangle(r.Left, r.Bottom - arcH, arcW, arcH);

            //path.StartFigure();
            //path.AddArc(tl, 180, 90);
            //path.AddArc(tr, 270, 90);
            //path.AddArc(br, 0, 90);
            //path.AddArc(bl, 90, 90);
            //path.CloseFigure();
            //return path;
        }

        #endregion

        #region Control Handlers
        /// <summary>
        /// Add any control to the ShellBox
        /// </summary>
        /// <param name="oneControl">Control Object</param>
        public void AddControl(Control oneControl)
        {
            if (AutoSizeControlsToBox)
            {
                //oneControl.Location = new Point(0, 0);

                Size adjustedSize = Size;

                // Check if vertical scroll bar is visible
                if ((this.ScrollBars == RichTextBoxScrollBars.Vertical || this.ScrollBars == RichTextBoxScrollBars.Both) ||
                    (this.ScrollBars == RichTextBoxScrollBars.ForcedVertical || this.ScrollBars == RichTextBoxScrollBars.ForcedBoth))
                {
                    adjustedSize.Width -= SystemInformation.VerticalScrollBarWidth - 11;
                }

                // Check if horizontal scroll bar is visible
                if ((this.ScrollBars == RichTextBoxScrollBars.Horizontal || this.ScrollBars == RichTextBoxScrollBars.Both) ||
                    (this.ScrollBars == RichTextBoxScrollBars.ForcedHorizontal || this.ScrollBars == RichTextBoxScrollBars.ForcedBoth))
                {
                    adjustedSize.Height -= SystemInformation.HorizontalScrollBarHeight;
                }

                //oneControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                //oneControl.Anchor = AnchorStyles.Left | AnchorStyles.Right;

                oneControl.Size = adjustedSize;

                if (oneControl.GetType() == typeof(PictureBox))
                {
                    ((PictureBox)oneControl).SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }

            // Obtain the initial metadata.
            MetaInfo one = new MetaInfo(oneControl);
            base.Controls.Add(oneControl);
            one.CharIndex = this.TextLength;
            one.TheControl.Location = this.GetPositionFromCharIndex(one.CharIndex);
            one.DeltaY = this.GetPositionFromCharIndex(0).Y - one.TheControl.Location.Y;
            ControlList.Add(one);

            //"Push" the text away from the space occupied by the control.
            do
            {
                this.AppendText(Environment.NewLine);
            }
            while (this.GetPositionFromCharIndex(this.TextLength).Y < (oneControl.Location.Y + oneControl.Height));

            RemoveSome();
            AutoScroll();
        }

        public void AutoScroll()
        {
            // move caret to end then scroll
            this.SelectionStart = this.TextLength - 1;
            this.ScrollToCaret();

        }

        private void RemoveSome()
        {
            //Remove some text and control if too many, to release system resources and improve performance.
            if (!KeepShort)
            {
                return;
            }

            int texttoRemove = 0;
            int imgtoRemove = 0;
            try
            {
                if (this.TextLength > MaximumTextLength)
                {
                    texttoRemove = TextLengthToBeRemoved;
                    this.Text = this.Text.Substring(texttoRemove);
                    texttoRemove += this.Text.IndexOf("\n");
                    if (texttoRemove > TextLengthToBeRemoved)
                    {
                        this.Text = this.Text.Substring(texttoRemove - TextLengthToBeRemoved);
                    }

                    foreach (MetaInfo oldone in ControlList)
                    {
                        if (oldone.CharIndex < texttoRemove)
                        {
                            imgtoRemove++;
                        }
                        else
                        {
                            oldone.CharIndex -= texttoRemove;
                        }
                    }

                    for (int i = 0; i < imgtoRemove; i++)
                    {
                        this.Controls[0].Dispose();
                        ControlList.RemoveAt(0);
                    }
                    //need to calculate the metadata again.
                    CalculateDelta();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            try
            {
                if (ControlList.Count > MaximumNoOfControl)
                {
                    imgtoRemove = NoOfControlToBeRemoved;
                    for (int i = 0; i < imgtoRemove; i++)
                    {
                        texttoRemove = ControlList[0].CharIndex;
                        ControlList.RemoveAt(0);
                        this.Controls[0].Dispose();
                    }
                    this.Text = this.Text.Substring(texttoRemove);
                    foreach (MetaInfo oldone in ControlList)
                    {
                        oldone.CharIndex -= texttoRemove;
                    }
                    //need to calculate the metadata again.
                    CalculateDelta();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void CalculateDelta()
        {
            foreach (MetaInfo one in ControlList)
            {
                one.TheControl.Location = this.GetPositionFromCharIndex(one.CharIndex);
                one.DeltaY = this.GetPositionFromCharIndex(0).Y - one.TheControl.Location.Y;
            }
        }

        #endregion

        #region Winforms Overrides
        protected override void WndProc(ref Message m)
        {
            //const int WM_SETFOCUS = 0x0007;

            const int WM_PASTE = 0x0302;
            const int WM_PAINT = 0x000F;

            if (m.Msg == WM_PASTE)
            {
                PastePlainText();
                return;
            }

            base.WndProc(ref m);

            //if (m.Msg == WM_SETFOCUS)
            //{
            //    SuppressSystemCaret();
            //}

            SuppressSystemCaret();
            if (m.Msg == WM_PAINT && Focused && _visible && _cursorVisible)
                DrawCaretOverlay();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Intercept Ctrl+V and Shift+Insert for plain text paste
            if ((e.Control && e.KeyCode == Keys.V) || (e.Shift && e.KeyCode == Keys.Insert))
            {
                PastePlainText();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private void PastePlainText()
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText(TextDataFormat.Text); // Always plain text
                int selStart = SelectionStart;
                int selLength = SelectionLength;
                // Remove selected text
                if (selLength > 0)
                    SelectedText = string.Empty;
                // Insert plain text
                int insertPos = SelectionStart;
                SelectedText = text;
                // Select the newly inserted text
                Select(insertPos, text.Length);
                SelectionFont = this.Font;
                SelectionColor = this.ForeColor;
                // Move caret to end of inserted text
                Select(insertPos + text.Length, 0);
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SuppressSystemCaret();

            //if (Persistant)
            //{
            //    Persistor.LoadShellFromFile(this, "1");
            //    AutoScroll();
            //}
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _blink?.Stop();
            _suppress?.Stop();
            _outlineBlink?.Stop();

            //if (Persistant)
            //{ 
            //    Persistor.SaveShellToFile(this, "1");

            //    if (ControlList.Count > 0)
            //    {
            //        Persistor.SaveAsXml(ControlList, "1.xml"); //emtpy file for some reason
            //    }
            //}


            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _blink?.Dispose();
                _suppress?.Dispose();
                _outlineBlink?.Dispose();
                Microsoft.Win32.SystemEvents.UserPreferenceChanged -= _userPrefChangedHandler;
            }
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            //if (!LockedResize)
            //    return;

            //// Use the current font size as the base for scaling
            //float baseFontSize = Font.Size;

            //// Calculate scale based on the change in control size
            //float scaleW = (float)ClientSize.Width / baseFontSize;
            //float scaleH = (float)ClientSize.Height / baseFontSize;
            //float scale = Math.Min(scaleW, scaleH);
            ////Debug.WriteLine(ClientSize.Width);
            ////Debug.WriteLine(Width);
            ////Debug.WriteLine(scaleW);

            //float newFontSize = Math.Max(6f, baseFontSize * scale);

           
            //Debug.WriteLine(newFontSize);

            //if (Math.Abs(Font.Size - newFontSize) > 0.1f)
            //{
            //    var newFont = new Font(Font.FontFamily, newFontSize, Font.Style);
            //    Font = newFont;

            //    if (TextLength > 0)
            //    {
            //        int selStart = SelectionStart;
            //        int selLen = SelectionLength;
            //        bool wasReadOnly = ReadOnly;
            //        if (wasReadOnly) ReadOnly = false;

            //        int i = 0;
            //        while (i < TextLength)
            //        {
            //            Select(i, 1);
            //            var currentFont = SelectionFont;
            //            if (currentFont != null && Math.Abs(currentFont.Size - newFontSize) > 0.1f)
            //            {
            //                SelectionFont = new Font(currentFont.FontFamily, newFontSize, currentFont.Style);
            //            }
            //            i += SelectionLength > 0 ? SelectionLength : 1;
            //        }
            //        Select(selStart, selLen);
            //        if (wasReadOnly) ReadOnly = true;
            //    }
            //}
        }
        #endregion

        #region Win32
        [DllImport("user32.dll")] private static extern bool HideCaret(IntPtr hWnd);
        //[DllImport("user32.dll")] private static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int w, int h);
        //[DllImport("user32.dll")] private static extern bool ShowCaret(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hwnd, Int32 wMsg, Int32 wParam, ref Point pt);
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int EM_POSFROMCHAR = 0xD6;
        private const int EM_LINEINDEX = 0xBB;
        #endregion
    }
    #endregion
}
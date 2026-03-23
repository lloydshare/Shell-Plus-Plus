using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Editor.Controls
{
    [ToolboxItem(true)]
    [DesignerCategory("UserControl")]
    public class DarkModeTextBox : UserControl
    {
        public CustomTextBox innerTextBox;
        private DarkScrollBar vScrollBar;

        // P/Invoke for TextBox scrolling
        private const int EM_GETLINECOUNT = 0xBA;
        private const int EM_LINESCROLL = 0xB6;
        private const int EM_GETFIRSTVISIBLELINE = 0xCE;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public DarkModeTextBox()
        {
            InitializeComponents();

            // Default properties
            innerTextBox.Multiline = true;
            innerTextBox.ScrollBars = RichTextBoxScrollBars.None;
            innerTextBox.BackColor = Color.FromArgb(30, 30, 30);
            innerTextBox.ForeColor = Color.White;
            innerTextBox.BorderStyle = BorderStyle.FixedSingle;

            vScrollBar.Visible = false;

            // Events
            innerTextBox.TextChanged += (s, e) => UpdateScrollBar();
            innerTextBox.Resize += (s, e) => UpdateScrollBar();
            innerTextBox.FontChanged += (s, e) => UpdateScrollBar();
            innerTextBox.MouseWheel += InnerTextBox_MouseWheel;

            vScrollBar.ValueChanged += VScrollBar_ValueChanged;
        }

        private void InitializeComponents()
        {
            innerTextBox = new CustomTextBox(this)
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, 0),
                Margin = new Padding(0),
                Size = this.Size
            };

            vScrollBar = new DarkScrollBar
            {
                Dock = DockStyle.Right,
                Width = 12,
                Visible = false
            };

            this.Controls.Add(vScrollBar);
            this.Controls.Add(innerTextBox);
        }

        internal void SyncScrollBar()
        {
            if (DesignMode || !vScrollBar.Visible) return;

            int currentTop = SendMessage(innerTextBox.Handle, EM_GETFIRSTVISIBLELINE, 0, 0);
            vScrollBar.Value = Math.Min(currentTop, vScrollBar.Maximum - vScrollBar.LargeChange + 1);
        }

        private void UpdateScrollBar()
        {
            if (DesignMode || !innerTextBox.Multiline) return;

            int lineCount = SendMessage(innerTextBox.Handle, EM_GETLINECOUNT, 0, 0);
            int visibleLines = innerTextBox.ClientSize.Height / innerTextBox.Font.Height;

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

        private void VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            int currentTop = SendMessage(innerTextBox.Handle, EM_GETFIRSTVISIBLELINE, 0, 0);
            int delta = vScrollBar.Value - currentTop;
            SendMessage(innerTextBox.Handle, EM_LINESCROLL, 0, delta);
        }

        private void InnerTextBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!vScrollBar.Visible) return;

            int delta = -Math.Sign(e.Delta) * vScrollBar.SmallChange * 3;
            int newValue = vScrollBar.Value + delta;
            newValue = Math.Max(vScrollBar.Minimum, Math.Min(newValue, vScrollBar.Maximum - vScrollBar.LargeChange + 1));
            vScrollBar.Value = newValue;
        }

        // Exposed properties
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get => innerTextBox.Text;
            set => innerTextBox.Text = value;
        }

        [Browsable(true)]
        public bool Multiline
        {
            get => innerTextBox.Multiline;
            set
            {
                innerTextBox.Multiline = value;
                UpdateScrollBar();
            }
        }

        [Browsable(true)]
        public bool ReadOnly
        {
            get => innerTextBox.ReadOnly;
            set => innerTextBox.ReadOnly = value;
        }

        [Browsable(true)]
        public bool WordWrap
        {
            get => innerTextBox.WordWrap;
            set
            {
                innerTextBox.WordWrap = value;
                UpdateScrollBar();
            }
        }

        // Scrollbar colors
        [Category("Appearance")]
        [Description("The background color of the scrollbar.")]
        public Color ScrollBarBackgroundColor
        {
            get => vScrollBar.ScrollBarBackgroundColor;
            set => vScrollBar.ScrollBarBackgroundColor = value;
        }

        [Category("Appearance")]
        [Description("The thumb color of the scrollbar.")]
        public Color ScrollBarThumbColor
        {
            get => vScrollBar.ScrollBarThumbColor;
            set => vScrollBar.ScrollBarThumbColor = value;
        }

        [Category("Appearance")]
        [Description("The hover color of the scrollbar thumb.")]
        public Color ScrollBarHoverColor
        {
            get => vScrollBar.ScrollBarHoverColor;
            set => vScrollBar.ScrollBarHoverColor = value;
        }

        [Category("Appearance")]
        [Description("The border color of the scrollbar.")]
        public Color ScrollBarBorderColor
        {
            get => vScrollBar.ScrollBarBorderColor;
            set => vScrollBar.ScrollBarBorderColor = value;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            innerTextBox.Font = Font;
            UpdateScrollBar();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            innerTextBox.Focus();
        }
    }

    public class CustomTextBox :  RichTextBox //TextBox
    {
        private readonly DarkModeTextBox _parent;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;

        public CustomTextBox(DarkModeTextBox parent)
        {
            _parent = parent;
           // this.SetStyle(ControlStyles.UserPaint, true); // REQUIRED
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                _parent.SyncScrollBar();
            }
        }
    }

    public class DarkScrollBar : Control
    {
        private const int ArrowHeight = 10;

        // Properties
        private int _minimum = 0;
        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                Invalidate();
            }
        }

        private int _maximum = 100;
        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                Invalidate();
            }
        }

        private int _smallChange = 1;
        public int SmallChange
        {
            get => _smallChange;
            set => _smallChange = value;
        }

        private int _largeChange = 10;
        public int LargeChange
        {
            get => _largeChange;
            set
            {
                _largeChange = value;
                Invalidate();
            }
        }

        private int _value = 0;
        public int Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                _value = Math.Clamp(value, Minimum, Maximum - LargeChange + 1);
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;

        // Colors
        private Color _scrollBarBackground = Color.FromArgb(64, 64, 64, 64);
        public Color ScrollBarBackgroundColor
        {
            get => _scrollBarBackground;
            set
            {
                _scrollBarBackground = value;
                Invalidate();
            }
        }

        private Color _scrollBarThumb = Color.FromArgb(64, 128, 128, 128);
        public Color ScrollBarThumbColor
        {
            get => _scrollBarThumb;
            set
            {
                _scrollBarThumb = value;
                Invalidate();
            }
        }

        private Color _scrollBarHover = Color.FromArgb(176, 176, 176);
        public Color ScrollBarHoverColor
        {
            get => _scrollBarHover;
            set
            {
                _scrollBarHover = value;
                Invalidate();
            }
        }

        private Color _scrollBarBorder = Color.FromArgb(0, 0, 0, 0);
        public Color ScrollBarBorderColor
        {
            get => _scrollBarBorder;
            set
            {
                _scrollBarBorder = value;
                Invalidate();
            }
        }

        // State
        private bool _isHovering = false;
        private bool _isDragging = false;
        private int _dragOffset;
        private System.Windows.Forms.Timer _repeatTimer;
        private int _repeatDirection = 0;
        private System.Windows.Forms.Timer _animationTimer;
        private float _opacity = 0.4f;
        private float _targetOpacity = 0.4f;
        private const float OpacityStep = 0.05f;

        private bool _hoverUpArrow = false;
        private bool _hoverDownArrow = false;
        private bool _hoverThumb = false;

        public DarkScrollBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Width = 12;

            _repeatTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _repeatTimer.Tick += RepeatTimer_Tick;

            _animationTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Height < 2 * ArrowHeight + 20)
            {
                return;
            }

            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background
            using (var bgBrush = new SolidBrush(ApplyOpacity(_scrollBarBackground)))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Up arrow
            Rectangle upRect = new Rectangle(0, 0, Width, ArrowHeight);
            Color upColor = (_hoverUpArrow || _isHovering) ? _scrollBarHover : ApplyOpacity(_scrollBarThumb);
            DrawArrow(g, upRect, true, upColor);

            // Down arrow
            Rectangle downRect = new Rectangle(0, Height - ArrowHeight, Width, ArrowHeight);
            Color downColor = (_hoverDownArrow || _isHovering) ? _scrollBarHover : ApplyOpacity(_scrollBarThumb);
            DrawArrow(g, downRect, false, downColor);

            // Thumb
            var thumbRect = GetThumbRectangle();
            if (!thumbRect.IsEmpty)
            {
                Color thumbColor = _isDragging || _hoverThumb ? _scrollBarHover : ApplyOpacity(_scrollBarThumb);
                using (var thumbBrush = new SolidBrush(thumbColor))
                {
                    float radius = 4f;
                    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(thumbRect.Left, thumbRect.Top, radius * 2, radius * 2, 180, 90);
                    path.AddArc(thumbRect.Right - radius * 2, thumbRect.Top, radius * 2, radius * 2, 270, 90);
                    path.AddArc(thumbRect.Right - radius * 2, thumbRect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(thumbRect.Left, thumbRect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseFigure();
                    g.FillPath(thumbBrush, path);
                }
            }
        }

        private void DrawArrow(Graphics g, Rectangle rect, bool isUp, Color color)
        {
            using (var brush = new SolidBrush(color))
            {
                Point[] points;
                int midX = rect.Width / 2;
                int arrowSize = 6;
                int offsetY = (rect.Height - arrowSize) / 2;

                if (isUp)
                {
                    points = new Point[]
                    {
                        new Point(midX - arrowSize / 2, rect.Bottom - offsetY - 1),
                        new Point(midX + arrowSize / 2, rect.Bottom - offsetY - 1),
                        new Point(midX, rect.Top + offsetY)
                    };
                }
                else
                {
                    points = new Point[]
                    {
                        new Point(midX - arrowSize / 2, rect.Top + offsetY),
                        new Point(midX + arrowSize / 2, rect.Top + offsetY),
                        new Point(midX, rect.Bottom - offsetY - 1)
                    };
                }

                g.FillPolygon(brush, points);
            }
        }

        private Color ApplyOpacity(Color color)
        {
            return Color.FromArgb((int)(_opacity * color.A), color.R, color.G, color.B);
        }

        private Rectangle GetThumbRectangle()
        {
            if (Maximum - Minimum + 1 <= LargeChange || Height < 2 * ArrowHeight + 20) return Rectangle.Empty;

            float trackHeight = Height - 2 * ArrowHeight;
            float thumbHeight = Math.Max(20f, (LargeChange / (float)(Maximum - Minimum + 1)) * trackHeight);
            float availableTrack = trackHeight - thumbHeight;
            float posRatio = (Value - Minimum) / (float)(Maximum - Minimum - LargeChange + 1);
            float thumbPos = posRatio * availableTrack;

            thumbPos = Math.Max(0, Math.Min(thumbPos, trackHeight - thumbHeight));

            return new Rectangle(2, ArrowHeight + (int)thumbPos, Width - 4, (int)thumbHeight);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovering = true;
            _targetOpacity = 1.0f;
            _animationTimer.Start();
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovering = false;
            _hoverUpArrow = false;
            _hoverDownArrow = false;
            _hoverThumb = false;
            Cursor = Cursors.Default;
            if (!_isDragging)
            {
                _targetOpacity = 0.4f;
                _animationTimer.Start();
            }
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool prevHoverUp = _hoverUpArrow;
            bool prevHoverDown = _hoverDownArrow;
            bool prevHoverThumb = _hoverThumb;

            var thumbRect = GetThumbRectangle();
            _hoverUpArrow = e.Y < ArrowHeight;
            _hoverDownArrow = e.Y > Height - ArrowHeight;
            _hoverThumb = thumbRect.Contains(e.Location);

            if (prevHoverUp != _hoverUpArrow || prevHoverDown != _hoverDownArrow || prevHoverThumb != _hoverThumb)
            {
                Invalidate();
            }

            if (_isDragging)
            {
                float trackHeight = Height - 2 * ArrowHeight;
                float thumbHeight = Math.Max(20f, (LargeChange / (float)(Maximum - Minimum + 1)) * trackHeight);
                float availableTrack = trackHeight - thumbHeight;
                float trackY = e.Y - ArrowHeight - _dragOffset;
                float posRatio = Math.Max(0, Math.Min(1, trackY / availableTrack));
                int maxValue = Maximum - LargeChange + 1;
                Value = Minimum + (int)(posRatio * (maxValue - Minimum));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            var thumbRect = GetThumbRectangle();

            int change = 0;

            if (_hoverUpArrow)
            {
                _repeatDirection = -1;
                change = SmallChange;
            }
            else if (_hoverDownArrow)
            {
                _repeatDirection = 1;
                change = SmallChange;
            }
            else if (thumbRect.Contains(e.Location))
            {
                _isDragging = true;
                _dragOffset = e.Y - thumbRect.Top;
                Capture = true;
            }
            else if (!thumbRect.IsEmpty)
            {
                _repeatDirection = e.Y < thumbRect.Top + ArrowHeight ? -1 : 1;
                change = LargeChange;
            }

            if (change != 0)
            {
                ChangeValue(_repeatDirection * change);
                _repeatTimer.Start();
            }

            _targetOpacity = 1.0f;
            _animationTimer.Start();
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            if (_isDragging)
            {
                _isDragging = false;
                Capture = false;
                _targetOpacity = 0.4f;
                _animationTimer.Start();
            }

            _repeatTimer.Stop();
            _repeatDirection = 0;
            Invalidate();
        }

        private void RepeatTimer_Tick(object sender, EventArgs e)
        {
            if (_repeatDirection != 0)
            {
                int change = (_hoverUpArrow || _hoverDownArrow) ? SmallChange : LargeChange;
                ChangeValue(_repeatDirection * change);
                Invalidate();
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (Math.Abs(_opacity - _targetOpacity) > OpacityStep)
            {
                if (_opacity < _targetOpacity)
                {
                    _opacity += OpacityStep;
                }
                else
                {
                    _opacity -= OpacityStep;
                }
                Invalidate();
            }
            else
            {
                _opacity = _targetOpacity;
                _animationTimer.Stop();
                Invalidate();
            }
        }

        private void ChangeValue(int delta)
        {
            Value += delta;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repeatTimer?.Dispose();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

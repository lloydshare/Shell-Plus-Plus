#region Using Namespaces
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
#pragma warning disable CS8765
#endregion

namespace CustomForm
{
    public partial class Skin : Form, ISkin
    {
        #region Public Properties
        [Browsable(false)]
        [Description("Singleton skin manager instance that provides theme colors and brushes.")]
        public SkinManager SkinManager => SkinManager.Instance;

        [Category("Layout")]
        [Description("When true, the window can be resized using the custom border.")]
        public bool Sizable { get; set; }

        [Category("Appearance")]
        [Description("Enables a blurred/acrylic background (system backdrop on Windows11, legacy accent blur otherwise).")]
        public bool BlurredBackgound
        {
            get => _showBlurredBackgound;
            set
            {
                if (_showBlurredBackgound == value) return;
                _showBlurredBackgound = value;

                if (IsHandleCreated)
                {
                    if (value)
                    {
                        // Only attempt enabling if not already enabled
                        if (!TryEnableSystemBackdrop(SystemBackdropType))
                            TryEnableLegacyBlur();
                    }
                    else
                    {
                        DisableAccent();
                    }

                    // Use RefreshChrome so layered rounded mode updates immediately (Invalidate alone is slower there)
                    RefreshChrome();
                }
            }
        }

        [Category("Appearance")]
        [Description("Overlay opacity (0-255) used over the blur when the theme is Dark. Higher = darker.")]
        public byte BlurredOpacityDark
        {
            get => _blurredOpacityDark;
            set
            {
                if (_blurredOpacityDark == value) return; // no change
                _blurredOpacityDark = value;
                // If blur is active we need to repaint the overlay; use fast path when possible
                if (_showBlurredBackgound && IsHandleCreated)
                {
                    RefreshChrome(); // ensures layered bitmap updates when rounded corners are on
                }
            }
        } // ~88% opaque, still transparent

        [Category("Appearance")]
        [Description("Overlay opacity (0-255) used over the blur when the theme is Light. Higher = darker.")]
        public byte BlurredOpacityLight
        {
            get => _blurredOpacityLight;
            set
            {
                if (_blurredOpacityLight == value) return;
                _blurredOpacityLight = value;
                if (_showBlurredBackgound && IsHandleCreated)
                {
                    RefreshChrome();
                }
            }
        } // ~55% opaque

        [Category("Appearance")]
        [Description("Applies rounded corners on pre-Windows11 systems. Windows11 uses system rounding.")]
        public bool RoundedCorners
        {
            get => _isRoundedCorners;
            set
            {
                if (_isRoundedCorners == value) return;
                _isRoundedCorners = value;

                if (IsHandleCreated && !DesignMode)
                {
                    // Always reapply DWM setting when toggling
                    if (isWindows11OrLater)
                    {
                        DisableDwmCorner();
                    }

                    // Only recreate handle if using layered window (pre-Win11 or forced)
                    if (!isWindows11OrLater || IsLayeredRounded)
                    {
                        RecreateHandle();
                        this.BringToFront();
                    }

                    // Ensure base opacity remains 100% in layered mode to avoid conflicts
                    if (IsLayeredRounded && base.Opacity != 1.0)
                        base.Opacity = 1.0;

                    // Rounded state changed -> invalidate caches
                    InvalidateCaches();
                    ApplyRoundedRegion();
                    RefreshChrome(); // ensure redraw uses correct path immediately
                }
                if (DesignMode)
                {
                    InvalidateCaches();
                    ApplyRoundedRegion();
                    RefreshChrome();
                }
            }
        }

        [Category("Appearance")]
        [Description("Applies radius of the rounded corners")]
        public int CornerRadius
        {
            get => DefaultCornerRadius;
            set
            {
                if (DefaultCornerRadius == value) return;
                DefaultCornerRadius = value;
                InvalidateCaches();
                ApplyRoundedRegion();
                RefreshChrome();
            }
        }

        [Category("Appearance")]
        [Description("Shows a theme toggle button (Light/Dark) in the title bar.")]
        public bool ThemeButton
        {
            get => _showThemeButton;
            set
            {
                if (_showThemeButton == value) return;
                _showThemeButton = value;
                if (IsHandleCreated) RefreshChrome();
            }
        }

        [Category("Appearance")]
        [Description("When true, draws the title bar without the primary background fill (transparent).")]
        public bool BlurredTitleBar
        {
            get => _blurredTitleBar;
            set
            {
                if (_blurredTitleBar == value) return;
                _blurredTitleBar = value;
                if (IsHandleCreated) RefreshChrome();
            }
        }

        [Category("Appearance")]
        [Description("Current application theme (Light or Dark).")]
        [DefaultValue(SkinManager.Themes.LIGHT)]
        public SkinManager.Themes Theme
        {
            get => SkinManager.Theme;
            set
            {
                if (SkinManager.Theme == value) return;
                SkinManager.Theme = value;
                if (IsHandleCreated) RefreshChrome();
            }
        }
        // Ensure the designer knows when to serialize/reset Theme
        public bool ShouldSerializeTheme() => Theme != SkinManager.Themes.LIGHT;
        public void ResetTheme() => Theme = SkinManager.Themes.LIGHT;

        [Category("Appearance")]
        [Description("Show a drop shadow")]
        public bool ShowDropShadow
        {
            get => _showDropShadow;
            set
            {
                if (_showDropShadow == value) return;
                _showDropShadow = value;
                if (IsHandleCreated && !DesignMode)
                {
                    UpdateDropShadow();
                }
            }
        }

        [Category("Appearance")]
        [Description("Enables or Disables the context menu on right-click.")]
        [DefaultValue(true)]
        public bool SystemMenuEnabled { get; set; } = true;

        [Category("Appearance")]
        [Description("Use a themed context menu for the system menu on right-click.")]
        [DefaultValue(true)]
        public bool ThemedSystemMenu { get; set; } = true;

        [Description("Text displayed in the window title.")]
        public override string Text
        {
            get => base.Text;
            set { base.Text = value; Invalidate(); }
        }

        [Description("Gets or sets the current window state (Normal, Minimized, Maximized).")]
        public new FormWindowState WindowState
        {
            get => base.WindowState;
            set => base.WindowState = value;
        }

        [Description("Gets or sets the form border style. The skin expects None for custom chrome.")]
        public new FormBorderStyle FormBorderStyle
        {
            get => base.FormBorderStyle;
            set => base.FormBorderStyle = value;
        }

        [Browsable(false)]
        [Description("Client area available for user content (excludes the custom title bar).")]
        public Rectangle UserArea =>
            new Rectangle(ClientRectangle.X, ClientRectangle.Y + STATUS_BAR_HEIGHT, ClientSize.Width, ClientSize.Height - STATUS_BAR_HEIGHT);

        [Browsable(false)]
        [Description("True when the window is maximized; setting toggles between Maximized and Normal.")]
        public bool Maximized
        {
            get => WindowState == FormWindowState.Maximized;
            set
            {
                if (!MaximizeBox || !ControlBox) return;
                WindowState = value ? FormWindowState.Maximized : FormWindowState.Normal;
            }
        }

        [Browsable(false)]
        [Description("Indicates whether the window is currently the active foreground window (affects border color).")]
        public bool isActive { get; set; }

        [Category("Data")]
        [Description("Indicates whether the window is the main form, for changing main properties/settings")]
        public bool isMain { get; set; } = false;

        [Category("Data")]
        [Description("Indicates basic form settings & properties are saved and loaded")]
        public bool Persistance { get; set; } = false;

        // Intercept and alpha-blend Opacity when using layered rounded mode (pre-Windows 11)
        [Category("Appearance")]
        [Description("Overall opacity of the window. In layered rounded mode, this is applied via alpha blending to preserve rounded corners.")]
        public new double Opacity
        {
            get => IsLayeredRounded ? _customOpacity : base.Opacity;
            set
            {
                double clamped = Math.Clamp(value, 0.0, 1.0);
                if (IsLayeredRounded)
                {
                    if (Math.Abs(_customOpacity - clamped) < 0.0001) return;
                    _customOpacity = clamped;
                    if (base.Opacity != 1.0) base.Opacity = 1.0; // ensure OS doesn't override the layered bitmap
                    RefreshChrome();
                }
                else
                {
                    base.Opacity = clamped;
                }
            }
        }

        [Category("Appearance")]
        [Description("Specifies the DWM system backdrop type (Mica, Acrylic, etc) for Windows 11+.")]
        public DWM_SYSTEMBACKDROP_TYPE SystemBackdropType { get; set; } = DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW;

        #endregion

        #region Private Objects
        private bool _useLayeredRounding = false; // keep child controls interactive by default
        private bool IsLayeredRounded => _isRoundedCorners && !isWindows11OrLater && _useLayeredRounding;
        private bool _showBlurredBackgound;
        private bool _showThemeButton = true;
        private bool _blurredTitleBar;
        private const int Windows11Build = 22000;      // First Win11 public build
        private int DefaultCornerRadius = 9;
        private readonly bool isWindows11OrLater;
        private bool _isRoundedCorners;
        private const int WM_SYSCOMMAND = 0x112;
        private const int TPM_LEFTALIGN = 0x0000;
        private const int TPM_RETURNCMD = 0x0100;
        private const int STATUS_BAR_BUTTON_WIDTH = 24;
        private int STATUS_BAR_HEIGHT = 24;
        private const int BORDER_WIDTH = 7;
        private const int PADDING_MINIMUM = 3;
        private readonly Cursor[] _resizeCursors = { Cursors.SizeNESW, Cursors.SizeWE, Cursors.SizeNWSE, Cursors.SizeWE, Cursors.SizeNS };
        private ResizeDirection _resizeDir;
        private bool _showDropShadow;
        private Rectangle _statusBarBounds => new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientSize.Width, STATUS_BAR_HEIGHT);
        private Rectangle _minButtonBounds => new Rectangle(ClientSize.Width - 3 * STATUS_BAR_BUTTON_WIDTH, ClientRectangle.Y, STATUS_BAR_BUTTON_WIDTH, STATUS_BAR_HEIGHT);
        private Rectangle _maxButtonBounds => new Rectangle(ClientSize.Width - 2 * STATUS_BAR_BUTTON_WIDTH, ClientRectangle.Y, STATUS_BAR_BUTTON_WIDTH, STATUS_BAR_HEIGHT);
        private Rectangle _xButtonBounds => new Rectangle(ClientSize.Width - STATUS_BAR_BUTTON_WIDTH, ClientRectangle.Y, STATUS_BAR_BUTTON_WIDTH, STATUS_BAR_HEIGHT);

        private byte _blurredOpacityDark = 225; // backing field for dark overlay (was auto property default)
        private byte _blurredOpacityLight = 140; // backing field for light overlay (was auto property default)

        private ButtonState _buttonState = ButtonState.None;
        private bool _isClosing = false; // flag to skip layered redraws during shutdown
        private double _customOpacity = 1.0; // our own alpha for layered rounded mode

        // === Caches for performance ===
        private GraphicsPath? _roundedClipPath;
        private Size _roundedClipSize;
        private int _roundedClipRadius;
        private bool _roundedClipMaximized;

        private Bitmap? _backBuffer;
        private Graphics? _backG;

        // System command IDs
        private const int SC_SIZE = 0xF000;
        private const int SC_MOVE = 0xF010;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;
        private const int SC_CLOSE = 0xF060;

        // Dirty region tracking for partial redraws
        private readonly List<Rectangle> _dirtyRegions = new();
        private bool _needsFullRedraw = true;
        #endregion

        #region Constructor
        public Skin()
        {
            InitializeComponent();

            FormBorderStyle = FormBorderStyle.None;
            Sizable = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Padding = new Padding(PADDING_MINIMUM, STATUS_BAR_HEIGHT, PADDING_MINIMUM, PADDING_MINIMUM);

            // Detect OS version once at construction
            isWindows11OrLater = Environment.OSVersion.Version.Major >= 10 &&
                                 Environment.OSVersion.Version.Build >= Windows11Build;

            this.Load += (s, e) =>
                                    {
                                        if (!_isClosing) { _needsFullRedraw = true; RedrawForm(); }

                                        if (isMain && Persistance)
                                        {
                                            Persistor.Load();
                                        }
                                     };

           

            this.FormClosing += (s, e) =>
                                    {
                                        if (isMain && Persistance)
                                        {
                                            Persistor.Save();
                                        }
                                    };

            this.Resize += (s, e) => { if (!_isClosing) { InvalidateCaches(sizeOnly: true); ApplyRoundedRegion(); _needsFullRedraw = true; RedrawForm(); } };
        }
        #endregion

        #region Methods
        private void RefreshChrome()
        {
            if (_isClosing) return;
            // mark title bar area dirty for partial redraw
            MarkDirty(_statusBarBounds);
            if (IsLayeredRounded)
                RedrawForm();
            else
                Invalidate();
        }

        private void ResizeForm(ResizeDirection direction)
        {
            if (DesignMode)
                return;

            var dir = direction switch
            {
                ResizeDirection.BottomLeft => (int)HT.BottomLeft,
                ResizeDirection.Left => (int)HT.Left,
                ResizeDirection.Right => (int)HT.Right,
                ResizeDirection.BottomRight => (int)HT.BottomRight,
                ResizeDirection.Bottom => (int)HT.Bottom,
                ResizeDirection.Top => (int)HT.Top,
                ResizeDirection.TopLeft => (int)HT.TopLeft,
                ResizeDirection.TopRight => (int)HT.TopRight,
                _ => -1
            };

            if (direction == ResizeDirection.Left)
                Cursor = Cursors.SizeWE;
            else if (direction == ResizeDirection.BottomLeft)
                Cursor = Cursors.SizeNESW;

            Win32.ReleaseCapture();
            if (dir != -1)
            {
                Win32.SendMessage(Handle, (int)WM.NonClientLeftButtonDown, dir, 0);
            }
        }

        public void UpdateButtons(MouseButtons button, Point location, bool up = false)
        {
            if (_isClosing) return;
            if (DesignMode) return;

            var oldState = _buttonState;
            bool showMin = MinimizeBox && ControlBox;
            bool showMax = MaximizeBox && ControlBox;
            var themeBounds = GetThemeButtonBounds(showMin, showMax);

            if (button == MouseButtons.Left && !up)
            {
                if (!themeBounds.IsEmpty && themeBounds.Contains(location))
                    _buttonState = ButtonState.DrawerDown; // reuse Drawer* states for Theme button
                else if (showMin && !showMax && _maxButtonBounds.Contains(location))
                    _buttonState = ButtonState.MinDown;
                else if (showMin && showMax && _minButtonBounds.Contains(location))
                    _buttonState = ButtonState.MinDown;
                else if (showMax && _maxButtonBounds.Contains(location))
                    _buttonState = ButtonState.MaxDown;
                else if (ControlBox && _xButtonBounds.Contains(location))
                    _buttonState = ButtonState.XDown;
                else
                    _buttonState = ButtonState.None;
            }
            else
            {
                if (_showThemeButton && !themeBounds.IsEmpty && themeBounds.Contains(location))
                {
                    _buttonState = ButtonState.DrawerOver;

                    if (oldState == ButtonState.DrawerDown && up)
                    {
                        if (!isMain)
                        {
                            SkinManager.UpdateTheme(this, SkinManager.Theme);
                        }
                        SkinManager.Theme = SkinManager.Theme == SkinManager.Themes.DARK
                                ? SkinManager.Themes.LIGHT
                                : SkinManager.Themes.DARK;
                        _needsFullRedraw = true; // theme change affects all visuals
                    }
                }
                else if (showMin && !showMax && _maxButtonBounds.Contains(location))
                {
                    _buttonState = ButtonState.MinOver;
                    if (oldState == ButtonState.MinDown && up)
                        WindowState = FormWindowState.Minimized;
                }
                else if (showMin && showMax && _minButtonBounds.Contains(location))
                {
                    _buttonState = ButtonState.MinOver;
                    if (oldState == ButtonState.MinDown && up)
                        WindowState = FormWindowState.Minimized;
                }
                else if (showMax && _maxButtonBounds.Contains(location))
                {
                    _buttonState = ButtonState.MaxOver;
                    if (oldState == ButtonState.MaxDown && up)
                        Maximized = !Maximized;
                }
                else if (ControlBox && _xButtonBounds.Contains(location))
                {
                    _buttonState = ButtonState.XOver;
                    if (oldState == ButtonState.XDown && up)
                        Close();
                }
                else
                {
                    _buttonState = ButtonState.None;
                }
            }

            if (oldState != _buttonState || up)
            {
                MarkDirty(_statusBarBounds);
                if (!themeBounds.IsEmpty) MarkDirty(themeBounds);
                MarkDirty(_minButtonBounds);
                MarkDirty(_maxButtonBounds);
                MarkDirty(_xButtonBounds);
                RefreshChrome();
            }
        }

        // Method to handle drop shadow updates
        private void UpdateDropShadow()
        {
            if (DesignMode || !IsHandleCreated) return;
            if (_showDropShadow && Environment.OSVersion.Version.Major >= 6) // Vista+
            {
                int ncrpEnabled = 2; // DWMNCRP_ENABLED
                _ = Win32.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY, ref ncrpEnabled, sizeof(int));

                var margins = new MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 };
                _ = Win32.DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            }
            else
            {
                // Remove drop shadow by setting margins to 0
                var margins = new MARGINS { cxLeftWidth = 0, cxRightWidth = 0, cyTopHeight = 0, cyBottomHeight = 0 };
                _ = Win32.DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            }
        }

        // Invalidate caches for rounded path/backbuffer when needed
        private void InvalidateCaches(bool sizeOnly = false)
        {
            _roundedClipPath?.Dispose();
            _roundedClipPath = null;
            if (!sizeOnly)
            {
                _roundedClipRadius = 0;
                _roundedClipSize = Size.Empty;
                _roundedClipMaximized = false;
            }

            // Recreate backbuffer on size changes
            _backG?.Dispose();
            _backG = null;
            _backBuffer?.Dispose();
            _backBuffer = null;
        }

        // Apply a rounded Region to the window (pre-Windows11) to keep child controls interactive.
        private void ApplyRoundedRegion()
        {
            if (!IsHandleCreated) return;

            // Only apply a region when we are not relying on DWM rounding and when rounded corners are requested
            if (_isRoundedCorners && !isWindows11OrLater && !_useLayeredRounding)
            {
                if (Maximized || DefaultCornerRadius <= 0)
                {
                    // Clear region to full rectangle when maximized or no radius
                    if (Region != null)
                    {
                        var old = Region;
                        Region = null;
                        old?.Dispose();
                    }
                    return;
                }

                using var path = GetRoundedRect(new Rectangle(0, 0, Width, Height), DefaultCornerRadius);
                var newRegion = new Region(path);
                var oldRegion = Region;
                Region = newRegion;
                oldRegion?.Dispose();
            }
            else
            {
                // Clear any custom region when not needed
                if (Region != null)
                {
                    var old = Region;
                    Region = null;
                    old?.Dispose();
                }
            }
        }

        // dirty region helpers
        private void AttachControlHandlers(Control c)
        {
            c.Invalidated += (_, __) => { MarkDirty(c.Bounds); };
            c.LocationChanged += (_, __) => { MarkDirty(c.Bounds); };
            c.SizeChanged += (_, __) => { MarkDirty(c.Bounds); _needsFullRedraw = true; };
        }

        private void MarkDirty(Rectangle r)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            var clip = new Rectangle(Point.Empty, ClientSize);
            r.Intersect(clip);
            if (r.IsEmpty) return;
            r.Inflate(2, 2); // small padding
            _dirtyRegions.Add(r);
        }

        private void MergeDirtyRegions()
        {
            if (_dirtyRegions.Count < 2) return;
            var merged = new List<Rectangle>();
            foreach (var rect in _dirtyRegions)
            {
                bool mergedIn = false;
                for (int i = 0; i < merged.Count; i++)
                {
                    if (merged[i].IntersectsWith(rect) || merged[i].Contains(rect))
                    {
                        merged[i] = Rectangle.Union(merged[i], rect);
                        mergedIn = true;
                        break;
                    }
                }
                if (!mergedIn) merged.Add(rect);
            }
            _dirtyRegions.Clear();
            _dirtyRegions.AddRange(merged);
        }

        #endregion

        #region Rendering
        #region Menu Methods
        private Color GetGlyphColor(ButtonState current, ButtonState overState, ButtonState downState)
            => current == overState || current == downState ? SkinManager.TextHover : SkinManager.TextColor;
        private void DrawThemeGlyph(Graphics g, Rectangle bounds, Color glyphColor)
        {
            const int pad = 4; // Defines inner padding of 4 pixels
            var iconRect = new Rectangle( // Creates the usable drawing area with padding
                bounds.X + pad, bounds.Y + pad, // Left and top offset by padding
                bounds.Width - 2 * pad, bounds.Height - 2 * pad); // Width/height reduced by 2×padding
            var oldSmoothing = g.SmoothingMode; // Saves current smoothing mode to restore later
            Region? oldClip = null; // Declares variable to store current clip region (if any)
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // Enables anti-aliasing for smooth edges
                // g.PixelOffsetMode = PixelOffsetMode.HighQuality;  // Or Half for sub-pixel shift

                switch (SkinManager.Theme) // Begins theme-based drawing logic
                {
                    case SkinManager.Themes.LIGHT: // Handles light theme (moon icon)
                        {
                            const float moonScale = 0.75f; // Scales moon to 80% of available space
                            int sw = (int)(iconRect.Width * moonScale); // Calculates scaled width
                            int sh = (int)(iconRect.Height * moonScale); // Calculates scaled height
                            var scaledRect = new Rectangle( // Defines centered rectangle for scaled icon
                                iconRect.X + (iconRect.Width - sw) / 2, // Centers horizontally
                                iconRect.Y + (iconRect.Height - sh) / 2, // Centers vertically
                                sw, sh); // Uses scaled width and height
                                         // Offset moon position
                            scaledRect.Offset(-1, 0);
                            int diameter = Math.Min(scaledRect.Width, scaledRect.Height); // Ensures perfect circle
                            var outer = new Rectangle( // Creates outer circle (full moon)
                                scaledRect.X + (scaledRect.Width - diameter) / 2, // Centers circle in X
                                scaledRect.Y + (scaledRect.Height - diameter) / 2, // Centers circle in Y
                                diameter, diameter); // Width and height = diameter
                            int innerOffsetX = -(int)(diameter * 0.45f); // Shifts inner circle left to form crescent
                            int innerOffsetY = (int)(diameter * 0.05f); // Slight vertical offset for aesthetics
                            int innerDiameter = (int)(diameter * 0.95f); // Inner circle slightly smaller
                            var inner = new Rectangle( // Defines inner circle (to be subtracted)
                                outer.X + innerOffsetX, // Applies horizontal offset
                                outer.Y + innerOffsetY, // Applies vertical offset
                                innerDiameter, innerDiameter); // Uses smaller diameter

                            // Calculate centers and radii (float for precision)
                            float ox = outer.X + diameter / 2f;
                            float oy = outer.Y + diameter / 2f;
                            float r1 = diameter / 2f;
                            float ix = inner.X + innerDiameter / 2f;
                            float iy = inner.Y + innerDiameter / 2f;
                            float r2 = innerDiameter / 2f;

                            // Distance between centers
                            float d = (float)Math.Sqrt((ix - ox) * (ix - ox) + (iy - oy) * (iy - oy));

                            // Check for valid intersection (add error handling in production)
                            if (d <= Math.Abs(r1 - r2) || d >= r1 + r2)
                            {
                                // Fallback: Draw full outer (or handle error)
                                // For now, assume valid as per params
                            }
                            else
                            {
                                float a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
                                float h = (float)Math.Sqrt(r1 * r1 - a * a);

                                // Intermediate point
                                float px = ox + a * (ix - ox) / d;
                                float py = oy + a * (iy - oy) / d;

                                // Intersection points
                                float x1 = px + h * (iy - oy) / d;
                                float y1 = py - h * (ix - ox) / d;
                                float x2 = px - h * (iy - oy) / d;
                                float y2 = py + h * (ix - ox) / d;

                                // Helper to compute angle in degrees (0 at positive X, clockwise)
                                static float GetAngle(float cx, float cy, float px, float py)
                                {
                                    float angle = (float)(Math.Atan2(py - cy, px - cx) * 180 / Math.PI);
                                    return (angle + 360) % 360; // Normalize to 0-360
                                }

                                float angle1_outer = GetAngle(ox, oy, x1, y1);
                                float angle2_outer = GetAngle(ox, oy, x2, y2);
                                float angle1_inner = GetAngle(ix, iy, x1, y1);
                                float angle2_inner = GetAngle(ix, iy, x2, y2);

                                // Helper to compute start and sweep (larger for outer, smaller for inner)
                                static (float start, float sweep) ComputeStartSweep(float a1, float a2, bool isOuter)
                                {
                                    // Compute both directions
                                    float sweep1 = (a2 - a1 + 360) % 360;
                                    float sweep2 = (a1 - a2 + 360) % 360;

                                    if (isOuter)
                                    {
                                        // Larger sweep
                                        return sweep1 > sweep2 ? (a1, sweep1) : (a2, sweep2);
                                    }
                                    else
                                    {
                                        // Smaller sweep
                                        return sweep1 < sweep2 ? (a1, sweep1) : (a2, sweep2);
                                    }
                                }

                                var (outer_start, outer_sweep) = ComputeStartSweep(angle1_outer, angle2_outer, true);
                                var (inner_start, inner_sweep) = ComputeStartSweep(angle1_inner, angle2_inner, false);

                                // Create paths with only visible arcs
                                using var outerPath = new GraphicsPath();
                                outerPath.AddArc(outer, outer_start, outer_sweep);
                                using var innerPath = new GraphicsPath();
                                innerPath.AddArc(inner, inner_start, inner_sweep);

                                // Apply rotation
                                float cx = scaledRect.X + scaledRect.Width / 2f; // Center X
                                float cy = scaledRect.Y + scaledRect.Height / 2f; // Center Y
                                using (var m = new Matrix())
                                {
                                    m.RotateAt(20f, new PointF(cx, cy)); // Rotates 20° clockwise around center
                                    outerPath.Transform(m);
                                    innerPath.Transform(m);
                                }

                                // Draw the arcs (no clip needed)
                                using var pen = new Pen(glyphColor, 1f) { LineJoin = LineJoin.Round };
                                g.DrawPath(pen, outerPath);
                                g.DrawPath(pen, innerPath);
                            }

                            break; // Exits light theme case
                        }
                    case SkinManager.Themes.DARK: // Handles dark theme (sun icon)
                        {
                            const float sunScale = 1f;
                            int sw = (int)(iconRect.Width * sunScale);
                            int sh = (int)(iconRect.Height * sunScale);
                            var scaledRect = new Rectangle(
                                iconRect.X + (iconRect.Width - sw) / 2,
                                iconRect.Y + (iconRect.Height - sh) / 2,
                                sw, sh);

                            int diameter = Math.Min(scaledRect.Width, scaledRect.Height);
                            int cx = iconRect.X + iconRect.Width / 2;   // centre of the whole icon
                            int cy = iconRect.Y + iconRect.Height / 2;

                            int coreRadius = (int)(diameter * 0.3f);               // core circle
                            int rayLength = (int)(diameter * 0.20f);              // default ray length
                            int extraLen = 1;                                     // <-- extra 2 px for cardinal rays

                            var coreRect = new Rectangle(
                                cx - coreRadius, cy - coreRadius,
                                coreRadius * 2, coreRadius * 2);

                            // --- draw core outline -------------------------------------------------
                            using (var pen = new Pen(glyphColor, 1.5f) { LineJoin = LineJoin.Round })
                                g.DrawEllipse(pen, coreRect);

                            // --- ray geometry ------------------------------------------------------
                            float baseWidth = diameter * 0.07f;   // width at the base (where it meets the core)
                            float tipWidth = diameter * 0.01f;   // width at the tip

                            using var rayBrush = new SolidBrush(glyphColor);

                            for (int i = 0; i < 8; i++)
                            {
                                double angle = Math.PI * 2 / 8 * i;          // 0°, 45°, 90°,
                                double sin = Math.Sin(angle);
                                double cos = Math.Cos(angle);

                                // ---- choose length -------------------------------------------------
                                // i == 0 to right, 2 to bottom, 4 to left, 6 to top
                                int thisRayLen = (i % 2 == 0) ? rayLength + extraLen : rayLength;

                                // ---- base points (on the core surface) ----------------------------- 
                                float bx1 = cx + (float)(cos * coreRadius - sin * (baseWidth / 2));
                                float by1 = cy + (float)(sin * coreRadius + cos * (baseWidth / 2));
                                float bx2 = cx + (float)(cos * coreRadius + sin * (baseWidth / 2));
                                float by2 = cy + (float)(sin * coreRadius - cos * (baseWidth / 2));

                                // ---- tip points (far end of the ray) -------------------------------
                                float tx1 = cx + (float)(cos * (coreRadius + thisRayLen) - sin * (tipWidth / 2));
                                float ty1 = cy + (float)(sin * (coreRadius + thisRayLen) + cos * (tipWidth / 2));
                                float tx2 = cx + (float)(cos * (coreRadius + thisRayLen) + sin * (tipWidth / 2));
                                float ty2 = cy + (float)(sin * (coreRadius + thisRayLen) - cos * (tipWidth / 2));

                                // ---- quadrilateral that forms the tapered ray ----------------------
                                PointF[] rayPoints =
                                {
                                    new PointF(bx1, by1),
                                    new PointF(bx2, by2),
                                    new PointF(tx2, ty2),
                                    new PointF(tx1, ty1)
                                };

                                g.FillPolygon(rayBrush, rayPoints);
                            }

                            break; // Exits dark theme case
                        }
                }
            }
            finally
            {
                if (oldClip != null) // Only if a clip was saved
                {
                    g.SetClip(oldClip, CombineMode.Replace); // Restores original clip
                    oldClip.Dispose(); // Releases clip region memory
                }
                g.SmoothingMode = oldSmoothing; // Restores original smoothing mode
            }
        }
        private void ShowSystemMenu(Point screenLocation)
        {
            IntPtr hMenu = Win32.GetSystemMenu(this.Handle, false);
            int cmd = Win32.TrackPopupMenuEx(hMenu, TPM_LEFTALIGN | TPM_RETURNCMD, screenLocation.X, screenLocation.Y, this.Handle, IntPtr.Zero);
            if (cmd != 0)
            {
                Win32.SendMessage(this.Handle, WM_SYSCOMMAND, cmd, 0);
            }
        }
        private void ShowThemedSystemMenu(Point screenLocation)
        {
            var cms = new ContextMenuStrip
            {
                ShowImageMargin = true,
                Renderer = new ThemedContextRenderer(SkinManager, Font),
                ImageScalingSize = new Size(16, 16),
            };

            // Ensure layout is measured using the same font as rendering to avoid cramped text
            cms.Font = this.Font;
            // We'll let the menu autosize its height but guide its width with MinimumSize and MaximumSize
            cms.AutoSize = false;

            // Build items
            var restore = new ToolStripMenuItem("Restore", null, (_, __) => Win32.SendMessage(this.Handle, WM_SYSCOMMAND, SC_RESTORE, 0))
            {
                Enabled = this.WindowState != FormWindowState.Normal && this.ControlBox,
            };
            var move = new ToolStripMenuItem("Move", null, (_, __) => Win32.SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE, 0))
            {
                Enabled = this.WindowState == FormWindowState.Normal && this.Sizable,
            };
            var size = new ToolStripMenuItem("Size", null, (_, __) => Win32.SendMessage(this.Handle, WM_SYSCOMMAND, SC_SIZE, 0))
            {
                Enabled = this.WindowState == FormWindowState.Normal && this.Sizable,
            };
            var minimize = new ToolStripMenuItem("Minimize", null, (_, __) => Win32.SendMessage(this.Handle, WM_SYSCOMMAND, SC_MINIMIZE, 0))
            {
                Enabled = this.MinimizeBox && this.ControlBox,
            };
            var maximize = new ToolStripMenuItem("Maximize", null, (_, __) => Win32.SendMessage(this.Handle, WM_SYSCOMMAND, SC_MAXIMIZE, 0))
            {
                Enabled = this.MaximizeBox && this.ControlBox,
            };
            var close = new ToolStripMenuItem("Close", null, (_, __) => Win32.SendMessage(this.Handle, WM_SYSCOMMAND, SC_CLOSE, 0))
            {
                Enabled = this.ControlBox,
                ShortcutKeys = Keys.Alt | Keys.F4,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            var Properties = new ToolStripMenuItem("Properties", null, (_, __) =>
            {
                Properties propertiesForm = new Properties();
                propertiesForm.StartPosition = FormStartPosition.Manual;
                propertiesForm.Location = new Point(this.Left + STATUS_BAR_HEIGHT / 2, this.Top + STATUS_BAR_HEIGHT / 2);
                propertiesForm.ShowDialog(this);

            })

            {
                Enabled = true
            };

            // Assign vector icons
            restore.Image = CreateSystemMenuIcon(MenuIcon.Restore, restore.Enabled);
            //move.Image = CreateSystemMenuIcon(MenuIcon.Move, move.Enabled);
            //size.Image = CreateSystemMenuIcon(MenuIcon.Size, size.Enabled);
            minimize.Image = CreateSystemMenuIcon(MenuIcon.Minimize, minimize.Enabled);
            maximize.Image = CreateSystemMenuIcon(MenuIcon.Maximize, maximize.Enabled);
            close.Image = CreateSystemMenuIcon(MenuIcon.Close, close.Enabled);

            if (FormBorderStyle == FormBorderStyle.FixedToolWindow ||
                FormBorderStyle == FormBorderStyle.SizableToolWindow ||
                FormBorderStyle == FormBorderStyle.FixedDialog)
            {
                cms.Items.AddRange(new ToolStripItem[]
                {
                   move,
                   new ToolStripSeparator(),
                   close
                });
            }
            else
            if (FormBorderStyle == FormBorderStyle.None ||
                FormBorderStyle == FormBorderStyle.Sizable ||
                FormBorderStyle == FormBorderStyle.FixedSingle ||
                FormBorderStyle == FormBorderStyle.Fixed3D)
            {
                cms.Items.AddRange(new ToolStripItem[]
                {
                    restore, move, size, minimize, maximize,
                    new ToolStripSeparator(),
                    close,
                    Properties
                });
            }

            // Compute a comfortable width so text isn't cramped
            int maxTextWidth = 0;
            using (var g = this.CreateGraphics())
            {
                foreach (ToolStripItem it in cms.Items)
                {
                    if (it is ToolStripSeparator) continue;
                    var sz = TextRenderer.MeasureText(g, it.Text, cms.Font);
                    if (sz.Width > maxTextWidth) maxTextWidth = sz.Width;
                }
            }
            // Account for image margin and internal padding
            int imageArea = cms.ShowImageMargin ? cms.ImageScalingSize.Width + 24 : 12; // icon + gap
            int paddingExtra = 24; // slightly tighter than before
            int desiredWidth = maxTextWidth + imageArea + paddingExtra;

            // Clamp width between min and max so it doesn't look too long
            int minWidth = 170;   // tighter minimum
            int maxWidth = 260;   // reasonable maximum for this menu
            int finalWidth = Math.Max(minWidth, Math.Min(maxWidth, desiredWidth));

            // Let height be preferred, but force the computed width
            var preferred = cms.GetPreferredSize(Size.Empty);
            cms.Size = new Size(finalWidth, preferred.Height);

            // Ensure each item stretches to the menu width so the hover highlight reaches the right edge
            foreach (ToolStripItem it in cms.Items)
            {
                it.AutoSize = false;
                it.Width = finalWidth;
            }

            cms.Show(screenLocation);
        }
        private enum MenuIcon { Restore, Move, Size, Minimize, Maximize, Close }

        #endregion

        #region Custom Rendering Methods
        private Bitmap CreateSystemMenuIcon(MenuIcon icon, bool enabled)
        {
            int w = 16, h = 16;
            var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Color col = enabled ? SkinManager.TextColor : Color.FromArgb(120, SkinManager.TextColor);
            using var pen = new Pen(col, 1.6f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var penThin = new Pen(col, 1.2f) { LineJoin = LineJoin.Round };
            using var brush = new SolidBrush(col);

            switch (icon)
            {
                case MenuIcon.Minimize:
                    g.DrawLine(pen, 4, 11, 12, 11);
                    break;
                case MenuIcon.Maximize:
                    g.DrawRectangle(pen, 4, 4, 8, 8);
                    break;
                case MenuIcon.Restore:
                    {
                        var front = new Rectangle(3, 7, 8, 6);
                        g.DrawRectangle(pen, front);
                        g.DrawLine(pen, front.X + 3, front.Y, front.X + 3, front.Y + 2 - 4);
                        g.DrawLine(pen, front.X + 3, front.Y + 2 - 4, front.X + 3 + 8, front.Y + 2 - 4);
                        g.DrawLine(pen, front.X + 3 + 8, front.Y + 2 - 4, front.X + 3 + 8, front.Y + 2 - 4 + 6);
                        g.DrawLine(pen, front.X + 3 + 8, front.Y + 2 - 4 + 6, front.X + 3 + 8 - 3, front.Y + 2 - 4 + 6);
                    }
                    break;
                case MenuIcon.Close:
                    g.DrawLine(pen, 5, 5, 11, 11);
                    g.DrawLine(pen, 11, 5, 5, 11);
                    break;
                case MenuIcon.Move:
                    // four arrows from center
                    Point c = new Point(8, 8);
                    // up
                    g.DrawLine(pen, c.X, c.Y, c.X, 3);
                    g.FillPolygon(brush, new[] { new Point(c.X, 1), new Point(c.X - 2, 4), new Point(c.X + 2, 4) });
                    // down
                    g.DrawLine(pen, c.X, c.Y, c.X, 13);
                    g.FillPolygon(brush, new[] { new Point(c.X, 15), new Point(c.X - 2, 12), new Point(c.X + 2, 12) });
                    // left
                    g.DrawLine(pen, c.X, c.Y, 3, c.Y);
                    g.FillPolygon(brush, new[] { new Point(1, c.Y), new Point(c.X - 2, c.Y - 2), new Point(c.X - 2, c.Y + 2) });
                    // right
                    g.DrawLine(pen, c.X, c.Y, 13, c.Y);
                    g.FillPolygon(brush, new[] { new Point(15, c.Y), new Point(c.X + 2, c.Y - 2), new Point(c.X + 2, c.Y + 2) });
                    break;
                case MenuIcon.Size:
                    // diagonal double-headed arrow
                    g.DrawLine(pen, 4, 12, 12, 4);
                    g.FillPolygon(brush, new[] { new Point(12, 4), new Point(10, 4), new Point(12, 6) });
                    g.FillPolygon(brush, new[] { new Point(4, 12), new Point(6, 12), new Point(4, 10) });
                    break;
            }

            return bmp;
        }

        private sealed class ThemedContextRenderer : ToolStripProfessionalRenderer
        {
            private readonly SkinManager _mgr;
            private readonly Font _TextFont;
            public ThemedContextRenderer(SkinManager mgr, Font TextFont)
            {
                _mgr = mgr;
                _TextFont = TextFont;
            }
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(_mgr.PrimaryColor);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Bounds.Size);
                if (e.Item.Selected || e.Item.Pressed)
                {
                    // Use focus brush for hover/press
                    e.Graphics.FillRectangle(_mgr.BackgroundFocusBrush, bounds);

                    using (Pen pen = new Pen(_mgr.BorderColor))
                    {
                        var rect = e.Item.ContentRectangle;
                        rect.Width -= 1;
                        rect.Height -= 1;
                        e.Graphics.DrawRectangle(pen, rect);
                    }

                }
                else
                {
                    using var b = new SolidBrush(_mgr.PrimaryColor);
                    e.Graphics.FillRectangle(b, bounds);
                }


            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? _mgr.TextColor : _mgr.TextColorDisabled;
                //e.TextFont = _TextFont;
                var itemFont = e.Item.Font ?? _TextFont;
                e.TextFont = itemFont;

                // Custom draw caption (left) and shortcut (right) to ensure proper separation
                if (e.Item is ToolStripMenuItem mi)
                {
                    var g = e.Graphics;
                    var textColor = e.TextColor;
                    var font = e.TextFont;

                    // Left caption
                    var captionRect = e.TextRectangle;

                    TextRenderer.DrawText(
                        g,
                        mi.Text,
                        font,
                        captionRect,
                        textColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

                    // Right aligned shortcut (skip when none)
                    if (mi.ShowShortcutKeys)
                    {
                        // Prefer explicit display string when provided
                        string? shortcutText = string.IsNullOrWhiteSpace(mi.ShortcutKeyDisplayString)
                            ? null
                            : mi.ShortcutKeyDisplayString.Trim();

                        // If no explicit string, use ShortcutKeys (but ignore Keys.None)
                        if (shortcutText == null && mi.ShortcutKeys != Keys.None)
                        {
                            shortcutText = new System.Windows.Forms.KeysConverter()
                                .ConvertToString(mi.ShortcutKeys);
                        }

                        // Normalize and ignore unwanted placeholders like "(none)" or "None"
                        if (!string.IsNullOrWhiteSpace(shortcutText))
                        {
                            var normalized = shortcutText.Trim();
                            if (!normalized.Equals("(none)", StringComparison.OrdinalIgnoreCase) &&
                                !normalized.Equals("none", StringComparison.OrdinalIgnoreCase))
                            {
                                const int rightPadding = 8; // Space from the right border
                                var content = e.Item.ContentRectangle;
                                var right = content.Right - rightPadding;

                                // Measure and place the shortcut text flush-right
                                var shortcutSize = TextRenderer.MeasureText(g, normalized, font,
                                    Size.Empty,
                                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                                var shortcutRect = new Rectangle(
                                    right - shortcutSize.Width,
                                    e.TextRectangle.Y,
                                    shortcutSize.Width,
                                    e.TextRectangle.Height);

                                TextRenderer.DrawText(
                                    g,
                                    normalized,
                                    font,
                                    shortcutRect,
                                    textColor,
                                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                            }
                        }
                    }

                    // Prevent base from drawing again to avoid overlap
                    return;
                }

                base.OnRenderItemText(e);
            }
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                var c = _mgr.DividersColor;
                using var pen = new Pen(c);
                int y = e.Item.ContentRectangle.Y + e.Item.ContentRectangle.Height / 2;
                e.Graphics.DrawLine(pen, e.Item.ContentRectangle.Left + 2, y, e.Item.ContentRectangle.Right - 2, y);
            }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var pen = new Pen(_mgr.BorderColor);
                var r = new Rectangle(Point.Empty, e.ToolStrip.Size);
                r.Width -= 1; r.Height -= 1;
                e.Graphics.DrawRectangle(pen, r);
            }
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(_mgr.PrimaryColor);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

        }

        private void DrawAll(Graphics g, Rectangle clientRect)
        {
            DrawAll(g, clientRect, null);
        }

        private void DrawAll(Graphics g, Rectangle clientRect, GraphicsPath path)
        {
            var hoverBrush = SkinManager.BackgroundHoverBrush;
            var downBrush = SkinManager.BackgroundFocusBrush;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // If in layered-rounded mode, set a single clip to the rounded path for all drawing
            GraphicsState? globalClipState = null;
            if (IsLayeredRounded && path != null)
            {
                globalClipState = g.Save();
                g.SetClip(path, CombineMode.Intersect);
            }

            #region Blur
            if (!_showBlurredBackgound)
            {
                if (!IsLayeredRounded)
                {
                    g.Clear(SkinManager.BackdropColor);
                }
                // else: background already filled in RedrawForm respecting rounded corners
            }
            else
            {
                g.Clear(Color.Transparent);

                // draw translucent overlay; if rounded mode, clip already active
                Color overlayColor = SkinManager.Theme == SkinManager.Themes.DARK
                    ? Color.FromArgb(BlurredOpacityDark, SkinManager.BackdropColor)
                    : Color.FromArgb(BlurredOpacityLight, SkinManager.BackdropColor);
                using var overlay = new SolidBrush(overlayColor);
                g.FillRectangle(overlay, ClientRectangle);
            }
            #endregion

            #region Icon and Form Text
            if (ControlBox)
            {
                if (!_blurredTitleBar)
                {
                    g.FillRectangle(SkinManager.PrimaryBrush, _statusBarBounds); // skip fill when transparent
                }

                //---Draw the window icon-- -
                int iconPadding = 4;
                int iconSize = STATUS_BAR_HEIGHT - 2 * iconPadding;

                // ---offset for icon ---
                int textOffset = 2 * iconPadding;
                if (this.Icon != null && ShowIcon)
                {
                    // Draw icon left-aligned and vertically centered
                    g.DrawIcon(this.Icon, new Rectangle(
                        _statusBarBounds.X + iconPadding,
                        _statusBarBounds.Y + iconPadding + 1, //center fix
                        iconSize,
                        iconSize));

                    textOffset = iconSize + 2 * iconPadding;
                }

                // --- Draw the window title text - ensure solid, non-ClearType text when drawing over blurred/transparent title bar
                var prevHint = g.TextRenderingHint;
                if (_showBlurredBackgound && _blurredTitleBar && Theme == SkinManager.Themes.LIGHT)
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAlias; // ClearType looks washed on translucent surfaces with black text
                }
                using (var textBrush = new SolidBrush(Color.FromArgb(255, SkinManager.TextColor.R, SkinManager.TextColor.G, SkinManager.TextColor.B)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    Rectangle textRect = new Rectangle(
                        _statusBarBounds.X + textOffset,
                        _statusBarBounds.Y + 1, //center fix
                        _statusBarBounds.Width - textOffset,
                        _statusBarBounds.Height
                    );

                    g.DrawString(Text, Font, textBrush, textRect, sf);
                }
                g.TextRenderingHint = prevHint;
            }
            #endregion

            #region Buttons
            bool showMin = MinimizeBox && ControlBox;
            bool showMax = MaximizeBox && ControlBox;
            var themeBoundsForPaint = GetThemeButtonBounds(showMin, showMax);

            //theme button hover
            if (_buttonState == ButtonState.DrawerOver && !themeBoundsForPaint.IsEmpty && _showThemeButton)
            {
                // Clip already set globally when needed
                FillTitleBarButton(g, themeBoundsForPaint, hoverBrush, path, restrictClip: false);
            }

            //theme button click
            if (_buttonState == ButtonState.DrawerDown && !themeBoundsForPaint.IsEmpty && _showThemeButton)
            {
                FillTitleBarButton(g, themeBoundsForPaint, downBrush, path, restrictClip: false);
            }

            if (_buttonState == ButtonState.MinOver && showMin)
            {
                var minRect = showMax ? _minButtonBounds : _maxButtonBounds;
                FillTitleBarButton(g, minRect, hoverBrush, path, restrictClip: false);
            }

            if (_buttonState == ButtonState.MinDown && showMin)
            {
                var minRect = showMax ? _minButtonBounds : _maxButtonBounds;
                FillTitleBarButton(g, minRect, downBrush, path, restrictClip: false);
            }

            if (_buttonState == ButtonState.MaxOver && showMax)
            {
                FillTitleBarButton(g, _maxButtonBounds, hoverBrush, path, restrictClip: false);
            }

            if (_buttonState == ButtonState.MaxDown && showMax)
            {
                FillTitleBarButton(g, _maxButtonBounds, downBrush, path, restrictClip: false);
            }

            if (_buttonState == ButtonState.XOver && ControlBox)
            {
                FillTitleBarButton(g, _xButtonBounds, hoverBrush, path, restrictClip: false);
            }

            if (_buttonState == ButtonState.XDown && ControlBox)
            {
                FillTitleBarButton(g, _xButtonBounds, hoverBrush, path, restrictClip: false);
            }

            // Determine glyph colors
            Color themeGlyphColor = GetGlyphColor(_buttonState, ButtonState.DrawerOver, ButtonState.DrawerDown);
            Color minGlyphColor = GetGlyphColor(_buttonState, ButtonState.MinOver, ButtonState.MinDown);
            Color maxGlyphColor = GetGlyphColor(_buttonState, ButtonState.MaxOver, ButtonState.MaxDown);
            Color closeGlyphColor = GetGlyphColor(_buttonState, ButtonState.XOver, ButtonState.XDown);

            // Theme glyph
            if (!themeBoundsForPaint.IsEmpty && _showThemeButton)
            {
                DrawThemeGlyph(g, themeBoundsForPaint, themeGlyphColor);
            }

            // Minimize glyph
            if (showMin)
            {
                int x = showMax ? _minButtonBounds.X : _maxButtonBounds.X;
                int y = showMax ? _minButtonBounds.Y : _maxButtonBounds.Y;
                using var minPen = new Pen(minGlyphColor, 2);
                g.DrawLine(minPen,
                    x + (int)(_minButtonBounds.Width * 0.33),
                    y + (int)(_minButtonBounds.Height * 0.66),
                    x + (int)(_minButtonBounds.Width * 0.66),
                    y + (int)(_minButtonBounds.Height * 0.66));
            }

            // Maximize glyph
            if (showMax)
            {
                using var maxPen = new Pen(maxGlyphColor, 2);
                if (WindowState != FormWindowState.Maximized)
                {
                    g.DrawRectangle(maxPen,
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.33),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Height * 0.36),
                        (int)(_maxButtonBounds.Width * 0.39),
                        (int)(_maxButtonBounds.Height * 0.31));
                }
                else
                {
                    g.DrawRectangle(maxPen,
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.30),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Height * 0.42),
                        (int)(_maxButtonBounds.Width * 0.40),
                        (int)(_maxButtonBounds.Height * 0.32));
                    g.DrawLine(maxPen,
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.42),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Height * 0.30),
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.42),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Height * 0.38));
                    g.DrawLine(maxPen,
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.40),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Height * 0.30),
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.86),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Width * 0.30));
                    g.DrawLine(maxPen,
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.82),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Height * 0.28),
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.82),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Width * 0.64));
                    g.DrawLine(maxPen,
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.70),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Height * 0.62),
                        _maxButtonBounds.X + (int)(_maxButtonBounds.Width * 0.84),
                        _maxButtonBounds.Y + (int)(_maxButtonBounds.Width * 0.62));
                }
            }

            // Close glyph
            if (ControlBox)
            {
                // Use hover color when hovered/pressed, otherwise CloseColor
                Color closePenColor = (_buttonState == ButtonState.XOver || _buttonState == ButtonState.XDown)
                 ? SkinManager.TextHover
                 : SkinManager.CloseColor;
                using var closePen = new Pen(closePenColor, 2);
                g.DrawLine(closePen,
                    _xButtonBounds.X + (int)(_xButtonBounds.Width * 0.33),
                    _xButtonBounds.Y + (int)(_xButtonBounds.Height * 0.33),
                    _xButtonBounds.X + (int)(_xButtonBounds.Width * 0.66),
                    _xButtonBounds.Y + (int)(_xButtonBounds.Height * 0.66));
                g.DrawLine(closePen,
                    _xButtonBounds.X + (int)(_xButtonBounds.Width * 0.66),
                    _xButtonBounds.Y + (int)(_xButtonBounds.Height * 0.33),
                    _xButtonBounds.X + (int)(_xButtonBounds.Width * 0.33),
                    _xButtonBounds.Y + (int)(_xButtonBounds.Height * 0.66));
            }
            #endregion

            #region Border
            if (WindowState != FormWindowState.Maximized)
            {
                int colorRef = isActive ? SkinManager.FormBorderColor : SkinManager.InactiveBorderColor;
                Color borderColor = Color.FromArgb(colorRef & 0xFF, (colorRef >> 8) & 0xFF, (colorRef >> 16) & 0xFF);
                using var pen = new Pen(borderColor, 1f) { LineJoin = LineJoin.Round };
                var oldSmooth = g.SmoothingMode;
                var oldOffset = g.PixelOffsetMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                int w = clientRect.Width;
                int h = clientRect.Height;
                if (_isRoundedCorners)
                {
                    pen.Alignment = PenAlignment.Outset;
                    using var borderPath = BuildBorderPath(w, h, DefaultCornerRadius, pen.Width - 1, Maximized);
                    g.DrawPath(pen, borderPath);
                }
                else
                {
                    g.DrawRectangle(pen, 0.5f, 0.5f, w - 1f, h - 1f);
                }

                g.SmoothingMode = oldSmooth;
                g.PixelOffsetMode = oldOffset;
            }
            #endregion

            // Restore global rounded clip if it was set
            if (globalClipState != null)
            {
                g.Restore(globalClipState);
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            // Cache by size/radius/maximized to avoid reallocation
            bool maximized = Maximized;
            if (_roundedClipPath != null && _roundedClipSize == bounds.Size && _roundedClipRadius == radius && _roundedClipMaximized == maximized)
            {
                return (GraphicsPath)_roundedClipPath.Clone();
            }

            int diameter = radius * 2;
            var path = new GraphicsPath();

            // Use RectangleF for subpixel accuracy
            var rect = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            if (!maximized)
            {
                path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
            }
            else
            {
                path.AddRectangle(rect);
                path.CloseFigure();
            }

            // update cache
            _roundedClipPath?.Dispose();
            _roundedClipPath = (GraphicsPath)path.Clone();
            _roundedClipSize = bounds.Size;
            _roundedClipRadius = radius;
            _roundedClipMaximized = maximized;

            return path;
        }

        // Precise border path: inflate outward by half stroke so corners are not visually undercut compared to fill path.
        private GraphicsPath BuildBorderPath(int width, int height, int radius, float strokeWidth, bool maximized)
        {
            var path = new GraphicsPath();
            if (width <= 1 || height <= 1) return path;
            float offsetX = 0f;
            float offsetY = 0f;
            float w = width;
            float h = height;
            if (w <= 0 || h <= 0)
            {
                path.AddRectangle(new RectangleF(0, 0, Math.Max(0, width - 1), Math.Max(0, height - 1)));
                path.CloseFigure();
                return path;
            }

            if (maximized || radius <= 0)
            {
                path.AddRectangle(new RectangleF(offsetX, offsetY, w, h));
                path.CloseFigure();
                return path;
            }

            float d = Math.Min(radius * 2f, Math.Min(w, h));
            float right = offsetX + w;
            float bottom = offsetY + h;

            path.AddArc(offsetX + 0.5f, offsetY, d, d, 180, 90);   // TL
            path.AddArc(right - d, offsetY + 0.2f, d, d, 270, 90); // TR
            path.AddArc(right - d, bottom - d, d, d, 0, 90);       // BR
            path.AddArc(offsetX, bottom - d, d, d, 90, 90);        // BL
            path.CloseFigure();
            return path;
        }

        private void RedrawForm()
        {
            if (_isClosing || IsDisposed || !IsHandleCreated || Width <= 0 || Height <= 0) return;

            if (!IsLayeredRounded)
            {
                Invalidate();
                _dirtyRegions.Clear();
                _needsFullRedraw = false;
                return;
            }

            if (!Visible || WindowState == FormWindowState.Minimized)
                return;

            bool full = _needsFullRedraw || _backBuffer == null || _dirtyRegions.Count == 0;

            // Ensure backbuffer
            if (_backBuffer == null || _backBuffer.Width != Width || _backBuffer.Height != Height)
            {
                _backG?.Dispose();
                _backBuffer?.Dispose();
                _backBuffer = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                _backG = Graphics.FromImage(_backBuffer);
                full = true;
            }

            var g = _backG!;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (full)
            {
                g.Clear(Color.Transparent);

                GraphicsPath path = GetRoundedRect(new Rectangle(0, 0, Width, Height), DefaultCornerRadius);

                if (!_showBlurredBackgound)
                {
                    using (var bg = new SolidBrush(SkinManager.BackdropColor))
                    {
                        g.FillPath(bg, path);
                    }
                }

                //Add inner highlight border(1 line)

                //if (_isRoundedCorners && _showBlurredBackgound)
                //{
                //    // Draw 1px INNER highlight (Microsoft style)
                //    using var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f);
                //        pen.Alignment = PenAlignment.Inset;
                //    g.DrawPath(pen, path);
                //}


                DrawAll(g, ClientRectangle, path);

                // draw controls
                foreach (Control c in Controls)
                {
                    if (c.Visible && c.Width > 0 && c.Height > 0)
                    {
                        using var cb = new Bitmap(c.Width, c.Height, PixelFormat.Format32bppArgb);
                        c.DrawToBitmap(cb, new Rectangle(0, 0, c.Width, c.Height));
                        g.DrawImageUnscaled(cb, c.Left, c.Top);
                    }
                }

                path.Dispose();
            }
            else
            {
                MergeDirtyRegions();
                foreach (var region in _dirtyRegions)
                {
                    var state = g.Save();
                    g.SetClip(region);

                    // clear region
                    using var clearBrush = new SolidBrush(Color.Transparent);
                    g.FillRectangle(clearBrush, region);

                    if (!_showBlurredBackgound)
                    {
                        using var bg = new SolidBrush(SkinManager.BackdropColor);
                        g.FillRectangle(bg, region);
                    }
                    else
                    {
                        Color overlayColor = SkinManager.Theme == SkinManager.Themes.DARK
                            ? Color.FromArgb(_blurredOpacityDark, SkinManager.BackdropColor)
                            : Color.FromArgb(_blurredOpacityLight, SkinManager.BackdropColor);
                        using var overlay = new SolidBrush(overlayColor);
                        g.FillRectangle(overlay, region);
                    }

                    DrawAll(g, ClientRectangle, null); // clip restricts drawing

                    // redraw controls intersecting region
                    foreach (Control c in Controls)
                    {
                        if (!c.Visible || c.Width <= 0 || c.Height <= 0) continue;
                        Rectangle cbounds = new Rectangle(c.Left, c.Top, c.Width, c.Height);
                        if (!cbounds.IntersectsWith(region)) continue;
                        using var cb = new Bitmap(c.Width, c.Height, PixelFormat.Format32bppArgb);
                        c.DrawToBitmap(cb, new Rectangle(0, 0, c.Width, c.Height));
                        g.DrawImageUnscaled(cb, c.Left, c.Top);
                    }

                    g.Restore(state);
                }
            }

            SetBitmap(_backBuffer);
            _dirtyRegions.Clear();
            _needsFullRedraw = false;
        }

        private void SetBitmap(Bitmap bitmap)
        {
            if (_isClosing) return;
            if (IsDisposed || !IsHandleCreated) return; // handle gone
            if (bitmap == null) return;
            if (bitmap.Width == 0 || bitmap.Height == 0) return;
            IntPtr screenDc = IntPtr.Zero;
            IntPtr memDc = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            try
            {
                screenDc = Win32.GetDC(IntPtr.Zero);
                if (screenDc == IntPtr.Zero) return;
                memDc = Win32.CreateCompatibleDC(screenDc);
                if (memDc == IntPtr.Zero) return;
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                if (hBitmap == IntPtr.Zero) return;
                oldBitmap = Win32.SelectObject(memDc, hBitmap);

                Win32.Size size = new Win32.Size(bitmap.Width, bitmap.Height);
                Win32.Point pointSource = new Win32.Point(0, 0);
                Win32.Point topPos = new Win32.Point(Left, Top);
                byte alpha = (byte)Math.Round(Math.Clamp(_customOpacity, 0.0, 1.0) * 255.0);
                Win32.BLENDFUNCTION blend = new Win32.BLENDFUNCTION
                {
                    BlendOp = Win32.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = alpha,
                    AlphaFormat = Win32.AC_SRC_ALPHA
                };

                // If window got destroyed between checks, abort
                if (IsDisposed || !IsHandleCreated) return;
                Win32.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, Win32.ULW_ALPHA);
            }
            catch (Exception ex)
            {
                // Suppress exceptions during shutdown; optionally log
                System.Diagnostics.Debug.WriteLine("[SetBitmap] " + ex.Message);
            }
            finally
            {
                if (screenDc != IntPtr.Zero)
                    Win32.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero && memDc != IntPtr.Zero)
                {
                    Win32.SelectObject(memDc, oldBitmap);
                    Win32.DeleteObject(hBitmap);
                }
                if (memDc != IntPtr.Zero)
                    Win32.DeleteDC(memDc);
            }
        }

        private void FillTitleBarButton(Graphics g, Rectangle bounds, Brush brush, GraphicsPath clipPath, bool restrictClip)
        {
            if (brush == null) return;
            if (bounds.IsEmpty) return;
            if (restrictClip && clipPath != null)
            {
                var state = g.Save();
                g.SetClip(clipPath, CombineMode.Intersect);
                g.FillRectangle(brush, bounds);
                g.Restore(state);
            }
            else
            {
                g.FillRectangle(brush, bounds);
            }
        }

        // Compute the theme button bounds so it sits immediately left of the effective Minimize button (or Max/Close if Min is hidden).
        private Rectangle GetThemeButtonBounds(bool showMin, bool showMax)
        {
            if (!ControlBox) return Rectangle.Empty;

            int anchorX;
            if (showMin)
            {
                // Minimize sits at _min when Max exists, otherwise it uses _max slot.
                anchorX = showMax ? _minButtonBounds.X : _maxButtonBounds.X;
            }
            else if (showMax)
            {
                anchorX = _maxButtonBounds.X;
            }
            else
            {
                // No Min/Max: place left of Close
                anchorX = _xButtonBounds.X;
            }

            return new Rectangle(anchorX - STATUS_BAR_BUTTON_WIDTH, _statusBarBounds.Y, STATUS_BAR_BUTTON_WIDTH, STATUS_BAR_HEIGHT);
        }

        #endregion

        #region Blur Methods
        private bool TryEnableSystemBackdrop(DWM_SYSTEMBACKDROP_TYPE Type)  // windows 11 blur
        {
            try
            {
                //Available on Windows11(22000 +) only
                if (!isWindows11OrLater)
                    return false;

                int wndattr = (int)Type;
                int useDark = 1;

                _ = Win32.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

                int hr = Win32.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, ref wndattr, sizeof(int));
                return hr == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERR] " + ex.Message);
                return false;
            }
        }

        private void TryEnableLegacyBlur()
        {
            // Prefer the lighter blur-behind effect for better move performance; fallback to acrylic if needed
            if (!EnableAccent(AccentState.ACCENT_ENABLE_BLURBEHIND))
            {
                EnableAccent(AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND);
            }
        }

        private bool EnableAccent(AccentState state)
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = state,
                    // Acrylic can benefit from some flags but simple blur does not need them
                    AccentFlags = state == AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND ? 2 : 0,
                    // Use a less-opaque tint so the effect comes from DWM/backdrop, not a layered window (ARGB)
                    GradientColor = unchecked((int)0x601E1E1E) //unchecked((int)0x80101010)//ToAccentAbgr(accentTint) //
                };
                int accentStructSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentStructSize
                };
                int result = Win32.SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
                return result != 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERR]" + ex.Message);
                return false;
            }
        }

        private void DisableAccent()
        {
            try
            {
                var accent = new AccentPolicy { AccentState = AccentState.ACCENT_DISABLED };
                int size = Marshal.SizeOf(accent);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(accent, ptr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = ptr,
                    SizeOfData = size
                };
                _ = Win32.SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(ptr);

                if (isWindows11OrLater)
                {
                    int backdropNone = 0; // DWMSBT_NONE
                    _ = Win32.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, ref backdropNone, sizeof(int));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERR]" + ex.Message);
            }
        }

        private void DisableDwmCorner()
        {
            if (!isWindows11OrLater) return;

            int preference = (int)DWMWINDOWATTRIBUTE.DWMWCP_DONOTROUND;
            Win32.DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        #endregion

        #region WinForm Override Methods
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

                cp.Style |= (int)WS.MinimizeBox | (int)WS.SysMenu;

                // Only use layered window when:
                // 1. Rounded corners enabled
                // 2. NOT Windows 11 (DWM does it)
                if (IsLayeredRounded)
                {
                    cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                }

                return cp;
            }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            var flags = Win32.GetWindowLongPtr(Handle, -16).ToInt64();
            Win32.SetWindowLongPtr(Handle, -16, (IntPtr)(flags | (int)WS.SizeFrame));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Prefer smooth DWM system backdrop (Win11), fallback to legacy blur
            if (_showBlurredBackgound)
            {
                if (!TryEnableSystemBackdrop(SystemBackdropType))
                {
                    TryEnableLegacyBlur();
                }
            }

            UpdateDropShadow();

            DisableDwmCorner();

            // Ensure base opacity is 100% in layered mode so our custom alpha blending works reliably
            if (IsLayeredRounded && base.Opacity != 1.0)
                base.Opacity = 1.0;

            // Apply rounded Region instead of full layered bitmap so buttons/controls stay interactive
            ApplyRoundedRegion();

            // attach control handlers for partial redraw
            foreach (Control c in Controls)
                AttachControlHandlers(c);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            AttachControlHandlers(e.Control);
            MarkDirty(e.Control.Bounds);
            _needsFullRedraw = true; // new control
        }

        protected override void WndProc(ref Message m)
        {
            var message = (WM)m.Msg;
            if (message == WM.NonClientCalcSize) return;

            if (message == WM.NonClientActivate)
            {
                m.Result = new IntPtr(-1);
                return;
            }

            base.WndProc(ref m);

            if (DesignMode || IsDisposed)
                return;

            var cursorPos = PointToClient(Cursor.Position);
            var isOverCaption = _statusBarBounds.Contains(cursorPos) &&
                !(_minButtonBounds.Contains(cursorPos) || _maxButtonBounds.Contains(cursorPos) || _xButtonBounds.Contains(cursorPos));

            // Exclude the theme button area from caption dragging
            if (ControlBox && _showThemeButton)
            {
                bool showMin = MinimizeBox && ControlBox;
                bool showMax = MaximizeBox && ControlBox;
                var themeBounds = GetThemeButtonBounds(showMin, showMax);
                if (!themeBounds.IsEmpty && themeBounds.Contains(cursorPos))
                    isOverCaption = false;
            }

            if (message == WM.LeftButtonDoubleClick && isOverCaption)
            {
                Maximized = !Maximized;
            }
            else if (message == WM.LeftButtonDown && isOverCaption)
            {
                Win32.ReleaseCapture();
                Win32.SendMessage(Handle, (int)WM.NonClientLeftButtonDown, (int)HT.Caption, 0);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawAll(e.Graphics, ClientRectangle);
        }

        protected override void OnMove(EventArgs e)
        {
            MaximizedBounds = new Rectangle(Point.Empty, Screen.GetWorkingArea(Location).Size);
            base.OnMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (DesignMode)
                return;
            UpdateButtons(e.Button, e.Location);

            if (e.Button == MouseButtons.Left && !Maximized && _resizeCursors.Contains(Cursor))
                ResizeForm(_resizeDir);
            base.OnMouseDown(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Cursor = Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (DesignMode)
                return;
            _buttonState = ButtonState.None;
            _resizeDir = ResizeDirection.None;
            if (_resizeCursors.Contains(Cursor))
            {
                Cursor = Cursors.Default;
            }
            RefreshChrome();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (DesignMode) return;

            var coords = e.Location;

            UpdateButtons(e.Button, coords);

            if (!Sizable) return;

            var isChildUnderMouse = GetChildAtPoint(coords) != null;

            if (!isChildUnderMouse && !Maximized && coords.Y < BORDER_WIDTH && coords.X > BORDER_WIDTH && coords.X < ClientSize.Width - BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.Top;
                Cursor = Cursors.SizeNS;
            }
            else if (!isChildUnderMouse && !Maximized && coords.X <= BORDER_WIDTH && coords.Y < BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.TopLeft;
                Cursor = Cursors.SizeNWSE;
            }
            else if (!isChildUnderMouse && !Maximized && coords.X >= ClientSize.Width - BORDER_WIDTH && coords.Y < BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.TopRight;
                Cursor = Cursors.SizeNESW;
            }
            else if (!isChildUnderMouse && !Maximized && coords.X <= BORDER_WIDTH && coords.Y >= ClientSize.Height - BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.BottomLeft;
                Cursor = Cursors.SizeNESW;
            }
            else if (!isChildUnderMouse && !Maximized && coords.X <= BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.Left;
                Cursor = Cursors.SizeWE;
            }
            else if (!isChildUnderMouse && !Maximized && coords.X >= ClientSize.Width - BORDER_WIDTH && coords.Y >= ClientSize.Height - BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.BottomRight;
                Cursor = Cursors.SizeNWSE;
            }
            else if (!isChildUnderMouse && !Maximized && coords.X >= ClientSize.Width - BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.Right;
                Cursor = Cursors.SizeWE;
            }
            else if (!isChildUnderMouse && !Maximized && coords.Y >= ClientSize.Height - BORDER_WIDTH)
            {
                _resizeDir = ResizeDirection.Bottom;
                Cursor = Cursors.SizeNS;
            }
            else
            {
                _resizeDir = ResizeDirection.None;
                if (_resizeCursors.Contains(Cursor))
                    Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (DesignMode)
                return;
            UpdateButtons(e.Button, e.Location, true);

            // Show system menu on right-click in the custom title bar
            if (e.Button == MouseButtons.Right)
            {
                var cursorPos = e.Location;
                // Only show if in the title bar area (not on buttons)
                var isOverCaption = _statusBarBounds.Contains(cursorPos) &&
                    !(_minButtonBounds.Contains(cursorPos) || _maxButtonBounds.Contains(cursorPos) || _xButtonBounds.Contains(cursorPos));

                // Exclude theme toggle button
                if (isOverCaption && ControlBox && _showThemeButton)
                {
                    bool showMin = MinimizeBox && ControlBox;
                    bool showMax = MaximizeBox && ControlBox;
                    var themeBounds = GetThemeButtonBounds(showMin, showMax);
                    if (!themeBounds.IsEmpty && themeBounds.Contains(cursorPos))
                        isOverCaption = false;
                }

                if (isOverCaption)
                {
                    if (SystemMenuEnabled)
                    {
                        // Convert to screen coordinates
                        var screenPoint = PointToScreen(cursorPos);
                        if (ThemedSystemMenu)
                            ShowThemedSystemMenu(screenPoint);
                        else
                            ShowSystemMenu(screenPoint);
                    }
                }
            }

            base.OnMouseUp(e);
            Win32.ReleaseCapture();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            isActive = true;
            Invalidate();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            isActive = false;
            Invalidate();
        }
        #endregion
        #endregion
    }
}
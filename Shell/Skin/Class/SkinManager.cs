using Editor.Controls;

namespace CustomForm
{
    public class SkinManager
    {
        #pragma warning disable CS8604

        #region Public Objects
        public delegate void SkinManagerEventHandler(object sender);
        public event SkinManagerEventHandler? ThemeChanged;
        public static SkinManager Instance => _instance ?? (_instance = new SkinManager());

        private static Skin MainFormRef;

        public Themes Theme
        {
            get { return _theme; }
            set
            {
                _theme = value;
                UpdateTheme();
                ThemeChanged?.Invoke(this);
            }
        }

        public enum Themes : byte
        {
            LIGHT,
            DARK
        }
        #endregion

        #region Private Objects
        private readonly List<Skin> _formsToManage = new List<Skin>();
        private static SkinManager? _instance;
        private Themes _theme = Themes.LIGHT; // Ensure a consistent default to avoid designer resets
        private bool _suppressGlobalThemeUpdate; // prevents re-updating when a single form is themed
        #endregion

        #region Theme Methods

        public static SkinManager Apply(Skin Form)
        {
            Instance.AddFormToManage(Form);
            return Instance;
        }

        private void AddFormToManage(Skin Form)
        {
            _formsToManage.Add(Form);
            UpdateTheme();

            //save mainform referrence here

            if (Form.isMain)
            {
                MainFormRef = Form;
            }

            // Set background on newly added controls
            Form.ControlAdded += (sender, e) =>
            {
                UpdateControl(e.Control, BackdropColor);
            };
        }

        
        public Skin GetMainForm()
        {
            return MainFormRef;
        }

        /// <summary>
        /// Update all managed forms to the current Theme unless suppressed by a single-form update.
        /// </summary>
        public void UpdateTheme()
        {
            if (_suppressGlobalThemeUpdate) return; // avoid re-updating while a single-form update is in progress

            var newBackColor = BackdropColor;

            foreach (var materialForm in _formsToManage)
            {
                if (materialForm == null) continue;

                bool blur = materialForm.BlurredBackgound;

                // Do not force an opaque BackColor onto a blurred form
                if (!blur)
                    materialForm.BackColor = newBackColor;

                // If blur is on, prefer transparent backgrounds for children (where supported)
                UpdateControl(materialForm, blur ? Color.Transparent : newBackColor);
            }
        }

        /// <summary>
        /// Updates theme to a single skinned form without triggering a global re-update.
        /// </summary>
        /// <param name="SkinForm">The form to update</param>
        /// <param name="theme">The theme to apply to this form</param>
        public void UpdateTheme(Skin SkinForm, Themes theme)
        {
            if (SkinForm == null) return;

            var prevTheme = _theme;
            _suppressGlobalThemeUpdate = true;
            try
            {
                // Temporarily set the manager's theme so getters (e.g., BackdropColor, TextColor) reflect the requested theme
                _theme = theme; // use backing field to avoid firing global updates

                var newBackColor = BackdropColor;
                bool blur = SkinForm.BlurredBackgound;

                // Do not force an opaque BackColor onto a blurred form
                if (!blur)
                    SkinForm.BackColor = newBackColor;

                // If blur is on, prefer transparent backgrounds for children (where supported)
                UpdateControl(SkinForm, blur ? Color.Transparent : newBackColor);

                // Redraw only this form
                SkinForm.Invalidate();
            }
            finally
            {
                _theme = prevTheme; // restore
                _suppressGlobalThemeUpdate = false; // allow global updates again
            }
        }

        private void UpdateControl(Control controlToUpdate, Color newBackColor)
        {
            if (controlToUpdate == null || controlToUpdate is ShellBoxWithScrollBar || controlToUpdate is ScrollBar) return;

            // Apply theme text color to all controls
            controlToUpdate.ForeColor = TextColor;

            if (controlToUpdate.ContextMenuStrip != null)
                UpdateToolStrip(controlToUpdate.ContextMenuStrip, newBackColor);

            if (controlToUpdate is TabPage tab)
            {
                tab.BackColor = newBackColor.A == 0 ? Color.Transparent : newBackColor;
                tab.ForeColor = TextColor;
            }
            else if (controlToUpdate.Parent != null)
            {
                // When asking for transparency, set it explicitly where the control supports it.io
                controlToUpdate.BackColor = newBackColor.A == 0
                    ? Color.Transparent
                    : controlToUpdate.Parent.BackColor;
            }

            // Type-specific visual tweaks
            switch (controlToUpdate)
            {
                case LinkLabel link:
                    link.LinkColor = TextHover;
                    link.ActiveLinkColor = TextHover;
                    link.VisitedLinkColor = TextHover;
                    break;
                case Button btn:
                    if (btn.FlatStyle == FlatStyle.Flat || btn.FlatStyle == FlatStyle.Popup)// || btn.FlatStyle == FlatStyle.Standard)
                    {
                        btn.FlatAppearance.BorderColor = BorderColor;
                        btn.FlatAppearance.MouseOverBackColor = Theme == Themes.LIGHT ? BACKGROUND_HOVER_LIGHT : BACKGROUND_HOVER_DARK;
                        btn.FlatAppearance.MouseDownBackColor = Theme == Themes.LIGHT ? BACKGROUND_FOCUS_LIGHT : BACKGROUND_FOCUS_DARK;
                    }
                    break;
                case DataGridView dgv:
                    try
                    {
                        var effectiveBack = newBackColor.A == 0 ? controlToUpdate.BackColor : newBackColor;
                        dgv.EnableHeadersVisualStyles = false;
                        dgv.BackgroundColor = effectiveBack;
                        dgv.GridColor = BorderColor;

                        // Default cells
                        var dc = dgv.DefaultCellStyle;
                        dc.BackColor = effectiveBack;
                        dc.ForeColor = TextColor;
                        dc.SelectionBackColor = Theme == Themes.LIGHT ? BACKGROUND_FOCUS_LIGHT : BACKGROUND_FOCUS_DARK;
                        dc.SelectionForeColor = TextColor;

                        // Alternating rows
                        var alt = dgv.AlternatingRowsDefaultCellStyle;
                        alt.BackColor = effectiveBack;
                        alt.ForeColor = TextColor;
                        alt.SelectionBackColor = Theme == Themes.LIGHT ? BACKGROUND_FOCUS_LIGHT : BACKGROUND_FOCUS_DARK;
                        alt.SelectionForeColor = TextColor;

                        // Headers
                        var ch = dgv.ColumnHeadersDefaultCellStyle;
                        ch.BackColor = PrimaryColor;
                        ch.ForeColor = TextColor;
                        ch.SelectionBackColor = PrimaryColor;
                        ch.SelectionForeColor = TextColor;

                        var rh = dgv.RowHeadersDefaultCellStyle;
                        rh.BackColor = PrimaryColor;
                        rh.ForeColor = TextColor;
                        rh.SelectionBackColor = PrimaryColor;
                        rh.SelectionForeColor = TextColor;
                    }
                    catch { /* some properties may throw if handle not created; ignore */ }
                    break;
                case TabControl tc:
                    tc.ForeColor = TextColor;
                    foreach (TabPage page in tc.TabPages)
                    {
                        page.ForeColor = TextColor;
                        if (newBackColor.A != 0)
                            page.BackColor = newBackColor;
                    }
                    break;
                case ToolStrip ts:
                    UpdateToolStrip(ts, newBackColor);
                    break;
            }

            foreach (Control child in controlToUpdate.Controls)
            {
                UpdateControl(child, newBackColor);
            }
        }

        private void UpdateToolStrip(ToolStrip toolStrip, Color newBackColor)
        {
            if (toolStrip == null)
            {
                return;
            }

            toolStrip.BackColor = newBackColor;
            toolStrip.ForeColor = TextColor;

            foreach (ToolStripItem control in toolStrip.Items)
            {
                control.BackColor = newBackColor;
                control.ForeColor = TextColor;
            }
        }

        private void InvalidateManagedForms()
        {
            foreach (var f in _formsToManage) f?.Invalidate();
        }

        #endregion

        #region Private Colours
        private Color BORDERLIGHT = (0xCCCEDB).ToColor();
        private Color BORDERDARK = (0x424242).ToColor();

        //Dark - Generic back colors - for user controls
        private static Color BACKGROUND_HOVER_DARK = Color.FromArgb(20, 255, 255, 255);
        private static Brush BACKGROUND_HOVER_DARK_BRUSH = new SolidBrush(BACKGROUND_HOVER_DARK);
        private static Color BACKGROUND_FOCUS_DARK = Color.FromArgb(30, 255, 255, 255);
        private static Brush BACKGROUND_FOCUS_DARK_BRUSH = new SolidBrush(BACKGROUND_FOCUS_DARK);
        private static Color BACKGROUND_HOVER_RED = Color.FromArgb(255, 255, 0, 0);
        private static Brush BACKGROUND_HOVER_RED_BRUSH = new SolidBrush(BACKGROUND_HOVER_RED);
        private static Color BACKGROUND_DOWN_RED = Color.FromArgb(255, 255, 84, 54);
        private static Brush BACKGROUND_DOWN_RED_BRUSH = new SolidBrush(BACKGROUND_DOWN_RED);
        // Backdrop colors - for containers, like forms or panels
        private static Color BACKDROP_DARK = (0x1F1F1F).ToColor(); //Color.FromArgb(255, 50, 50, 50); // Color.FromArgb(30, 30, 30);
        private static Color DIVIDERS_DARK = Color.FromArgb(30, 0, 0, 0); // Alpha 30%

        //Light - Generic back colors - for user controls
        private static Color BACKGROUND_HOVER_LIGHT = Color.FromArgb(20, 0, 0, 0);
        private static Brush BACKGROUND_HOVER_LIGHT_BRUSH = new SolidBrush(BACKGROUND_HOVER_LIGHT);
        private static Color BACKGROUND_FOCUS_LIGHT = (0xC9DEF5).ToColor();// Color.FromArgb(30, 0, 0, 0);
        private static Brush BACKGROUND_FOCUS_LIGHT_BRUSH = new SolidBrush(BACKGROUND_FOCUS_LIGHT);
        // Backdrop colors - for containers, like forms or panels
        private static Color BACKDROP_LIGHT = Color.FromArgb(255, 242, 242, 242);
        private static Color DIVIDERS_LIGHT = Color.FromArgb(30, 255, 255, 255); // Alpha 30%

        private static Color PRIMARY_LIGHT = (0xEEEEF2).ToColor();
        private static Brush PRIMARY_LIGHT_BRUSH = new SolidBrush(PRIMARY_LIGHT);

        private static Color PRIMARY_DARK = (0x1F1F1F).ToColor();
        private static Brush PRIMARY_DARK_BRUSH = new SolidBrush(PRIMARY_DARK);

        private static Color TEXT_DARK = (0x212121).ToColor();
        private static Color TEXT_LIGHT = (0xD6D6D6).ToColor();

        private static Color TEXT_DARK_DISABLED = (0x5C5C5C).ToColor();
        private static Color TEXT_LIGHT_DISABLED = (0xB6B6B6).ToColor();

        private static Color TEXT_HOVER_LIGHT = (0x006CBE).ToColor();
        private static Color TEXT_HOVER_DARK = (0xD6D6D6).ToColor();

        //border light and dark
        private static int BORDERCOLOR_DARK = (232 << 16) | (96 << 8) | 113;
        private static int BORDERCOLOR_LIGHT = (0xB9 << 16) | (0x9F << 8) | 0x9B;
        private static int INACTIVEBORDERCOLOR_DARK = (61 << 16) | (61 << 8) | 61;
        private static int INACTIVEBORDERCOLOR_LIGHT = 0xD6D6D6;

        private static Color CLOSE_COLOR_DARK = Color.FromArgb(185, 185, 185, 185);// (0xB7B7B7).ToColor();
        private static Color CLOSE_COLOR_LIGHT = (0x494949).ToColor(); //Color.FromArgb(73, 73, 73, 73);// (0xB7B7B7).ToColor();

        #endregion

        #region Public Colors
        public Brush BackgroundFocusBrush => Theme == Themes.LIGHT ? BACKGROUND_FOCUS_LIGHT_BRUSH : BACKGROUND_FOCUS_DARK_BRUSH;

        // Update backing color for current theme then refresh form backgrounds
        public Color BackdropColor
        {
            get => Theme == Themes.LIGHT ? BACKDROP_LIGHT : BACKDROP_DARK;
            set
            {
                if (Theme == Themes.LIGHT) BACKDROP_LIGHT = value; else BACKDROP_DARK = value;
                UpdateTheme();
                InvalidateManagedForms();
            }
        }

        // Update divider color for current theme and invalidate forms (paint uses this value)
        public Color DividersColor
        {
            get => Theme == Themes.LIGHT ? DIVIDERS_DARK : DIVIDERS_DARK;
            set
            {
                if (Theme == Themes.LIGHT) DIVIDERS_LIGHT = value; else DIVIDERS_DARK = value;
                InvalidateManagedForms();
            }
        }

        public static Brush BackgroundDownRedBrush => BACKGROUND_DOWN_RED_BRUSH;
        public static Brush BackgroundHoverBrush => BACKGROUND_HOVER_DARK_BRUSH;
        public static Brush BackgroundHoverRedBrush => BACKGROUND_HOVER_RED_BRUSH;

        public Brush PrimaryBrush => Theme == Themes.LIGHT ? PRIMARY_LIGHT_BRUSH : PRIMARY_DARK_BRUSH;
        public Color PrimaryColor
        {
            get => Theme == Themes.LIGHT ? PRIMARY_LIGHT : PRIMARY_DARK;
            set
            {
                if (Theme == Themes.LIGHT)
                {
                    PRIMARY_LIGHT = value;
                    PRIMARY_LIGHT_BRUSH.Dispose();
                    PRIMARY_LIGHT_BRUSH = new SolidBrush(PRIMARY_LIGHT);
                }
                else
                {
                    PRIMARY_DARK = value;
                    PRIMARY_DARK_BRUSH.Dispose();
                    PRIMARY_DARK_BRUSH = new SolidBrush(PRIMARY_DARK);
                }
                InvalidateManagedForms();
            }
        }

        public Color TextColor
        {
            get => Theme == Themes.LIGHT ? TEXT_DARK : TEXT_LIGHT;
            set
            {
                if (Theme == Themes.LIGHT) TEXT_DARK = value; else TEXT_LIGHT = value;
                InvalidateManagedForms();
            }
        }

        public Color TextColorDisabled
        {
            get => Theme == Themes.LIGHT ? TEXT_LIGHT_DISABLED : TEXT_DARK_DISABLED;
            set
            {
                if (Theme == Themes.LIGHT) TEXT_LIGHT_DISABLED = value; else TEXT_DARK_DISABLED = value;
                InvalidateManagedForms();
            }
        }

        public Color TextHover
        {
            get => Theme == Themes.LIGHT ? TEXT_HOVER_LIGHT : TEXT_HOVER_DARK;
            set
            {
                if (Theme == Themes.LIGHT) TEXT_HOVER_LIGHT = value; else TEXT_HOVER_DARK = value;
                InvalidateManagedForms();
            }
        }

        public Color CloseColor
        {
            get => Theme == Themes.LIGHT ? CLOSE_COLOR_LIGHT : CLOSE_COLOR_DARK;
            set
            {
                if (Theme == Themes.LIGHT) CLOSE_COLOR_LIGHT = value; else CLOSE_COLOR_DARK = value;
                InvalidateManagedForms();
            }
        }

        public Color BorderColor
        {
            get => Theme == Themes.LIGHT ? BORDERLIGHT : BORDERDARK;
            set
            {
                if (Theme == Themes.LIGHT) BORDERLIGHT = value; else BORDERDARK = value;
                InvalidateManagedForms();
            }
        }

        public int FormBorderColor
        {
            get => Theme == Themes.LIGHT ? BORDERCOLOR_LIGHT : BORDERCOLOR_DARK;
            set
            {
                if (Theme == Themes.LIGHT) BORDERCOLOR_LIGHT = value; else BORDERCOLOR_DARK = value;
                InvalidateManagedForms();
            }
        }
        public int InactiveBorderColor
        {
            get => Theme == Themes.LIGHT ? INACTIVEBORDERCOLOR_LIGHT : INACTIVEBORDERCOLOR_DARK;
            set
            {
                if (Theme == Themes.LIGHT) INACTIVEBORDERCOLOR_LIGHT = value; else INACTIVEBORDERCOLOR_DARK = value;
                InvalidateManagedForms();
            }
        }
        #endregion
    }
}

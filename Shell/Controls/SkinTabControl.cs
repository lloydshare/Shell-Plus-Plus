namespace MaterialSkin.Controls
{
    //using Editor;
    using CustomForm;
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;
    using MaterialSkin.Animations;

    public class MaterialTabControl : TabControl, ISkin
    {
        #region private
        // Drawer overlay and speed improvements
        private bool _drawerShowIconsWhenHidden;
        private bool _drawerAutoHide;
        private bool _drawerAutoShow;
        private bool _drawerIsOpen;
        private bool _drawerUseColors;
        private bool _drawerHighlightWithAccent;
        private bool _backgroundWithAccent;
        private MaterialDrawer drawerControl = new MaterialDrawer();
        private AnimationManager _drawerShowHideAnimManager;
        private readonly AnimationManager _clickAnimManager;

        #endregion

        public MaterialTabControl()
        {
            Multiline = true;
        }

        #region Public
        [Browsable(false)]
        public SkinManager SkinManager => CustomForm.SkinManager.Instance;

        [Category("Drawer")]
        public int DrawerWidth { get; set; }

        [Category("Drawer")]
        public bool DrawerAutoHide
        {
            get => _drawerAutoHide;
            set => drawerControl.AutoHide = _drawerAutoHide = value;
        }

        [Category("Drawer")]
        public bool DrawerAutoShow
        {
            get => _drawerAutoShow;
            set => drawerControl.AutoShow = _drawerAutoShow = value;
        }

        [Category("Drawer")]
        public int DrawerIndicatorWidth { get; set; }

        [Category("Drawer")]
        public bool DrawerIsOpen
        {
            get => _drawerIsOpen;
            set
            {
                if (_drawerIsOpen == value) return;

                _drawerIsOpen = value;

                if (value)
                    drawerControl?.Show();
                else
                    drawerControl?.Hide();
            }
        }

        [Category("Drawer")]
        public bool DrawerUseColors
        {
            get => _drawerUseColors;
            set
            {
                if (_drawerUseColors == value) return;

                _drawerUseColors = value;

                if (drawerControl == null) return;

                drawerControl.UseColors = value;
                drawerControl.Refresh();
            }
        }

        [Category("Drawer")]
        public bool DrawerHighlightWithAccent
        {
            get => _drawerHighlightWithAccent;
            set
            {
                if (_drawerHighlightWithAccent == value) return;

                _drawerHighlightWithAccent = value;

                if (drawerControl == null) return;

                drawerControl.HighlightWithAccent = value;
                drawerControl.Refresh();
            }
        }

        [Category("Drawer")]
        public bool DrawerBackgroundWithAccent
        {
            get => _backgroundWithAccent;
            set
            {
                if (_backgroundWithAccent == value) return;

                _backgroundWithAccent = value;

                if (drawerControl == null) return;

                drawerControl.BackgroundWithAccent = value;
                drawerControl.Refresh();
            }
        }

        [Category("Drawer")]
        public MaterialTabControl DrawerTabControl { get; set; }

        #endregion

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x1328 && !DesignMode) m.Result = (IntPtr)1;
            else base.WndProc(ref m);
        }
    }
}
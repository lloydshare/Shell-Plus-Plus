using System.Runtime.InteropServices;

namespace CustomForm
{
    [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight; }

        public enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_ENABLE_HOSTBACKDROP = 5,
            ACCENT_INVALID_STATE = 6
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        public enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_SYSTEMBACKDROP_TYPE = 38,
            DWMWA_NCRENDERING_POLICY = 2,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2
        }

        public enum DWM_SYSTEMBACKDROP_TYPE
        {
            DWMSBT_AUTO = 0,
            DWMSBT_NONE = 1,
            DWMSBT_MAINWINDOW = 2, // Mica
            DWMSBT_TRANSIENTWINDOW = 3, // Acrylic-like blur
            DWMSBT_TABBEDWINDOW = 4 // Mica Alt
        }
        /// <summary>
        /// Defines the MouseState
        /// </summary>
        public enum MouseState
        {
            /// <summary>
            /// Defines the HOVER
            /// </summary>
            HOVER,

            /// <summary>
            /// Defines the DOWN
            /// </summary>
            DOWN,

            /// <summary>
            /// Defines the OUT
            /// </summary>
            OUT
        }
        /// <summary>
        /// Window Messages
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/winmsg/about-messages-and-message-queues"/>
        /// </summary>
        public enum WM
        {
            /// <summary>
            /// WM_NCCALCSIZE
            /// </summary>
            NonClientCalcSize = 0x0083,
            /// <summary>
            /// WM_NCACTIVATE
            /// </summary>
            NonClientActivate = 0x0086,
            /// <summary>
            /// WM_NCLBUTTONDOWN
            /// </summary>
            NonClientLeftButtonDown = 0x00A1,
            /// <summary>
            /// WM_SYSCOMMAND
            /// </summary>
            SystemCommand = 0x0112,
            /// <summary>
            /// WM_MOUSEMOVE
            /// </summary>
            MouseMove = 0x0200,
            /// <summary>
            /// WM_LBUTTONDOWN
            /// </summary>
            LeftButtonDown = 0x0201,
            /// <summary>
            /// WM_LBUTTONUP
            /// </summary>
            LeftButtonUp = 0x0202,
            /// <summary>
            /// WM_LBUTTONDBLCLK
            /// </summary>
            LeftButtonDoubleClick = 0x0203,
            /// <summary>
            /// WM_RBUTTONDOWN
            /// </summary>
            RightButtonDown = 0x0204,
        }
        /// <summary>
        /// Hit Test Results
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-nchittest"/>
        /// </summary>
        public enum HT
        {
            /// <summary>
            /// HTNOWHERE - Nothing under cursor
            /// </summary>
            None = 0,
            /// <summary>
            /// HTCAPTION - Titlebar
            /// </summary>
            Caption = 2,
            /// <summary>
            /// HTLEFT - Left border
            /// </summary>
            Left = 10,
            /// <summary>
            /// HTRIGHT - Right border
            /// </summary>
            Right = 11,
            /// <summary>
            /// HTTOP - Top border
            /// </summary>
            Top = 12,
            /// <summary>
            /// HTTOPLEFT - Top left corner
            /// </summary>
            TopLeft = 13,
            /// <summary>
            /// HTTOPRIGHT - Top right corner
            /// </summary>
            TopRight = 14,
            /// <summary>
            /// HTBOTTOM - Bottom border
            /// </summary>
            Bottom = 15,
            /// <summary>
            /// HTBOTTOMLEFT - Bottom left corner
            /// </summary>
            BottomLeft = 16,
            /// <summary>
            /// HTBOTTOMRIGHT - Bottom right corner
            /// </summary>
            BottomRight = 17,
        }
        /// <summary>
        /// Window Styles
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/winmsg/window-styles"/>
        /// </summary>
        public enum WS
        {
            /// <summary>
            /// WS_MINIMIZEBOX - Allow minimizing from taskbar
            /// </summary>
            MinimizeBox = 0x20000,
            /// <summary>
            /// WS_SIZEFRAME - Required for Aero Snapping
            /// </summary>
            SizeFrame = 0x40000,
            /// <summary>
            /// WS_SYSMENU - Trigger the creation of the system menu
            /// </summary>
            SysMenu = 0x80000,
        }
        /// <summary>
        /// Various directions the form can be resized in
        /// </summary>
        public enum ResizeDirection
        {
            BottomLeft,
            Left,
            Right,
            BottomRight,
            Bottom,
            Top,
            TopLeft,
            TopRight,
            None
        }
        /// <summary>
        /// The states a button can be in
        /// </summary>
        public enum ButtonState
        {
            XOver,
            MaxOver,
            MinOver,
            DrawerOver,
            XDown,
            MaxDown,
            MinDown,
            DrawerDown,
            None
        }
}

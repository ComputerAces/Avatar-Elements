// --- Helpers/NativeMethods.cs ---
// Contains P/Invoke declarations for Windows API functions used by the application.
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms; // For Keys enum definition (though not directly used in ModifierKeys)

namespace Avatar_Elements.Helpers {
    /// <summary>
    /// Provides P/Invoke declarations for native Windows API functions.
    /// Marked internal as it's intended for use only within this assembly.
    /// </summary>
    internal static class NativeMethods {
        #region Constants

        // Window Messages
        public const int WM_HOTKEY = 0x0312;

        // GetWindowLong constants
        public const int GWL_EXSTYLE = -20;

        // Extended Window Styles
        public const uint WS_EX_LAYERED = 0x80000;
        public const uint WS_EX_TRANSPARENT = 0x20; // Example: If needed for click-through

        // UpdateLayeredWindow constants
        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x02;
        public const int ULW_OPAQUE = 0x04; // Example: If needed

        #endregion

        #region Enums

        // Modifiers for RegisterHotKey fsModifiers parameter
        [Flags]
        public enum ModifierKeys : uint {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8 // Note: Use with care in RegisterHotKey
        }

        #endregion

        #region Structs

        // Point structure for API calls
        [StructLayout(LayoutKind.Sequential)]
        public struct Point {
            public int x;
            public int y;

            public Point(int x, int y) { this.x = x; this.y = y; }
        }

        // Size structure for API calls
        [StructLayout(LayoutKind.Sequential)]
        public struct Size {
            public int cx; // Width
            public int cy; // Height

            public Size(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        // Blend function structure for UpdateLayeredWindow
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        #endregion

        #region User32.dll Functions (Windowing, Input, Messages)

        // Used to register a system-wide hot key.
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // Used to unregister a system-wide hot key.
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Retrieves information about the specified window (e.g., extended styles).
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        // Changes an attribute of the specified window (e.g., extended styles).
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        // Retrieves a handle to a device context (DC) for the client area of a specified window or for the entire screen.
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        // Releases a device context (DC), freeing it for use by other applications.
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        // Updates the position, size, shape, content, and translucency of a layered window.
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(
            IntPtr hwnd,        // Handle to layered window
            IntPtr hdcDst,      // Handle to screen DC (can be null)
            [In] ref Point pptDst, // New screen position (optional, Point* in C++) -> use ref Point
            [In] ref Size psize,    // New size (optional, Size* in C++) -> use ref Size
            IntPtr hdcSrc,      // Handle to source DC (memory DC with bitmap)
            [In] ref Point pptSrc, // Location of layer in source DC (optional, Point* in C++) -> use ref Point
            uint crKey,         // Color key (0 for alpha blending)
            [In] ref BLENDFUNCTION pblend, // Blend function (optional, BLENDFUNCTION* in C++) -> use ref BLENDFUNCTION
            uint dwFlags        // Blend options (ULW_ALPHA or ULW_OPAQUE)
        );

        #endregion

        #region Gdi32.dll Functions (Graphics Device Interface)

        // Creates a memory device context (DC) compatible with the specified device.
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        // Deletes the specified device context (DC).
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hdc);

        // Selects an object into the specified device context (DC). The new object replaces the previous object of the same type.
        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        // Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object.
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        #endregion

    } // End of NativeMethods class
} // End of namespace
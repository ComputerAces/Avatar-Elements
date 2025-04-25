// --- Helpers/HotkeyHandlerWindow.cs ---
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Avatar_Elements.Data; // Assuming HotkeyConfig is here

namespace Avatar_Elements.Helpers {
    /// <summary>
    /// Handles registration and message processing for system-wide hotkeys
    /// using a dedicated message-only window.
    /// </summary>
    internal class HotkeyHandlerWindow : NativeWindow, IDisposable {
        // Event raised when a registered hotkey is pressed
        public event Action<int> HotkeyActivated; // Passes the registered hotkey ID

        // Dictionary to keep track of registered hotkeys (ID -> Config is useful for logging)
        private readonly Dictionary<int, HotkeyConfig> _registeredHotkeys = new Dictionary<int, HotkeyConfig>();
        private bool _disposed = false;

        public HotkeyHandlerWindow()
        {
            // Create a message-only window handle.
            try
            {
                CreateParams cp = new CreateParams();
                // cp.Parent = (IntPtr)(-3); // Setting Parent to HWND_MESSAGE can sometimes cause issues, try without first.
                this.CreateHandle(cp);
                Debug.WriteLine($"HotkeyHandlerWindow: Handle created successfully: {this.Handle}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HotkeyHandlerWindow: CRITICAL ERROR creating handle: {ex.Message}");
                // If handle fails to create, this instance is useless.
                // Consider throwing or handling this more gracefully.
                MessageBox.Show($"Failed to create hotkey handler window: {ex.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Registers a system-wide hotkey.
        /// </summary>
        /// <param name="id">A unique ID for this hotkey within the application.</param>
        /// <param name="config">The HotkeyConfig defining the key combination.</param>
        /// <returns>True if registration was successful, false otherwise.</returns>
        public bool RegisterHotkey(int id, HotkeyConfig config)
        {
            // --- ADDED Checks ---
            if (_disposed)
            {
                Debug.WriteLine($"HotkeyHandlerWindow: RegisterHotkey({id}) called on disposed object.");
                return false;
            }
            if (this.Handle == IntPtr.Zero)
            {
                Debug.WriteLine($"HotkeyHandlerWindow: RegisterHotkey({id}) called but handle is Zero.");
                return false; // Cannot register without a handle
            }
            // --- End Added Checks ---

            if (config == null || config.Key == Keys.None)
            {
                Debug.WriteLine($"HotkeyHandlerWindow: RegisterHotkey({id}) skipped - config null or Key is None.");
                return false; // Don't register 'None'
            }

            // If this ID is already registered, unregister it first for safety
            if (_registeredHotkeys.ContainsKey(id))
            {
                Debug.WriteLine($"HotkeyHandlerWindow: Hotkey ID {id} already registered. Unregistering first.");
                UnregisterHotkey(id);
            }

            uint modifiers = (uint)NativeMethods.ModifierKeys.None;
            if (config.Alt) modifiers |= (uint)NativeMethods.ModifierKeys.Alt;
            if (config.Control) modifiers |= (uint)NativeMethods.ModifierKeys.Control;
            if (config.Shift) modifiers |= (uint)NativeMethods.ModifierKeys.Shift;
            uint vk = (uint)config.Key;

            bool success = false;
            try
            {
                Debug.WriteLine($"HotkeyHandlerWindow: Attempting RegisterHotKey ID {id} ('{config}') on handle {this.Handle}...");
                success = NativeMethods.RegisterHotKey(this.Handle, id, modifiers, vk);

                if (!success)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"HotkeyHandlerWindow: RegisterHotKey FAILED for ID {id}. Error Code: {errorCode}");
                    // Removed MessageBox from here - let caller decide how to report UI errors
                }
                else
                {
                    Debug.WriteLine($"HotkeyHandlerWindow: RegisterHotKey SUCCEEDED for ID {id}.");
                    _registeredHotkeys[id] = config; // Store successful registration
                }
            }
            catch (Exception ex) // Catch potential exceptions during P/Invoke
            {
                Debug.WriteLine($"HotkeyHandlerWindow: EXCEPTION during RegisterHotKey for ID {id}: {ex.Message}");
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Unregisters a specific hotkey by its ID.
        /// </summary>
        /// <param name="id">The unique ID of the hotkey to unregister.</param>
        public void UnregisterHotkey(int id)
        {
            if (_disposed) return;

            if (_registeredHotkeys.ContainsKey(id))
            {
                // --- ADDED Handle Check ---
                if (this.Handle == IntPtr.Zero)
                {
                    Debug.WriteLine($"HotkeyHandlerWindow: UnregisterHotkey({id}) skipped - handle is Zero.");
                    _registeredHotkeys.Remove(id); // Remove tracking even if we can't call API
                    return;
                }
                // --- End Added Check ---

                HotkeyConfig config = _registeredHotkeys[id]; // Get for logging
                Debug.WriteLine($"HotkeyHandlerWindow: Attempting UnregisterHotKey ID {id} ('{config}') on handle {this.Handle}...");
                if (!NativeMethods.UnregisterHotKey(this.Handle, id))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    // --- MODIFIED Logging ---
                    Debug.WriteLine($"HotkeyHandlerWindow: UnregisterHotKey FAILED for ID {id}. Error: {errorCode}. (This might be okay if handle changed or already unregistered).");
                }
                else
                {
                    // --- ADDED Logging ---
                    Debug.WriteLine($"HotkeyHandlerWindow: UnregisterHotKey SUCCEEDED for ID {id}.");
                }
                _registeredHotkeys.Remove(id); // Always remove from our tracking after attempt
            }
            else
            {
                Debug.WriteLine($"HotkeyHandlerWindow: Attempted to unregister ID {id}, but it wasn't tracked.");
            }
        }

        /// <summary>
        /// Unregisters all currently tracked hotkeys.
        /// </summary>
        public void UnregisterAllHotkeys()
        {
            if (_disposed) return;
            // Get keys first as modifying collection while iterating causes issues
            List<int> idsToUnregister = new List<int>(_registeredHotkeys.Keys);
            Debug.WriteLine($"HotkeyHandlerWindow: Unregistering all {idsToUnregister.Count} tracked hotkeys...");
            foreach (int id in idsToUnregister)
            {
                UnregisterHotkey(id); // This method now contains logging and handle checks
            }
            // Double-check dictionary is clear after attempts
            if (_registeredHotkeys.Count > 0)
            {
                Debug.WriteLine($"Warning: _registeredHotkeys count is {_registeredHotkeys.Count} after UnregisterAllHotkeys.");
                _registeredHotkeys.Clear(); // Force clear if needed
            }
        }

        /// <summary>
        /// Overrides the window procedure to process messages, looking for WM_HOTKEY.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            // Log ALL messages briefly during debug to see if window is active
            // System.Diagnostics.Debug.WriteLine($"HotkeyHandlerWindow Msg: {m.Msg}");

            if (!_disposed && m.Msg == NativeMethods.WM_HOTKEY) // Check disposed flag
            {
                int id = m.WParam.ToInt32();
                Debug.WriteLine($"HotkeyHandlerWindow: WM_HOTKEY received! ID={id}, WParam={m.WParam}, LParam={m.LParam}");

                if (_registeredHotkeys.ContainsKey(id))
                {
                    Debug.WriteLine($"HotkeyHandlerWindow: Raising HotkeyActivated event for ID {id}.");
                    try
                    {
                        HotkeyActivated?.Invoke(id); // Raise event safely
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"HotkeyHandlerWindow: EXCEPTION in HotkeyActivated event handler: {ex.Message}");
                        // Swallow exceptions from handlers? Or let them bubble up? Depends on desired robustness.
                    }
                }
                else
                {
                    Debug.WriteLine($"HotkeyHandlerWindow: Received WM_HOTKEY for unrecognized/untracked ID: {id}");
                }
                // Indicate message was handled? Usually not strictly necessary for WM_HOTKEY
                // m.Result = IntPtr.Zero;
                // return; // Don't call base if handled? Test this - base likely does nothing for WM_HOTKEY.
            }

            // Call base for default processing ONLY if handle still exists and not disposed
            if (!_disposed && this.Handle != IntPtr.Zero)
            {
                try
                {
                    base.WndProc(ref m);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"HotkeyHandlerWindow: Exception in base.WndProc: {ex.Message}");
                    // This might happen if handle becomes invalid between check and call
                }
            }
        }

        // Implement IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // Check thread affinity - Dispose should happen on the thread that created the handle
                // if (InvokeRequired) { BeginInvoke(new Action(() => Dispose(disposing))); return; } // Complex - avoid if possible

                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    Debug.WriteLine("HotkeyHandlerWindow: Disposing managed resources (none).");
                    // None in this specific class to dispose here
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                Debug.WriteLine("HotkeyHandlerWindow: Disposing unmanaged resources - Unregistering hotkeys and destroying handle...");
                UnregisterAllHotkeys(); // Ensure all are unregistered

                // Destroy the window handle explicitly
                if (this.Handle != IntPtr.Zero)
                {
                    try
                    {
                        this.DestroyHandle();
                        Debug.WriteLine("HotkeyHandlerWindow: Handle destroyed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"HotkeyHandlerWindow: Exception during DestroyHandle: {ex.Message}");
                        // Handle might already be invalid
                    }
                }
                _disposed = true;
            }
        }

        // Finalizer in case Dispose wasn't called explicitly (good practice)
        ~HotkeyHandlerWindow()
        {
            Debug.WriteLine("HotkeyHandlerWindow: Finalizer called. Disposing resources.");
            Dispose(false);
        }
    }
}
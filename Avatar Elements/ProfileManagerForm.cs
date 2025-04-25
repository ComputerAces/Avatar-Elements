using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // Potentially needed if interacting directly with paths
using System.Diagnostics; // For Debug.WriteLine

// Reference your namespaces
using Avatar_Elements.Data;
using Avatar_Elements.Helpers;
using System.Runtime.InteropServices;
// Assuming ProfileEditForm is in the same namespace or referenced
// using Avatar_Elements.Forms;

namespace Avatar_Elements {
    public partial class ProfileManagerForm : Form {
        // --- Member Variables ---
        private AppSettings currentSettings;
        private bool _isListeningForGlobalHotkey = false;

        private const int TEMP_GLOBAL_HOTKEY_ID = 0x9001; // Unique ID for temporary check
        private Action<AvatarProfile> _requestPreviewUpdateAction;

        // --- Constructor ---
        public ProfileManagerForm(Action<AvatarProfile> requestPreviewUpdateAction = null) // <<< MODIFIED: Added parameter
        {
            InitializeComponent(); // Creates controls defined in Designer.cs
            _requestPreviewUpdateAction = requestPreviewUpdateAction; // <<< Store the action

            // Enable KeyPreview for global hotkey capture on this form
            this.KeyPreview = true;

            // Wire up all event handlers programmatically
            WireUpEvents();
        }

        // --- Event Wiring (Called from Constructor) ---
        private void WireUpEvents()
        {
            // Form events
            this.Load += ProfileManagerForm_Load;
            this.KeyDown += ProfileManagerForm_KeyDown; // For global hotkey capture

            // ListView event
            listViewAvatarProfiles.SelectedIndexChanged += ListViewAvatarProfiles_SelectedIndexChanged;
            listViewAvatarProfiles.DoubleClick += ListViewAvatarProfiles_DoubleClick; // Edit on double click

            // Button events (Ensure control names match your Designer.cs)
            btnAddProfile.Click += BtnAddProfile_Click;
            btnEditProfile.Click += BtnEditProfile_Click;
            btnRemoveProfile.Click += BtnRemoveProfile_Click;
            btnSetGlobalHideShowHotkey.Click += BtnSetGlobalHideShowHotkey_Click;
            btnSaveSettings.Click += BtnSaveSettings_Click;
            butClose.Click += ButClose_Click; // Matches designer name 'butClose'

            // Add wiring for other controls if needed (e.g., CheckBox CheckChanged)
            // chkStartMinimized.CheckedChanged += ChkStartMinimized_CheckedChanged; // Example
        }

        // --- Form Load ---
        private void ProfileManagerForm_Load(object sender, EventArgs e)
        {
            // Load settings first
            LoadSettingsAndPopulateForm(); 
            // Events are already wired by the constructor call to WireUpEvents()
            UpdateEditRemoveButtonStates(); // Set initial state after loading
        }

        // --- Data Loading & UI Population ---
        private void LoadSettingsAndPopulateForm()
        {
            currentSettings = SettingsManager.LoadSettings();
            if (currentSettings == null) // Should be handled by SettingsManager returning default
            {
                currentSettings = new AppSettings();
                Debug.WriteLine("Warning: SettingsManager returned null, using default AppSettings.");
            }

            // Populate global settings controls
            chkStartMinimized.Checked = currentSettings.StartMinimized;
            UpdateGlobalHotkeyDisplay();

            // Populate the list view with avatar profiles
            PopulateProfileListView();
        }

        private void PopulateProfileListView()
        {
            listViewAvatarProfiles.Items.Clear();
            if (currentSettings?.Profiles == null) return; // Safety check

            foreach (var profile in currentSettings.Profiles)
            {
                var item = new ListViewItem(profile.ProfileName ?? "Unnamed"); // Column 0: Name
                item.SubItems.Add(profile.Hotkey?.ToString() ?? "None"); // Column 1: Hotkey
                item.SubItems.Add(profile.BaseImagePath ?? ""); // Column 2: Image Path
                item.Tag = profile.Id; // Store the unique ID for later retrieval

                listViewAvatarProfiles.Items.Add(item);
            }
        }

        private void UpdateGlobalHotkeyDisplay()
        {
            txtGlobalHideShowHotkey.Text = currentSettings.GlobalHideShowHotkey?.ToString() ?? "None";
        }

        // --- UI State Updates ---
        private void UpdateEditRemoveButtonStates()
        {
            bool itemSelected = listViewAvatarProfiles.SelectedItems.Count > 0;
            btnEditProfile.Enabled = itemSelected;
            btnRemoveProfile.Enabled = itemSelected;
        }

        // --- Event Handlers ---

        private void ListViewAvatarProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEditRemoveButtonStates();
        }

        private void ListViewAvatarProfiles_DoubleClick(object sender, EventArgs e)
        {
            // Allow editing on double-click
            if (listViewAvatarProfiles.SelectedItems.Count > 0)
            {
                BtnEditProfile_Click(sender, e); // Just call the edit button's handler
            }
        }

        private void BtnAddProfile_Click(object sender, EventArgs e)
        {
            var newProfile = new AvatarProfile();

            // Pass the stored preview update action to the edit form's constructor
            using (var editForm = new ProfileEditForm(newProfile, currentSettings, _requestPreviewUpdateAction)) // <<< MODIFIED: Pass action
            {
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    currentSettings.Profiles.Add(newProfile);
                    PopulateProfileListView();

                    ListViewItem newItem = listViewAvatarProfiles.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag is Guid && (Guid)i.Tag == newProfile.Id);
                    if (newItem != null)
                    {
                        newItem.Selected = true;
                        newItem.EnsureVisible();
                        listViewAvatarProfiles.Focus();
                    }
                    // Trigger save/refresh in parent form? SettingsSaved event might be better here.
                    // For now, rely on BtnSaveSettings_Click or form closing.
                }
            }
        }

        private void BtnEditProfile_Click(object sender, EventArgs e)
        {
            if (listViewAvatarProfiles.SelectedItems.Count == 0) return;
            if (!(listViewAvatarProfiles.SelectedItems[0].Tag is Guid profileId)) return;

            AvatarProfile profileToEdit = currentSettings.GetProfileById(profileId);
            if (profileToEdit == null)
            { /* ... error handling ... */ return; }

            // Pass the stored preview update action to the edit form's constructor
            using (var editForm = new ProfileEditForm(profileToEdit, currentSettings, _requestPreviewUpdateAction)) // <<< MODIFIED: Pass action
            {
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    PopulateProfileListView();

                    ListViewItem editedItem = listViewAvatarProfiles.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag is Guid && (Guid)i.Tag == profileId);
                    if (editedItem != null)
                    {
                        editedItem.Selected = true;
                        editedItem.EnsureVisible();
                        listViewAvatarProfiles.Focus();
                    }
                    // Trigger save/refresh in parent form? SettingsSaved event might be better here.
                    // For now, rely on BtnSaveSettings_Click or form closing.
                }
                // If cancelled, ProfileEditForm reverts changes internally before closing
            }
        }

        private void BtnRemoveProfile_Click(object sender, EventArgs e)
        {
            if (listViewAvatarProfiles.SelectedItems.Count == 0) return;

            var selectedItem = listViewAvatarProfiles.SelectedItems[0];
            // Ensure Tag is Guid
            if (!(selectedItem.Tag is Guid profileId)) return;

            AvatarProfile profileToRemove = currentSettings.GetProfileById(profileId);

            if (profileToRemove == null) { /* Should not happen if list is synced */ return; }

            // Confirmation dialog
            var confirmResult = MessageBox.Show($"Are you sure you want to remove the profile '{profileToRemove.ProfileName}'?",
                                               "Confirm Removal",
                                               MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                currentSettings.Profiles.Remove(profileToRemove);
                PopulateProfileListView(); // Refresh list
                // Selection will be cleared, button states updated via SelectedIndexChanged
            }
        }

        private void BtnSetGlobalHideShowHotkey_Click(object sender, EventArgs e)
        {
            // Toggle hotkey listening mode for the *global* hotkey
            if (_isListeningForGlobalHotkey)
            {
                StopListeningForGlobalHotkey("Global hotkey capture cancelled.");
            }
            else
            {
                _isListeningForGlobalHotkey = true;
                btnSetGlobalHideShowHotkey.Text = "Press Key...";
                this.Text = "Profile Manager - PRESS GLOBAL HOTKEY (Esc to cancel)"; // Temporary title change
                txtGlobalHideShowHotkey.Focus();
            }
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            // Update settings object from controls on this form
            currentSettings.StartMinimized = chkStartMinimized.Checked;
            // Global Hotkey is updated via its own setting mechanism (KeyDown)
            // Profile list changes (Add/Remove) have already modified currentSettings.Profiles
            // Profile edits were saved/updated via the ProfileEditForm interaction

            // Save the entire settings object
            SettingsManager.SaveSettings(currentSettings);

            // Removed the MessageBox confirmation

            // IMPORTANT: The main ControlPanelForm should reload settings when this form closes
            // to pick up any changes made (especially hotkeys). See the FormClosed handler
            // modification in ControlPanelForm where ShowSetupFormInstance is called.

            // Close the form after saving
            this.Close();
        }

        // --- Example Event for Notifying ControlPanelForm ---
        // public event EventHandler SettingsSaved;
        // protected virtual void RaiseSettingsSavedEvent() {
        //      SettingsSaved?.Invoke(this, EventArgs.Empty);
        // }
        // ControlPanelForm would need to subscribe: profileManagerForm.SettingsSaved += HandleSettingsSaved;

        private void ButClose_Click(object sender, EventArgs e)
        {
            // Simply close this form. Assumes explicit save is needed via Save button.
            this.Close();
        }


        // --- Global Hotkey Capture Logic ---

        // --- MODIFIED: Global Hotkey Capture Logic ---
        private void ProfileManagerForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Capture global hotkey if in listening mode
            if (_isListeningForGlobalHotkey)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                if (e.KeyCode == Keys.Escape)
                {
                    StopListeningForGlobalHotkey("Global hotkey capture cancelled.");
                    return;
                }

                // Ignore modifier-only presses
                if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu ||
                    e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin ||
                    e.KeyCode == Keys.Control || e.KeyCode == Keys.Shift || e.KeyCode == Keys.Alt)
                {
                    return; // Wait for non-modifier key
                }

                // Require at least one modifier
                if (!e.Control && !e.Alt && !e.Shift)
                {
                    this.Text = "Profile Manager - INVALID: Use Ctrl, Alt, or Shift"; // Temp feedback
                    // Clear feedback after delay? Or wait for valid combo.
                    return; // Wait for a valid combination
                }

                var capturedHotkey = new HotkeyConfig
                {
                    Key = e.KeyCode,
                    Control = e.Control,
                    Alt = e.Alt,
                    Shift = e.Shift
                };

                // --- Validation Steps ---
                bool registrationSuccess = false;
                bool tempRegistered = false;
                int errorCode = 0;

                // 1. Check internal conflicts first
                if (IsGlobalHotkeyUsed(capturedHotkey))
                {
                    this.Text = "Profile Manager - Hotkey Conflict!"; // Temp feedback
                    StopListeningForGlobalHotkey($"Hotkey '{capturedHotkey}' conflicts with a profile hotkey.");
                    return; // Stop processing
                }

                // 2. Attempt temporary system-wide registration to check external conflicts
                uint modifiers = (uint)NativeMethods.ModifierKeys.None;
                if (capturedHotkey.Alt) modifiers |= (uint)NativeMethods.ModifierKeys.Alt;
                if (capturedHotkey.Control) modifiers |= (uint)NativeMethods.ModifierKeys.Control;
                if (capturedHotkey.Shift) modifiers |= (uint)NativeMethods.ModifierKeys.Shift;
                uint vk = (uint)capturedHotkey.Key;

                try
                {
                    Debug.WriteLine($"Attempting temporary registration for {capturedHotkey} with ID {TEMP_GLOBAL_HOTKEY_ID}");
                    registrationSuccess = NativeMethods.RegisterHotKey(this.Handle, TEMP_GLOBAL_HOTKEY_ID, modifiers, vk);
                    tempRegistered = registrationSuccess; // Mark if registration call succeeded

                    if (!registrationSuccess)
                    {
                        errorCode = Marshal.GetLastWin32Error();
                        Debug.WriteLine($"Temporary registration failed. Error Code: {errorCode}");
                    }
                    else
                    {
                        Debug.WriteLine($"Temporary registration succeeded.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception during temporary hotkey registration: {ex.Message}");
                    // Treat exception as failure
                    registrationSuccess = false;
                    errorCode = -1; // Indicate general exception
                }
                finally
                {
                    // --- CRITICAL: Always unregister the temporary hotkey ---
                    if (tempRegistered) // Only unregister if RegisterHotKey returned true
                    {
                        Debug.WriteLine($"Unregistering temporary hotkey ID {TEMP_GLOBAL_HOTKEY_ID}");
                        NativeMethods.UnregisterHotKey(this.Handle, TEMP_GLOBAL_HOTKEY_ID);
                    }
                }

                // 3. Process results of temporary registration check
                if (!registrationSuccess)
                {
                    if (errorCode == 1409) // ERROR_HOTKEY_ALREADY_REGISTERED
                    {
                        StopListeningForGlobalHotkey($"Hotkey '{capturedHotkey}' is already registered by another application.");
                    }
                    else if (errorCode == -1) // General exception case
                    {
                        StopListeningForGlobalHotkey($"Error trying to check hotkey availability.");
                    }
                    else // Other registration error
                    {
                        StopListeningForGlobalHotkey($"Could not register hotkey. System Error Code: {errorCode}");
                    }
                }
                else
                {
                    // SUCCESS: Hotkey is valid internally and available externally
                    currentSettings.GlobalHideShowHotkey = capturedHotkey;
                    UpdateGlobalHotkeyDisplay();
                    StopListeningForGlobalHotkey("Global hotkey set successfully.");
                }
            } // end if (_isListeningForGlobalHotkey)
        } // end ProfileManagerForm_KeyDown

        private void StopListeningForGlobalHotkey(string finalStatusMessage)
        {
            _isListeningForGlobalHotkey = false;
            btnSetGlobalHideShowHotkey.Text = "Set...";
            this.Text = "Profile Manager"; // Reset title
                                           // TODO: Show finalStatusMessage more permanently (e.g., StatusStrip)
            Debug.WriteLine(finalStatusMessage);
        }


        // --- Validation Helpers ---

        // Checks if a potential global hotkey conflicts with any profile hotkey
        private bool IsGlobalHotkeyUsed(HotkeyConfig hotkeyToCheck)
        {
            if (hotkeyToCheck == null || hotkeyToCheck.Key == Keys.None) return false;
            if (currentSettings?.Profiles == null) return false; // Safety check

            foreach (var profile in currentSettings.Profiles)
            {
                if (profile.Hotkey != null && profile.Hotkey.Key != Keys.None)
                {
                    if (HotkeysMatch(profile.Hotkey, hotkeyToCheck))
                    {
                        Debug.WriteLine($"Global hotkey conflict: '{hotkeyToCheck}' matches profile '{profile.ProfileName}'.");
                        return true;
                    }
                }
            }
            return false;
        }

        // Helper to compare two HotkeyConfig objects (can be shared or duplicated)
        private bool HotkeysMatch(HotkeyConfig hk1, HotkeyConfig hk2)
        {
            if (hk1 == null || hk2 == null) return false;
            return hk1.Key == hk2.Key &&
                   hk1.Control == hk2.Control &&
                   hk1.Alt == hk2.Alt &&
                   hk1.Shift == hk2.Shift;
        }

    } // End of ProfileManagerForm class
} // End of namespace
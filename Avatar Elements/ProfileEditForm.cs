// --- Required Using Statements ---
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO; // For File operations
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avatar_Elements.Data; // Your data classes namespace
using Avatar_Elements.Helpers; // Your helpers namespace (if SettingsManager is there)
using System.Diagnostics; // For Debug.WriteLine

namespace Avatar_Elements {
    public partial class ProfileEditForm : Form {
        // --- Member Variables ---
        private readonly AvatarProfile _profile; // The profile object being edited/created
        private readonly AppSettings _allSettings; // Reference to all settings for validation
        private bool _isListeningForHotkey = false;
        private HotkeyConfig _originalHotkey; // Store original hotkey for validation check against self

        // --- ADDED: For Original State & Live Preview ---
        private string _originalBaseImagePath;
        private string _originalDepthMapPath;
        private float _originalDepthScale;
        private Action<AvatarProfile> _requestPreviewUpdateAction; // Delegate to request preview update
        private float _originalSpecularIntensity; // <<< ADDED FIELD DEFINITION
        private float _originalSpecularPower;

        private bool _loadingData = false;

        // --- Constructor ---
        // --- Constructor ---
        /// <summary>
        /// Creates or Edits an Avatar Profile.
        /// </summary>
        /// <param name="profile">The profile object to edit.</param>
        /// <param name="allSettings">Reference to all application settings for validation.</param>
        /// <param name="requestPreviewUpdateAction">Action to call when a preview update is desired.</param>
        public ProfileEditForm(AvatarProfile profile, AppSettings allSettings, Action<AvatarProfile> requestPreviewUpdateAction)
        {
            InitializeComponent();

            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _allSettings = allSettings ?? throw new ArgumentNullException(nameof(allSettings));
            _requestPreviewUpdateAction = requestPreviewUpdateAction;

            // --- Store Original State ---
            _originalHotkey = _profile.Hotkey?.Clone() ?? new HotkeyConfig(); // Clone or create new if null
            _originalBaseImagePath = _profile.BaseImagePath;
            _originalDepthMapPath = _profile.DepthMapPath;
            _originalDepthScale = _profile.DepthScale;
            _originalSpecularIntensity = _profile.SpecularIntensity; // <<< ADDED
            _originalSpecularPower = _profile.SpecularPower;         // <<< ADDED
                                                                     // --- End Store ---


            this.KeyPreview = true;
            WireUpEvents();
        }

        // --- Form Load ---
        private void ProfileEditForm_Load(object sender, EventArgs e)
        {
            // --- CORRECTED: Use _loadingData flag ---
            _loadingData = true; // SET FLAG before loading
            try
            {
                LoadProfileDataToForm(); // Load data while flag is true

                // Set dynamic window title
                if ((_profile.ProfileName == "New Profile" || string.IsNullOrEmpty(_profile.ProfileName))
                    && string.IsNullOrEmpty(_profile.BaseImagePath)
                    && string.IsNullOrEmpty(_profile.DepthMapPath))
                {
                    this.Text = "Add New Profile";
                    if (string.IsNullOrEmpty(_profile.ProfileName)) _profile.ProfileName = "New Profile";
                    txtProfileName.Text = _profile.ProfileName;
                }
                else
                {
                    this.Text = $"Edit Profile: {_profile.ProfileName}";
                }
            }
            finally
            {
                _loadingData = false; // CLEAR FLAG after loading
            }
            // --- END CORRECTION ---
        }

        // --- Event Wiring ---
        private void WireUpEvents()
        {
            this.Load += ProfileEditForm_Load;
            this.KeyDown += ProfileEditForm_KeyDown;
            this.FormClosing += ProfileEditForm_FormClosing;

            // Button clicks
            btnBrowseBaseImage.Click += BtnBrowseBaseImage_Click;
            btnBrowseDepthMap.Click += BtnBrowseDepthMap_Click;
            btnSetProfileHotkey.Click += BtnSetProfileHotkey_Click;
            btnClearProfileHotkey.Click += BtnClearProfileHotkey_Click;
            btnOK.Click += BtnOK_Click;

            // Handle ValueChanged for live preview controls
            // --- CORRECTED: Use only the combined handler for all numeric inputs ---
            numDepthScale.ValueChanged += NumValue_ValueChanged;
            numSpecularIntensity.ValueChanged += NumValue_ValueChanged;
            numSpecularPower.ValueChanged += NumValue_ValueChanged;
            // --- END CORRECTION ---

            // --- ADDED: Wire up the new button ---
            btnEditAnimations.Click += BtnEditAnimations_Click; // Make sure btnEditAnimations exists
                                                                // --- END ADDED ---
        }


        // --- Data Loading / UI Update ---

        // --- Data Loading / UI Update ---
        private void LoadProfileDataToForm()
        {
            // Load data from the profile object into the UI controls
            txtProfileName.Text = _profile.ProfileName;
            txtBaseImagePath.Text = _profile.BaseImagePath;
            txtDepthMapPath.Text = _profile.DepthMapPath;
            UpdateHotkeyDisplay(); // Update hotkey text

            // Clamp values just in case loaded values are outside control ranges
            decimal clampedScale = (decimal)Math.Max((float)numDepthScale.Minimum, Math.Min((float)numDepthScale.Maximum, _profile.DepthScale));
            numDepthScale.Value = clampedScale;

            // >>>>> ADDED: Load Specular values <<<<<
            decimal clampedIntensity = (decimal)Math.Max((float)numSpecularIntensity.Minimum, Math.Min((float)numSpecularIntensity.Maximum, _profile.SpecularIntensity));
            numSpecularIntensity.Value = clampedIntensity;

            decimal clampedPower = (decimal)Math.Max((float)numSpecularPower.Minimum, Math.Min((float)numSpecularPower.Maximum, _profile.SpecularPower));
            numSpecularPower.Value = clampedPower;
            // >>>>> END ADDED <<<<<

            LoadImagePreview(txtBaseImagePath.Text, picBaseImagePreview); // Load image previews
            LoadImagePreview(txtDepthMapPath.Text, picDepthMapPreview);
            lblValidationStatus.Text = ""; // Clear any previous validation status
        }

        private void UpdateHotkeyDisplay()
        {
            // Update the text box showing the current hotkey assignment
            txtProfileHotkey.Text = _profile.Hotkey?.ToString() ?? "None";
        }

        private void LoadImagePreview(string imagePath, PictureBox pictureBox)
        {
            // Safely load an image preview into a PictureBox, avoiding file locks

            if (pictureBox == null) return;

            // Clear previous image and dispose it to release resources
            pictureBox.Image?.Dispose(); // Use null-conditional operator
            pictureBox.Image = null;

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    // Load into memory stream first to avoid locking the file on disk
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        // Ensure stream position is at the beginning if needed, though FromStream usually handles it
                        // ms.Position = 0;
                        pictureBox.Image = Image.FromStream(ms);
                    }
                }
                catch (Exception ex)
                {
                    // Log error and clear preview if loading fails
                    Debug.WriteLine($"Error loading preview image '{imagePath}': {ex.Message}");
                    pictureBox.Image = null;
                    lblValidationStatus.Text = $"Error loading preview: {Path.GetFileName(imagePath)}";
                }
            }
        }


        // --- Event Handlers ---

        // --- Event Handlers ---
        private void ProfileEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If the user cancelled (clicked Cancel button or closed via 'X')
            if (this.DialogResult == DialogResult.Cancel)
            {
                // Revert changes made to the profile object back to original state
                Debug.WriteLine("ProfileEditForm closing with Cancel. Reverting changes.");
                _profile.BaseImagePath = _originalBaseImagePath;
                _profile.DepthMapPath = _originalDepthMapPath;
                _profile.DepthScale = _originalDepthScale;
                _profile.SpecularIntensity = _originalSpecularIntensity; // <<< ADDED
                _profile.SpecularPower = _originalSpecularPower;         // <<< ADDED
                _profile.Hotkey = _originalHotkey; // Also revert hotkey

                // Trigger one last preview update to show the reverted state if preview is visible
                _requestPreviewUpdateAction?.Invoke(_profile);
            }
            // If DialogResult is OK, BtnOK_Click already updated the object with final values.
        }
        // --- END ADDED ---

        private void BtnBrowseBaseImage_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { /* ... setup ... */ })
            {
                // ... (existing code to set initial directory) ...

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    txtBaseImagePath.Text = ofd.FileName;
                    LoadImagePreview(ofd.FileName, picBaseImagePreview);
                    bool dimsValid = ValidateImageDimensions(); // Re-validate dimensions

                    // --- ADDED: Live Preview Trigger ---
                    if (dimsValid) // Only update if dimensions are okay
                    {
                        _profile.BaseImagePath = ofd.FileName; // Update profile object immediately
                        _requestPreviewUpdateAction?.Invoke(_profile); // Request preview update
                    }
                    // --- END ADDED ---
                }
            }
        }

        private void BtnBrowseDepthMap_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { /* ... setup ... */ })
            {
                // ... (existing code to set initial directory) ...

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    txtDepthMapPath.Text = ofd.FileName;
                    LoadImagePreview(ofd.FileName, picDepthMapPreview);
                    bool dimsValid = ValidateImageDimensions(); // Re-validate dimensions

                    // --- ADDED: Live Preview Trigger ---
                    if (dimsValid) // Only update if dimensions are okay
                    {
                        _profile.DepthMapPath = ofd.FileName; // Update profile object immediately
                        _requestPreviewUpdateAction?.Invoke(_profile); // Request preview update
                    }
                    // --- END ADDED ---
                }
            }
        }

        // --- ADDED: Handle Depth Scale change for live preview ---
        private void NumDepthScale_ValueChanged(object sender, EventArgs e)
        {
            // Don't trigger during initial load
            // Note: LoadProfileDataToForm doesn't have a _loadingData flag, might need one
            // if programmatic setting triggers this undesirably. For now, assume it's user change.

            // Update profile object immediately
            _profile.DepthScale = (float)numDepthScale.Value;
            // Request preview update
            _requestPreviewUpdateAction?.Invoke(_profile);
        }
        // --- END ADDED ---

        // --- CORRECTED: Combined handler for numeric value changes (Checks Flag) ---
        // This function correctly handles changes for DepthScale, SpecularIntensity, and SpecularPower
        private void NumValue_ValueChanged(object sender, EventArgs e)
        {
            if (_loadingData) return; // CHECK FLAG: Exit if currently loading

            // Update profile object immediately from *all* relevant controls
            _profile.DepthScale = (float)numDepthScale.Value;
            _profile.SpecularIntensity = (float)numSpecularIntensity.Value;
            _profile.SpecularPower = (float)numSpecularPower.Value;

            // Request preview update
            _requestPreviewUpdateAction?.Invoke(_profile);
        }
        // --- END CORRECTION ---

        // --- MODIFIED: Event Handler for Edit Animations Button ---
        private void BtnEditAnimations_Click(object sender, EventArgs e)
        {
            // Check if profile and settings are available
            if (_profile == null || _allSettings == null)
            {
                MessageBox.Show("Profile or application settings are not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create and show the Animation Editor form modally
            // --- FIXED: Pass both _profile and _allSettings ---
            Debug.WriteLine($"Opening Animation Editor for profile '{_profile.ProfileName}'...");
            using (var animEditor = new AnimationEditorForm(_profile, _allSettings))
            // --- END FIX ---
            {
                // Optional: Subscribe to an event if the editor needs to notify this form
                // animEditor.AnimationsChanged += HandleAnimationsChanged; // Define this handler if needed

                DialogResult result = animEditor.ShowDialog(this); // Show as modal dialog

                // Optional: Unsubscribe
                // animEditor.AnimationsChanged -= HandleAnimationsChanged;

                // When the editor closes, changes are made directly to the _profile object.
                // The caller (e.g., ProfileManagerForm or ControlPanelForm) should handle saving AppSettings.
                // However, we might mark the main settings as dirty if the editor indicated changes were made.
                if (result == DialogResult.OK)
                {
                    Debug.WriteLine("Animation Editor closed with OK (Changes may have been saved within editor).");
                    // Optional: Mark the main settings as needing a save if editor signals success?
                    // SettingsManager.SaveSettings(_allSettings); // Maybe don't save here, let Profile Manager handle it.
                    // Potentially trigger unlit frame regeneration from ControlPanelForm after this returns?
                }
                else
                {
                    Debug.WriteLine("Animation Editor closed via Cancel or X.");
                }

                // Update the main Profile Edit form if needed (e.g., if animation count changed - though no UI shows this here)
                // No immediate action needed here unless editor interaction needs to update this specific form's state.
            }
        }
        // --- END MODIFIED ---

        private void BtnSetProfileHotkey_Click(object sender, EventArgs e)
        {
            // Toggle hotkey listening mode
            if (_isListeningForHotkey)
            {
                // Cancel listening if button clicked again
                StopListeningForHotkey("Hotkey capture cancelled.");
            }
            else
            {
                // Start listening
                _isListeningForHotkey = true;
                btnSetProfileHotkey.Text = "Press Key..."; // Update button text
                lblValidationStatus.Text = "Press the desired hotkey combination (e.g., Ctrl+Shift+K). Press Esc to cancel.";
                // Give feedback and potentially focus a control
                txtProfileHotkey.Focus(); // Focus the textbox, although form KeyPreview handles capture
            }
        }

        private void BtnClearProfileHotkey_Click(object sender, EventArgs e)
        {
            // Clear the hotkey assignment for the current profile
            if (_isListeningForHotkey) StopListeningForHotkey("Hotkey cleared."); // Cancel listen mode if active

            // Assign a new, empty HotkeyConfig
            _profile.Hotkey = new HotkeyConfig(); // Key = Keys.None by default
            UpdateHotkeyDisplay(); // Update the text box
            lblValidationStatus.Text = "Hotkey cleared.";
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate all inputs before saving and closing
            if (ValidateInput())
            {
                // Validation passed, update the profile object from form controls
                _profile.ProfileName = txtProfileName.Text.Trim();
                _profile.BaseImagePath = txtBaseImagePath.Text;
                _profile.DepthMapPath = txtDepthMapPath.Text;
                _profile.DepthScale = (float)numDepthScale.Value;
                _profile.SpecularIntensity = (float)numSpecularIntensity.Value; // <<< ADDED
                _profile.SpecularPower = (float)numSpecularPower.Value;         // <<< ADDED

                // Hotkey is already updated in the _profile object via its setting mechanism

                // Signal success and close the dialog
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            // Else: validation failed, error message shown in lblValidationStatus, form remains open
        }

        // Cancel button click is handled by Form.CancelButton property which sets DialogResult to Cancel


        // --- Hotkey Capture Logic ---

        private void ProfileEditForm_KeyDown(object sender, KeyEventArgs e)
        {
            // This event fires when a key is pressed down while the form has focus (requires KeyPreview = true)
            if (_isListeningForHotkey)
            {
                // Prevent the key press from triggering other actions or being typed
                e.Handled = true;
                e.SuppressKeyPress = true;

                // Handle cancellation via Escape key
                if (e.KeyCode == Keys.Escape)
                {
                    StopListeningForHotkey("Hotkey capture cancelled.");
                    return;
                }

                // Ignore key presses that are only modifiers (Ctrl, Alt, Shift, Win)
                if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                    e.KeyCode == Keys.Menu /*Alt*/ || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin ||
                    e.KeyCode == Keys.Control || e.KeyCode == Keys.Shift || e.KeyCode == Keys.Alt) // Add Keys.Alt for completeness
                {
                    return; // Wait for a non-modifier key
                }

                // Check if at least one standard modifier (Ctrl, Alt, Shift) is pressed
                // We typically don't use the Win key for app hotkeys registered this way.
                if (!e.Control && !e.Alt && !e.Shift)
                {
                    lblValidationStatus.Text = "Invalid Hotkey: Must include Ctrl, Alt, or Shift.";
                    // Optional: Stop listening or just wait for a valid combination
                    // StopListeningForHotkey("Invalid combination.");
                    return; // Wait for a valid combination
                }

                // Construct the captured hotkey configuration
                var capturedHotkey = new HotkeyConfig
                {
                    Key = e.KeyCode,
                    Control = e.Control,
                    Alt = e.Alt,
                    Shift = e.Shift
                };

                // Validate if this hotkey is already in use by global or other profiles
                // Pass the current profile's ID so it doesn't conflict with its original value
                if (IsHotkeyUsed(capturedHotkey, _profile.Id))
                {
                    lblValidationStatus.Text = $"Hotkey '{capturedHotkey}' is already in use.";
                    StopListeningForHotkey("Hotkey conflict detected."); // Stop listening on conflict
                }
                else
                {
                    // Assign the valid, unique hotkey to the profile object
                    _profile.Hotkey = capturedHotkey;
                    UpdateHotkeyDisplay(); // Update the UI text
                    StopListeningForHotkey("Hotkey set successfully."); // Stop listening
                }
            }
        }

        // Helper method to reset the hotkey listening state
        private void StopListeningForHotkey(string finalStatusMessage)
        {
            _isListeningForHotkey = false;
            btnSetProfileHotkey.Text = "Set..."; // Reset button text
            lblValidationStatus.Text = finalStatusMessage; // Show final status
        }

        // --- Validation Methods ---

        private bool ValidateInput()
        {
            // Master validation method called when OK is clicked

            // 1. Validate Profile Name
            if (string.IsNullOrWhiteSpace(txtProfileName.Text))
            {
                lblValidationStatus.Text = "Profile Name cannot be empty.";
                txtProfileName.Focus();
                return false;
            }

            // 2. Validate File Paths (Existence)
            if (string.IsNullOrWhiteSpace(txtBaseImagePath.Text) || !File.Exists(txtBaseImagePath.Text))
            {
                lblValidationStatus.Text = "Base Avatar Image file not found or not selected.";
                // Optionally focus the browse button or text box
                // btnBrowseBaseImage.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtDepthMapPath.Text) || !File.Exists(txtDepthMapPath.Text))
            {
                lblValidationStatus.Text = "Depth Map Image file not found or not selected.";
                // btnBrowseDepthMap.Focus();
                return false;
            }

            // 3. Validate Image Files (Loadable and Dimensions Match)
            if (!ValidateImageDimensions()) // This method sets lblValidationStatus on failure
            {
                return false;
            }

            // 4. Validate Depth Scale (already handled by NumericUpDown range)

            // 5. Hotkey Validation (Uniqueness check happens during setting, maybe re-check?)
            // It's generally safe to assume the _profile.Hotkey is valid if it was set via the UI,
            // unless external changes happened. Re-validating here could be added if needed.
            // if (IsHotkeyUsed(_profile.Hotkey, _profile.Id)) { ... return false ... }


            lblValidationStatus.Text = ""; // Clear status if all validations pass
            return true;
        }

        private bool ValidateImageDimensions()
        {
            // Checks if both images can be loaded and if their dimensions match
            string basePath = txtBaseImagePath.Text;
            string depthPath = txtDepthMapPath.Text;

            // Skip check if paths are invalid (caught by ValidateInput)
            if (string.IsNullOrWhiteSpace(basePath) || !File.Exists(basePath) ||
                string.IsNullOrWhiteSpace(depthPath) || !File.Exists(depthPath))
            {
                return true; // Let ValidateInput handle missing files
            }

            Size baseSize = Size.Empty;
            Size depthSize = Size.Empty;
            bool success = true;

            try
            {
                // Load images safely to check dimensions without locking files long-term
                using (var baseImg = Image.FromFile(basePath)) { baseSize = baseImg.Size; }
                using (var depthImg = Image.FromFile(depthPath)) { depthSize = depthImg.Size; }

                // Check if dimensions match
                if (baseSize.IsEmpty || depthSize.IsEmpty)
                {
                    lblValidationStatus.Text = "Could not determine image sizes.";
                    success = false;
                }
                else if (baseSize != depthSize)
                {
                    lblValidationStatus.Text = $"Dimension mismatch: Avatar is {baseSize.Width}x{baseSize.Height}, Depth map is {depthSize.Width}x{depthSize.Height}.";
                    success = false;
                }
                else
                {
                    // Dimensions match, clear status related to dimensions
                    // Avoid clearing other potential errors, maybe check existing text first
                    if (lblValidationStatus.Text.StartsWith("Dimension mismatch")) lblValidationStatus.Text = "";
                }
            }
            catch (OutOfMemoryException memEx) // Specifically catch invalid image format errors
            {
                Debug.WriteLine($"Image loading error: {memEx.Message}");
                lblValidationStatus.Text = "Invalid image format or file corrupted.";
                success = false;
            }
            catch (Exception ex) // Catch other potential file access/loading errors
            {
                Debug.WriteLine($"Image validation error: {ex.Message}");
                lblValidationStatus.Text = $"Error accessing image files: {ex.Message}";
                success = false;
            }

            return success;
        }


        // Checks if a given hotkey conflicts with global or other profiles
        private bool IsHotkeyUsed(HotkeyConfig hotkeyToCheck, Guid currentProfileId)
        {
            if (hotkeyToCheck == null || hotkeyToCheck.Key == Keys.None)
            {
                return false; // 'None' / unassigned hotkey cannot conflict
            }

            // Check against Global Hotkey first
            if (_allSettings.GlobalHideShowHotkey != null && _allSettings.GlobalHideShowHotkey.Key != Keys.None)
            {
                if (HotkeysMatch(_allSettings.GlobalHideShowHotkey, hotkeyToCheck))
                {
                    Debug.WriteLine($"Hotkey conflict: '{hotkeyToCheck}' matches Global Hotkey.");
                    return true; // Conflicts with global
                }
            }

            // Check against ALL profiles (including the one being edited, IF the hotkey differs from its original)
            foreach (var profile in _allSettings.Profiles)
            {
                // Skip the current profile IF the hotkey being checked is the same as its original one
                if (profile.Id == currentProfileId && HotkeysMatch(_originalHotkey, hotkeyToCheck))
                {
                    continue; // Allow profile to keep its own original hotkey
                }

                // Check against other profiles OR the current profile if the hotkey is NEW
                if (profile.Hotkey != null && profile.Hotkey.Key != Keys.None)
                {
                    if (HotkeysMatch(profile.Hotkey, hotkeyToCheck))
                    {
                        Debug.WriteLine($"Hotkey conflict: '{hotkeyToCheck}' matches profile '{profile.ProfileName}'.");
                        return true; // Conflicts with another profile (or self if new hotkey is assigned)
                    }
                }
            }

            return false; // No conflicts found
        }

        // Helper to compare two HotkeyConfig objects for equality
        private bool HotkeysMatch(HotkeyConfig hk1, HotkeyConfig hk2)
        {
            if (hk1 == null || hk2 == null) return false; // Cannot match if one is null
                                                          // Compare Key and all modifier states
            return hk1.Key == hk2.Key &&
                   hk1.Control == hk2.Control &&
                   hk1.Alt == hk2.Alt &&
                   hk1.Shift == hk2.Shift;
            // Note: Doesn't compare Win key, as it's typically not handled well by RegisterHotKey P/Invoke
        }

    } // End of ProfileEditForm class
} // End of namespace
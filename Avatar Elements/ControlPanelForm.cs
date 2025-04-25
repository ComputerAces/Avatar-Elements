using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Avatar_Elements.Data;
using Avatar_Elements.Helpers;
using System.IO;
using System.Security.Cryptography;
using System.Drawing.Imaging;
using System.Threading;

namespace Avatar_Elements {
    public partial class ControlPanelForm : Form {
        #region Member Variables

        private AppSettings _currentSettings;
        private const int GLOBAL_HOTKEY_ID = 0x1;
        private bool _isGlobalHotkeyRegistered = false;
        private AvatarProfile _activeProfile = null;

        private PreviewForm previewForm;
        private ProfileManagerForm profileManagerForm;
        private bool areEffectsEnabled = true;

        private HotkeyHandlerWindow _hotkeyWindow;
        private const int ANIMATION_BOUNDS_PADDING = 20;

        private CancellationTokenSource _frameGenerationCts = null;
        private bool _isGeneratingFrames = false;
        private Guid? _currentlyPlayingTimelineId = null;

        private Size _activeProfileMaxSize = Size.Empty;
        private Task<bool> _preCacheTask = Task.FromResult(false);

        // --- ADDED: State for looping animation ---
        private Guid? _currentlyLoopingTimelineId = null;
        // --- END ADDED ---

        #endregion

        #region Hotkey Handling Specifics
        private Dictionary<int, Guid?> _hotkeyIdToProfileMap = new Dictionary<int, Guid?>();
        private Dictionary<int, Guid> _hotkeyIdToAnimationMap = new Dictionary<int, Guid>();
        private const int GLOBAL_HIDE_SHOW_ID = 1;
        private int _nextProfileHotkeyId = 1000;
        private int _nextAnimationHotkeyId = 2000;

        private DateTime _lastHotkeyActivation = DateTime.MinValue;
        private const int HOTKEY_DEBOUNCE_MS = 300;
        #endregion

        #region Animation Cache Management

        private Dictionary<Guid, List<AnimationRenderer.FramePixelData>> _unlitGeometryCache = new Dictionary<Guid, List<AnimationRenderer.FramePixelData>>();
        private Dictionary<Guid, List<Bitmap>> _litFrameCache = new Dictionary<Guid, List<Bitmap>>();

        private Guid? _litCacheProfileId = null;
        private string _litCacheLightingSignature = null;

        private Bitmap _activeProfileBaseBitmap = null;
        private Bitmap _activeProfileDepthBitmap = null;

        #endregion

        public ControlPanelForm()
        {
            InitializeComponent();
            _hotkeyWindow = new HotkeyHandlerWindow();
            WireUpEvents();
        }

        private void ControlPanelForm_Load(object sender, EventArgs e)
        {
            if (notifyIcon1 != null)
            {
                notifyIcon1.Visible = true;
            }
            LoadAndApplySettings();
            UpdateUIState();

            if (_currentSettings != null && _currentSettings.StartMinimized)
            {
                this.BeginInvoke(new MethodInvoker(() => {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.WindowState = FormWindowState.Minimized;
                        this.Hide();
                        this.ShowInTaskbar = false;
                    }
                }));
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _hotkeyWindow?.Dispose();
                    _hotkeyWindow = null;

                    if (_frameGenerationCts != null && !_frameGenerationCts.IsCancellationRequested)
                    {
                        try { _frameGenerationCts.Cancel(); Task.Delay(50).Wait(); }
                        catch (ObjectDisposedException) { }
                        catch (AggregateException) { }
                        catch (Exception) { } // General catch
                    }
                    _frameGenerationCts?.Dispose();
                    _frameGenerationCts = null;

                    ClearAllAnimationCaches();
                    _activeProfileBaseBitmap?.Dispose(); _activeProfileBaseBitmap = null;
                    _activeProfileDepthBitmap?.Dispose(); _activeProfileDepthBitmap = null;

                    // Stop playback before closing forms
                    previewForm?.StopPlayback(false); // Stop any playback
                    _currentlyLoopingTimelineId = null; // Clear looping state

                    if (notifyIcon1 != null) { try { notifyIcon1.Dispose(); } catch { } }
                    if (components != null) { components.Dispose(); }

                    try { previewForm?.Close(); } catch { } // Close before disposing
                    try { profileManagerForm?.Close(); } catch { }
                    previewForm?.Dispose(); // Dispose after closing
                    profileManagerForm?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during ControlPanelForm Dispose: {ex.Message}");
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void ControlPanelForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
            }
        }

        private void ShowHideControlPanelMenuItem_Click(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Hide();
                this.ShowInTaskbar = false;
            }
            else
            {
                ShowControlPanel();
            }
            UpdateUIState();
        }

        private void EnableDisableEffectsMenuItem_Click(object sender, EventArgs e)
        {
            areEffectsEnabled = !areEffectsEnabled;
            UpdateUIState();
        }

        private void ExitApplicationMenuItem_Click(object sender, EventArgs e)
        {
            _hotkeyWindow?.Dispose();
            _hotkeyWindow = null;
            if (notifyIcon1 != null) { notifyIcon1.Visible = false; notifyIcon1.Dispose(); }
            previewForm?.Close();
            profileManagerForm?.Close();
            Application.Exit();
        }

        private void BtnShowPreview_Click(object sender, EventArgs e)
        {
            ShowHidePreviewMenuItem_Click(sender, e);
        }

        private void BtnShowSetup_Click(object sender, EventArgs e)
        {
            ShowSetupFormInstance();
        }

        private void BtnEditCurrentProfile_Click(object sender, EventArgs e)
        {
            if (_activeProfile == null || _currentSettings == null) { MessageBox.Show("No active profile selected.", "Cannot Edit", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using (var editForm = new ProfileEditForm(_activeProfile, _currentSettings, HandleProfileEditPreviewRequest))
            {
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    SettingsManager.SaveSettings(_currentSettings);
                    InvalidateUnlitGeometryCacheForProfile(_activeProfile);
                    InvalidateLitFrameCache();
                    if (!LoadActiveProfileImages(_activeProfile))
                    {
                        MessageBox.Show($"Failed reload images for '{_activeProfile.ProfileName}'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    RecalculateAndApplyMaxSize();
                    PopulateProfileComboBox();
                    UpdateAnimationComboBox();
                    TriggerPreviewRefresh();
                    UpdateUIState();
                }
                else
                {
                    TriggerPreviewRefresh();
                    UpdateUIState();
                }
            }
        }

        private void BtnEditLights_Click(object sender, EventArgs e)
        {
            if (_currentSettings == null) { MessageBox.Show("Settings not loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            using (var lightEditorForm = new LightEditorForm(_currentSettings, _activeProfile))
            {
                lightEditorForm.SettingsChanged += LightEditorForm_SettingsChanged;
                lightEditorForm.ShowDialog(this);
                lightEditorForm.SettingsChanged -= LightEditorForm_SettingsChanged;
                TriggerPreviewRefresh();
            }
        }

        private void CmbSelectAnimation_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUIState();
        }

        // MODIFIED: BtnPlayAnimation_Click
        private async void BtnPlayAnimation_Click(object sender, EventArgs e)
        {
            // --- ADDED: Prevent interaction while generating ---
            if (_isGeneratingFrames)
            {
                MessageBox.Show("Please wait for frame generation to complete.", "Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // --- END ADDED ---

            if (!(cmbSelectAnimation.SelectedItem is AnimationTimeline selectedTimeline)) { MessageBox.Show("Please select an animation.", "No Selection"); return; }
            if (previewForm == null || previewForm.IsDisposed) { MessageBox.Show("Preview window is not open.", "Error"); return; }
            // Removed redundant _isGeneratingFrames check
            if (_activeProfileBaseBitmap == null) { MessageBox.Show("Active profile image not loaded.", "Error"); return; }

            // --- Logic for Loop Start/Stop ---
            if (_currentlyLoopingTimelineId.HasValue && _currentlyLoopingTimelineId.Value == selectedTimeline.Id)
            {
                // Stop the currently looping animation because the button for it was clicked again
                previewForm.StopPlayback(true); // true to reset to static
                _currentlyLoopingTimelineId = null;
                Debug.WriteLine($"Stopped looping animation '{selectedTimeline.Name}' via Play button.");
                UpdateUIState(); // Update button text/state if needed
            }
            else
            {
                // Start a new animation (interrupting any current loop if necessary)
                bool ready = await EnsureLitFramesGenerated(selectedTimeline.Id);
                if (ready)
                {
                    if (_litFrameCache.TryGetValue(selectedTimeline.Id, out var framesToPlay) && framesToPlay?.Count > 0)
                    {
                        // StartPlayback internally stops previous animation (StopPlayback(false))
                        previewForm.StartPlayback(framesToPlay, selectedTimeline.Loop);
                        // Update looping state AFTER starting playback
                        if (selectedTimeline.Loop)
                        {
                            _currentlyLoopingTimelineId = selectedTimeline.Id;
                            Debug.WriteLine($"Started looping animation '{selectedTimeline.Name}' via Play button.");
                        }
                        else
                        {
                            _currentlyLoopingTimelineId = null; // Ensure it's clear if not looping
                            Debug.WriteLine($"Started non-looping animation '{selectedTimeline.Name}' via Play button.");
                        }
                        UpdateUIState();
                    }
                    else
                    {
                        MessageBox.Show("Failed to retrieve frames for playback.", "Playback Error");
                        _currentlyLoopingTimelineId = null; // Clear state on error
                    }
                }
                else
                {
                    // EnsureLitFramesGenerated likely showed an error or was cancelled
                    _currentlyLoopingTimelineId = null; // Clear state on failure
                }
            }
            // --- End Loop Logic ---
        }

        private void BtnCancelGeneration_Click(object sender, EventArgs e)
        {
            if (_frameGenerationCts != null && !_frameGenerationCts.IsCancellationRequested)
            {
                try
                {
                    _frameGenerationCts.Cancel();
                    btnCancelGeneration.Enabled = false;
                    toolStripStatusLabel.Text = "Cancelling generation...";
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception)
                {
                }
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowControlPanel();
        }

        private void ControlPanelForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
        }

        private void LoadAndApplySettings()
        {
            _currentSettings = SettingsManager.LoadSettings() ?? new AppSettings();
            ClearAllAnimationCaches();

            AvatarProfile initialProfile = null;
            if (_currentSettings.LastActiveProfileId.HasValue)
            {
                initialProfile = _currentSettings.GetProfileById(_currentSettings.LastActiveProfileId.Value);
                if (initialProfile != null && !IsProfileValid(initialProfile))
                {
                    initialProfile = null;
                }
            }
            if (initialProfile == null)
            {
                initialProfile = _currentSettings.Profiles?.FirstOrDefault(IsProfileValid);
            }

            bool imagesLoaded = LoadActiveProfileImages(initialProfile);
            if (!imagesLoaded && initialProfile != null)
            {
                MessageBox.Show($"Failed to load images for the initial profile '{initialProfile.ProfileName}'. Please check file paths in setup.", "Image Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                initialProfile = null;
            }
            _activeProfile = initialProfile;

            _activeProfileMaxSize = Size.Empty;
            if (_activeProfile != null && _activeProfileBaseBitmap != null)
            {
                try
                {
                    RectangleF maxBounds = CalculateMaxProfileAnimationBounds(_activeProfile, _activeProfileBaseBitmap.Size);
                    _activeProfileMaxSize = new Size(
                        (int)Math.Ceiling(maxBounds.Width + ANIMATION_BOUNDS_PADDING * 2),
                        (int)Math.Ceiling(maxBounds.Height + ANIMATION_BOUNDS_PADDING * 2)
                    );
                    _activeProfileMaxSize.Width = Math.Max(_activeProfileMaxSize.Width, _activeProfileBaseBitmap.Width);
                    _activeProfileMaxSize.Height = Math.Max(_activeProfileMaxSize.Height, _activeProfileBaseBitmap.Height);
                }
                catch (Exception)
                {
                    _activeProfileMaxSize = _activeProfileBaseBitmap?.Size ?? Size.Empty;
                }
            }

            PopulateProfileComboBox();
            UpdateAnimationComboBox();

            _hotkeyIdToProfileMap.Clear();
            _hotkeyIdToAnimationMap.Clear();
            RegisterAllHotkeys();
            RegisterActiveProfileAnimationHotkeys();

            UpdatePreviewFormWithActiveProfile();
            UpdateUIState();

            _ = StartBackgroundCacheGeneration(_activeProfile); // <<< Corrected Call
        }

        private void PopulateProfileComboBox()
        {
            Guid? previouslySelectedId = null;
            if (cmbActiveProfile.SelectedItem is AvatarProfile currentSelection)
            { previouslySelectedId = currentSelection.Id; }
            else if (_activeProfile != null)
            { previouslySelectedId = _activeProfile.Id; }

            cmbActiveProfile.DataSource = null;
            var validProfiles = _currentSettings?.Profiles?.Where(IsProfileValid).ToList() ?? new List<AvatarProfile>();

            if (validProfiles.Any())
            {
                cmbActiveProfile.DisplayMember = nameof(AvatarProfile.ProfileName);
                cmbActiveProfile.ValueMember = nameof(AvatarProfile.Id);
                cmbActiveProfile.DataSource = validProfiles;

                if (previouslySelectedId.HasValue)
                {
                    cmbActiveProfile.SelectedValue = previouslySelectedId.Value;
                    if (cmbActiveProfile.SelectedValue == null || !cmbActiveProfile.SelectedValue.Equals(previouslySelectedId.Value))
                    { if (cmbActiveProfile.Items.Count > 0) cmbActiveProfile.SelectedIndex = 0; }
                }
                else if (cmbActiveProfile.Items.Count > 0)
                { cmbActiveProfile.SelectedIndex = 0; }

                _activeProfile = cmbActiveProfile.SelectedItem as AvatarProfile;
            }
            else
            {
                cmbActiveProfile.DataSource = null;
                cmbActiveProfile.Items.Clear();
                cmbActiveProfile.Items.Add("(No valid profiles found)");
                cmbActiveProfile.SelectedIndex = 0;
                cmbActiveProfile.Enabled = false;
                _activeProfile = null;
            }
            cmbActiveProfile.Enabled = validProfiles.Any();
        }

        private void CmbActiveProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbActiveProfile.SelectedItem is AvatarProfile selectedProfile)
            {
                if (_activeProfile == null || _activeProfile.Id != selectedProfile.Id)
                {
                    SwitchToProfile(selectedProfile.Id);
                }
            }
        }

        private async void SwitchToProfile(Guid profileId)
        {
            AvatarProfile newProfile = _currentSettings?.GetProfileById(profileId);

            if (newProfile == null || !IsProfileValid(newProfile)) { PopulateProfileComboBox(); return; }
            if (_activeProfile != null && _activeProfile.Id == newProfile.Id) { return; } // Already active

            previewForm?.StopPlayback(false); // Stop animation without reset
            _currentlyLoopingTimelineId = null; // <<< ADDED: Clear looping state
            UnregisterActiveProfileAnimationHotkeys();

            // Cancel any ongoing generation for the old profile
            if (_isGeneratingFrames || (_preCacheTask != null && !_preCacheTask.IsCompleted))
            {
                var previousCts = _frameGenerationCts;
                if (previousCts != null && !previousCts.IsCancellationRequested) { try { previousCts.Cancel(); } catch { } }
                try { await Task.WhenAny(_preCacheTask ?? Task.CompletedTask, Task.Delay(500)); } catch { } // Wait briefly for task/cancellation
                try { previousCts?.Dispose(); } catch { } // Dispose old CTS
                // Update status on UI thread
                this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus("Ready", false); });
                await Task.Delay(50); // Allow UI update
                _isGeneratingFrames = false; // Reset generation flag
                _frameGenerationCts = null; // Clear CTS reference
            }
            ClearAllAnimationCaches(); // Clear caches for old profile

            // Load images for the new profile
            if (!LoadActiveProfileImages(newProfile))
            {
                MessageBox.Show($"Failed to load images for '{newProfile.ProfileName}'. Cannot switch profile.", "Error");
                // Attempt to revert UI to a valid state (e.g., select the first valid profile)
                PopulateProfileComboBox(); // This will try to reselect the first valid one
                return; // Exit switch attempt
            }

            _activeProfile = newProfile; // Set the new profile as active

            // Recalculate max size and apply to preview form if visible
            RecalculateAndApplyMaxSize();

            // Save the last active profile ID
            _currentSettings.LastActiveProfileId = _activeProfile.Id;
            SettingsManager.SaveSettings(_currentSettings);

            // Update preview form content
            if (previewForm != null && !previewForm.IsDisposed)
            {
                // Preview form size is already handled by RecalculateAndApplyMaxSize
                UpdatePreviewFormWithActiveProfile(); // Loads images and refreshes effects
            }

            // Register hotkeys for the new profile's animations
            RegisterActiveProfileAnimationHotkeys();

            // Update UI controls
            PopulateProfileComboBox(); // Re-populate to ensure correct selection highlight
            UpdateAnimationComboBox();
            UpdateUIState();

            // Start background caching for the new profile
            _ = StartBackgroundCacheGeneration(_activeProfile);
        }

        private bool LoadActiveProfileImages(AvatarProfile profile)
        {
            _activeProfileBaseBitmap?.Dispose(); _activeProfileBaseBitmap = null;
            _activeProfileDepthBitmap?.Dispose(); _activeProfileDepthBitmap = null;
            if (profile == null || !IsProfileValid(profile)) return false;
            bool success = true;
            try
            {
                byte[] baseBytes = File.ReadAllBytes(profile.BaseImagePath);
                using (var ms = new MemoryStream(baseBytes)) { _activeProfileBaseBitmap = new Bitmap(ms); }
                if (_activeProfileBaseBitmap.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    Bitmap converted = new Bitmap(_activeProfileBaseBitmap.Width, _activeProfileBaseBitmap.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(converted)) { g.DrawImage(_activeProfileBaseBitmap, 0, 0); }
                    _activeProfileBaseBitmap.Dispose(); _activeProfileBaseBitmap = converted;
                }
            }
            catch (Exception) { _activeProfileBaseBitmap?.Dispose(); _activeProfileBaseBitmap = null; success = false; }
            if (success)
            {
                try
                {
                    byte[] depthBytes = File.ReadAllBytes(profile.DepthMapPath);
                    using (var ms = new MemoryStream(depthBytes)) { _activeProfileDepthBitmap = new Bitmap(ms); }
                }
                catch (Exception) { _activeProfileDepthBitmap?.Dispose(); _activeProfileDepthBitmap = null; success = false; }
            }
            if (!success) { _activeProfileBaseBitmap?.Dispose(); _activeProfileBaseBitmap = null; _activeProfileDepthBitmap?.Dispose(); _activeProfileDepthBitmap = null; }
            return success;
        }

        private void RegisterAllHotkeys()
        {
            if (_hotkeyWindow == null) return;
            if (_currentSettings.GlobalHideShowHotkey != null && _currentSettings.GlobalHideShowHotkey.Key != Keys.None)
            {
                if (_hotkeyWindow.RegisterHotkey(GLOBAL_HIDE_SHOW_ID, _currentSettings.GlobalHideShowHotkey))
                { _hotkeyIdToProfileMap[GLOBAL_HIDE_SHOW_ID] = null; }
            }
            if (_currentSettings.Profiles != null)
            {
                _nextProfileHotkeyId = 1000;
                foreach (var profile in _currentSettings.Profiles)
                {
                    if (profile.Hotkey != null && profile.Hotkey.Key != Keys.None)
                    {
                        int profileHotkeyId = _nextProfileHotkeyId++;
                        if (_hotkeyWindow.RegisterHotkey(profileHotkeyId, profile.Hotkey))
                        { _hotkeyIdToProfileMap[profileHotkeyId] = profile.Id; }
                    }
                }
            }
        }

        private void RegisterActiveProfileAnimationHotkeys()
        {
            if (_hotkeyWindow == null || _activeProfile?.Animations == null) return;
            _hotkeyIdToAnimationMap.Clear();
            _nextAnimationHotkeyId = 2000;
            foreach (var animation in _activeProfile.Animations)
            {
                if (animation.Hotkey != null && animation.Hotkey.Key != Keys.None)
                {
                    bool conflict = false;
                    if (_currentSettings.GlobalHideShowHotkey != null && HotkeysMatch(_currentSettings.GlobalHideShowHotkey, animation.Hotkey)) conflict = true;
                    if (conflict) continue;
                    int animationHotkeyId = _nextAnimationHotkeyId++;
                    if (_hotkeyWindow.RegisterHotkey(animationHotkeyId, animation.Hotkey))
                    { _hotkeyIdToAnimationMap.Add(animationHotkeyId, animation.Id); }
                }
            }
        }

        private void UnregisterActiveProfileAnimationHotkeys()
        {
            if (_hotkeyWindow == null || _hotkeyIdToAnimationMap == null || _hotkeyIdToAnimationMap.Count == 0) return;
            List<int> idsToUnregister = _hotkeyIdToAnimationMap.Keys.ToList();
            foreach (int id in idsToUnregister) { _hotkeyWindow.UnregisterHotkey(id); }
            _hotkeyIdToAnimationMap.Clear();
        }

        private void HandleHotkeyActivated(int hotkeyId)
        {
            TimeSpan elapsed = DateTime.Now - _lastHotkeyActivation;
            if (elapsed.TotalMilliseconds < HOTKEY_DEBOUNCE_MS) return;
            _lastHotkeyActivation = DateTime.Now;
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => HandleHotkeyActivationLogic(hotkeyId))); }
            else { HandleHotkeyActivationLogic(hotkeyId); }
        }

        private async void HandleHotkeyActivationLogic(int hotkeyId)
        {
            if (_hotkeyIdToProfileMap == null || _hotkeyIdToAnimationMap == null) return;

            // --- ADDED: Prevent interaction while generating ---
            if (_isGeneratingFrames)
            {
                Debug.WriteLine($"Hotkey {hotkeyId} ignored: Frame generation in progress.");
                System.Media.SystemSounds.Beep.Play(); // Give feedback that it's busy
                return;
            }
            // --- END ADDED ---

            if (hotkeyId == GLOBAL_HIDE_SHOW_ID)
            {
                ShowHidePreviewMenuItem_Click(this, EventArgs.Empty);
            }
            else if (_hotkeyIdToProfileMap.TryGetValue(hotkeyId, out Guid? profileGuid) && profileGuid.HasValue)
            {
                SwitchToProfile(profileGuid.Value); // Switch profile if it's a profile hotkey
            }
            else if (_hotkeyIdToAnimationMap.TryGetValue(hotkeyId, out Guid animationGuid))
            {
                // --- Logic for Animation Hotkeys ---
                if (_activeProfile == null || previewForm == null || previewForm.IsDisposed || _activeProfileBaseBitmap == null) return; // Need active profile and preview

                AnimationTimeline timelineToPlay = _activeProfile.Animations?.FirstOrDefault(a => a.Id == animationGuid);
                if (timelineToPlay != null)
                {
                    // --- Loop Start/Stop Logic ---
                    if (_currentlyLoopingTimelineId.HasValue && _currentlyLoopingTimelineId.Value == timelineToPlay.Id)
                    {
                        // Stop the currently looping animation because its hotkey was pressed again
                        previewForm.StopPlayback(true); // true to reset to static
                        _currentlyLoopingTimelineId = null;
                        Debug.WriteLine($"Stopped looping animation '{timelineToPlay.Name}' via Hotkey.");
                        UpdateUIState();
                    }
                    else
                    {
                        // Start a new animation (interrupting any current loop if necessary)
                        bool ready = await EnsureLitFramesGenerated(timelineToPlay.Id);
                        if (ready)
                        {
                            if (_litFrameCache.TryGetValue(timelineToPlay.Id, out var framesToPlay) && framesToPlay?.Count > 0)
                            {
                                // StartPlayback internally stops previous animation (StopPlayback(false))
                                previewForm.StartPlayback(framesToPlay, timelineToPlay.Loop);
                                // Update looping state AFTER starting playback
                                if (timelineToPlay.Loop)
                                {
                                    _currentlyLoopingTimelineId = timelineToPlay.Id;
                                    Debug.WriteLine($"Started looping animation '{timelineToPlay.Name}' via Hotkey.");
                                }
                                else
                                {
                                    _currentlyLoopingTimelineId = null; // Ensure it's clear if not looping
                                    Debug.WriteLine($"Started non-looping animation '{timelineToPlay.Name}' via Hotkey.");
                                }
                                UpdateUIState();
                            }
                            else
                            {
                                Debug.WriteLine($"Failed to retrieve frames for animation '{timelineToPlay.Name}' hotkey.");
                                _currentlyLoopingTimelineId = null; // Clear state on error
                            }
                        }
                        else
                        {
                            // EnsureLitFramesGenerated likely showed an error or was cancelled
                            Debug.WriteLine($"Frame generation failed or cancelled for animation '{timelineToPlay.Name}' hotkey.");
                            _currentlyLoopingTimelineId = null; // Clear state on failure
                        }
                    }
                    // --- End Loop Logic ---
                }
            }
        }

        private bool HotkeysMatch(HotkeyConfig hk1, HotkeyConfig hk2)
        { return hk1 != null && hk2 != null && hk1.Key == hk2.Key && hk1.Control == hk2.Control && hk1.Alt == hk2.Alt && hk1.Shift == hk2.Shift; }

        private async Task<bool> EnsureUnlitGeometryGenerated(Guid timelineId, CancellationToken? externalToken = null, IProgress<string> externalProgress = null)
        {
            bool isOnDemandCall = (externalToken == null);
            if (_unlitGeometryCache.ContainsKey(timelineId)) return true;
            if (_activeProfile == null || _activeProfileBaseBitmap == null || _activeProfileDepthBitmap == null || _activeProfileBaseBitmap.Width <= 0 || _activeProfileDepthBitmap.Width <= 0)
            { if (isOnDemandCall) MessageBox.Show("Cannot generate frames: Active profile images invalid.", "Error"); return false; }
            AnimationTimeline timeline = _activeProfile.Animations?.FirstOrDefault(a => a.Id == timelineId);
            if (timeline == null) return false;

            CancellationTokenSource internalCts = null; CancellationToken token;
            if (externalToken.HasValue) { token = externalToken.Value; }
            else { var ctsToLink = _isGeneratingFrames ? _frameGenerationCts : null; internalCts = ctsToLink?.IsCancellationRequested == false ? CancellationTokenSource.CreateLinkedTokenSource(ctsToLink.Token) : new CancellationTokenSource(); token = internalCts.Token; if (isOnDemandCall) SetGenerationStatus($"Generating '{timeline.Name}' geometry...", true); }
            var progressReporter = (IProgress<string>)(externalProgress ?? new Progress<string>(status => SetGenerationStatus(status, true)));

            byte[] basePixelData = null; byte[] depthPixelData = null; int baseStride = 0; int depthStride = 0; int width = 0; int height = 0;
            PixelFormat baseFormat = PixelFormat.Format32bppArgb; PixelFormat depthFormat = PixelFormat.Format24bppRgb;
            bool prepSuccess = false;
            Action prepAction = () => {
                try { if (_activeProfileBaseBitmap == null || _activeProfileDepthBitmap == null) throw new InvalidOperationException("Bitmaps null."); width = _activeProfileBaseBitmap.Width; height = _activeProfileBaseBitmap.Height; if (width <= 0 || height <= 0) throw new ArgumentException("Invalid dims."); if (_activeProfileDepthBitmap.Width != width || _activeProfileDepthBitmap.Height != height) throw new ArgumentException("Dim mismatch."); using (var baseTmp = new Bitmap(width, height, baseFormat)) { using (Graphics g = Graphics.FromImage(baseTmp)) g.DrawImageUnscaled(_activeProfileBaseBitmap, 0, 0); using (var depthTmp = new Bitmap(width, height, depthFormat)) { using (var origDepthClone = (Bitmap)_activeProfileDepthBitmap.Clone()) using (Graphics g = Graphics.FromImage(depthTmp)) g.DrawImageUnscaled(origDepthClone, 0, 0); Rectangle rect = new Rectangle(0, 0, width, height); BitmapData bdBase = null, bdDepth = null; try { bdBase = baseTmp.LockBits(rect, ImageLockMode.ReadOnly, baseFormat); bdDepth = depthTmp.LockBits(rect, ImageLockMode.ReadOnly, depthFormat); baseStride = bdBase.Stride; int baseSize = Math.Abs(baseStride) * height; basePixelData = new byte[baseSize]; Marshal.Copy(bdBase.Scan0, basePixelData, 0, baseSize); depthStride = bdDepth.Stride; int depthSize = Math.Abs(depthStride) * height; depthPixelData = new byte[depthSize]; Marshal.Copy(bdDepth.Scan0, depthPixelData, 0, depthSize); prepSuccess = true; } finally { if (bdBase != null) baseTmp.UnlockBits(bdBase); if (bdDepth != null) depthTmp.UnlockBits(bdDepth); } } } } catch { prepSuccess = false; }
            };
            prepAction();
            if (!prepSuccess) { if (isOnDemandCall) SetGenerationStatus("Data Prep Failed", false); try { internalCts?.Dispose(); } catch { } return false; }

            List<AnimationRenderer.FramePixelData> frameDataResult = null; bool success = false;
            try
            {
                float effectiveDepthScale = (_activeProfile.DepthScale > 0 ? _activeProfile.DepthScale : 1.0f) * 10.0f;
                frameDataResult = await Task.Run(() => AnimationRenderer.GenerateFrameData(timeline, basePixelData, baseStride, baseFormat, depthPixelData, depthStride, depthFormat, width, height, effectiveDepthScale, token, progressReporter), token);
                token.ThrowIfCancellationRequested();
                if (frameDataResult != null) { this.Invoke((MethodInvoker)delegate { if (this.IsDisposed || token.IsCancellationRequested) return; if (_unlitGeometryCache.TryGetValue(timelineId, out var old)) DisposeGeometryList(old); _unlitGeometryCache[timelineId] = frameDataResult; success = true; InvalidateLitFrameCache(timelineId); }); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (isOnDemandCall) this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) MessageBox.Show($"Error generating frames:\n{ex.Message}", "Error"); }); success = false; }
            finally { if (!success && frameDataResult == null) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) _unlitGeometryCache.Remove(timelineId); }); } if (isOnDemandCall && (!externalToken.HasValue || !externalToken.Value.IsCancellationRequested)) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus(token.IsCancellationRequested ? "Cancelled" : (success ? "Ready" : "Failed"), false); }); } try { internalCts?.Dispose(); } catch { } }
            return success;
        }


        private async Task<bool> EnsureLitFramesGenerated(Guid timelineId)
        {
            if (_isGeneratingFrames) return false;
            if (_activeProfile == null || _activeProfileBaseBitmap == null) return false;

            string currentLightingSig = GenerateLightingSignature();
            if (_litFrameCache.TryGetValue(timelineId, out var cachedFrames) && cachedFrames?.Count > 0 && _litCacheProfileId == _activeProfile.Id && _litCacheLightingSignature == currentLightingSig) return true;

            if (!_unlitGeometryCache.ContainsKey(timelineId))
            {
                bool unlitReady = await EnsureUnlitGeometryGenerated(timelineId, null, null); // <<< Corrected Call
                if (!unlitReady) return false;
                if (!_unlitGeometryCache.ContainsKey(timelineId)) { MessageBox.Show("Failed geometry gen.", "Error"); return false; }
            }

            var unlitGeometry = _unlitGeometryCache[timelineId];
            if (unlitGeometry == null || unlitGeometry.Count == 0) { MessageBox.Show("Geometry invalid.", "Error"); return false; }

            AnimationTimeline timeline = _activeProfile.Animations?.FirstOrDefault(a => a.Id == timelineId);
            string timelineName = timeline?.Name ?? timelineId.ToString();
            SetGenerationStatus($"Applying lighting to '{timelineName}'...", true);
            _isGeneratingFrames = true;
            CancellationTokenSource onDemandCts = null;
            try { _frameGenerationCts?.Cancel(); _frameGenerationCts?.Dispose(); } catch { }
            _frameGenerationCts = new CancellationTokenSource(); onDemandCts = _frameGenerationCts;
            var token = onDemandCts.Token;
            var progressReporter = (IProgress<string>)(new Progress<string>(status => SetGenerationStatus(status, true)));

            List<LightSource> globalLightsCopy = null; List<LightSource> profileLightsCopy = null;
            float specI = 0, specP = 0, effDepth = 0; int width = 0, height = 0; bool prepOk = false;
            try
            {
                globalLightsCopy = _currentSettings?.GlobalLights?.Select(l => l.Clone()).ToList() ?? new List<LightSource>(); profileLightsCopy = _activeProfile?.ProfileLights?.Select(l => l.Clone()).ToList() ?? new List<LightSource>(); specI = _activeProfile.SpecularIntensity; specP = _activeProfile.SpecularPower; effDepth = (_activeProfile.DepthScale > 0 ? _activeProfile.DepthScale : 1.0f) * 10.0f; width = _activeProfileBaseBitmap.Width; height = _activeProfileBaseBitmap.Height; prepOk = true;
            }
            catch { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus("Error preparing lights.", false); }); prepOk = false; }
            if (!prepOk) { _isGeneratingFrames = false; try { onDemandCts?.Dispose(); } catch { } if (_frameGenerationCts == onDemandCts) _frameGenerationCts = null; return false; }

            List<Bitmap> litFramesResult = null; bool success = false;
            try
            {
                litFramesResult = await Task.Run(() => AnimationRenderer.GenerateLitFrames(unlitGeometry, globalLightsCopy, profileLightsCopy, specI, specP, effDepth, width, height, token, progressReporter), token);
                token.ThrowIfCancellationRequested();
                if (litFramesResult != null) { this.Invoke((MethodInvoker)delegate { if (this.IsDisposed || token.IsCancellationRequested) return; InvalidateLitFrameCache(timelineId); _litFrameCache[timelineId] = litFramesResult; _litCacheProfileId = _activeProfile.Id; _litCacheLightingSignature = currentLightingSig; success = true; }); }
                else { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) InvalidateLitFrameCache(timelineId); }); success = false; }
            }
            catch (OperationCanceledException) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) { InvalidateLitFrameCache(timelineId); SetGenerationStatus("Generation Cancelled.", false); } }); success = false; }
            catch (Exception ex) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) { MessageBox.Show($"Error lighting:\n{ex.Message}", "Error"); InvalidateLitFrameCache(timelineId); } }); success = false; }
            finally { if (!token.IsCancellationRequested) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus(success ? "Ready" : "Lighting Failed", false); }); } _isGeneratingFrames = false; try { onDemandCts?.Dispose(); } catch { } if (_frameGenerationCts == onDemandCts) _frameGenerationCts = null; }
            return success;
        }

        private async Task StartBackgroundCacheGeneration(AvatarProfile profile) // <<< Corrected Method Name
        {
            if (profile == null || profile.Animations == null || !profile.Animations.Any()) { if (_isGeneratingFrames) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus("Ready", false); }); _isGeneratingFrames = false; try { _frameGenerationCts?.Dispose(); } catch { } _frameGenerationCts = null; } _preCacheTask = Task.FromResult(false); return; }
            if (profile.Id != _activeProfile?.Id || _activeProfileBaseBitmap == null || _activeProfileDepthBitmap == null) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus("Profile/Image Error", false); }); _preCacheTask = Task.FromResult(false); return; }

            if (_isGeneratingFrames || (_preCacheTask != null && !_preCacheTask.IsCompleted))
            {
                var previousCts = _frameGenerationCts; if (previousCts != null && !previousCts.IsCancellationRequested) { try { previousCts.Cancel(); } catch { } }
                try { await Task.WhenAny(_preCacheTask ?? Task.CompletedTask, Task.Delay(500)); } catch { }
                try { previousCts?.Dispose(); } catch { }
                this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus("Ready", false); }); await Task.Delay(50); _isGeneratingFrames = false; _frameGenerationCts = null;
            }

            _frameGenerationCts = new CancellationTokenSource(); var token = _frameGenerationCts.Token;
            Action<Tuple<int, int, string>> progressAction = (update) => { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) { SetGenerationStatus($"{update.Item3} ({update.Item1}/{update.Item2})", true); } }); };
            var progress = new Progress<Tuple<int, int, string>>(progressAction);
            var progressReporter = (IProgress<Tuple<int, int, string>>)progress; // <<< Corrected Cast

            var animationsToCache = profile.Animations.ToList(); int totalAnims = animationsToCache.Count;
            string profileName = profile.ProfileName; Guid profileId = profile.Id;

            _isGeneratingFrames = true; SetGenerationStatus($"Pre-caching 0/{totalAnims * 2} steps for '{profileName}'...", true);

            _preCacheTask = Task.Run(async () =>
            {
                int successfulUnlit = 0; int successfulLit = 0; bool wasCancelledInternally = false;
                string finalMessage = "Pre-caching started."; // <<< Corrected Init
                string currentLightingSig = "";

                try
                {
                    this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) currentLightingSig = GenerateLightingSignature(); });
                    if (string.IsNullOrEmpty(currentLightingSig)) throw new InvalidOperationException("Failed sig gen.");

                    // Phase 1: Unlit
                    for (int i = 0; i < totalAnims; i++) { token.ThrowIfCancellationRequested(); var anim = animationsToCache[i]; progressReporter.Report(Tuple.Create(i + 1, totalAnims, $"Pre-caching Unlit '{anim.Name}'")); /*<<< Corrected Report Call */ bool unlitResult = await EnsureUnlitGeometryGenerated(anim.Id, token, null); /*<<< Corrected Call */ if (token.IsCancellationRequested) { wasCancelledInternally = true; break; } if (unlitResult) successfulUnlit++; }
                    if (wasCancelledInternally) throw new OperationCanceledException(token);

                    // Phase 2: Lit
                    if (successfulUnlit == totalAnims) { List<LightSource> globalLightsCopy = null; List<LightSource> profileLightsCopy = null; float specI = 0, specP = 0, effDepth = 0; int width = 0, height = 0; bool lightingPrepOk = false; this.Invoke((MethodInvoker)delegate { if (this.IsDisposed) return; try { globalLightsCopy = _currentSettings?.GlobalLights?.Select(l => l.Clone()).ToList() ?? new List<LightSource>(); profileLightsCopy = _activeProfile?.ProfileLights?.Select(l => l.Clone()).ToList() ?? new List<LightSource>(); specI = _activeProfile.SpecularIntensity; specP = _activeProfile.SpecularPower; effDepth = (_activeProfile.DepthScale > 0 ? _activeProfile.DepthScale : 1.0f) * 10.0f; width = _activeProfileBaseBitmap.Width; height = _activeProfileBaseBitmap.Height; lightingPrepOk = true; } catch { lightingPrepOk = false; } }); if (!lightingPrepOk) throw new InvalidOperationException("Failed light prep."); for (int i = 0; i < totalAnims; i++) { token.ThrowIfCancellationRequested(); var anim = animationsToCache[i]; progressReporter.Report(Tuple.Create(i + 1, totalAnims, $"Pre-caching Lit '{anim.Name}'")); /*<<< Corrected Report Call */ if (!_unlitGeometryCache.TryGetValue(anim.Id, out var unlitGeom) || unlitGeom == null || unlitGeom.Count == 0) continue; List<Bitmap> litFramesResult = AnimationRenderer.GenerateLitFrames(unlitGeom, globalLightsCopy, profileLightsCopy, specI, specP, effDepth, width, height, token, null); if (token.IsCancellationRequested) { wasCancelledInternally = true; break; } if (litFramesResult != null) { this.Invoke((MethodInvoker)delegate { if (this.IsDisposed || token.IsCancellationRequested) return; InvalidateLitFrameCache(anim.Id); _litFrameCache[anim.Id] = litFramesResult; _litCacheProfileId = profileId; _litCacheLightingSignature = currentLightingSig; }); successfulLit++; } else { this.Invoke((MethodInvoker)delegate { if (this.IsDisposed) return; InvalidateLitFrameCache(anim.Id); }); } } if (wasCancelledInternally) throw new OperationCanceledException(token); }

                    if (successfulUnlit == totalAnims && successfulLit == totalAnims) finalMessage = $"Finished pre-caching {totalAnims} for '{profileName}'.";
                    else finalMessage = $"Finished pre-caching for '{profileName}' (Unlit: {successfulUnlit}/{totalAnims}, Lit: {successfulLit}/{totalAnims}).";
                }
                catch (OperationCanceledException) { finalMessage = "Pre-caching Cancelled."; wasCancelledInternally = true; }
                catch (Exception ex) { finalMessage = "Pre-caching Error."; this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) MessageBox.Show($"Error pre-caching:\n{ex.Message}", "Error"); }); }
                finally { progressReporter.Report(Tuple.Create(totalAnims, totalAnims, finalMessage)); /* <<< Corrected Report Call */ }
                return wasCancelledInternally;
            }, token);

            // Handle completion
            bool cancelled = false; try { cancelled = await _preCacheTask; }
            catch { }
            finally { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) { SetGenerationStatus(cancelled ? "Pre-caching Cancelled." : "Ready", false); _isGeneratingFrames = false; try { _frameGenerationCts?.Dispose(); } catch { } _frameGenerationCts = null; } }); }
        }

        private async Task TriggerBackgroundRelight(Guid timelineId, string expectedOldSignature)
        {
            if (_isGeneratingFrames) return;
            if (_activeProfile == null || _activeProfileBaseBitmap == null) return;

            if (!_unlitGeometryCache.ContainsKey(timelineId))
            {
                if (!await EnsureUnlitGeometryGenerated(timelineId)) // <<< Corrected Call
                { InvalidateLitFrameCache(timelineId); return; }
                if (!_unlitGeometryCache.ContainsKey(timelineId)) { InvalidateLitFrameCache(timelineId); return; }
            }
            var unlitGeometry = _unlitGeometryCache[timelineId];
            if (unlitGeometry == null || unlitGeometry.Count == 0) return;

            AnimationTimeline timeline = _activeProfile.Animations?.FirstOrDefault(a => a.Id == timelineId);
            string timelineName = timeline?.Name ?? timelineId.ToString();
            string newLightingSig = GenerateLightingSignature();

            SetGenerationStatus($"Re-lighting '{timelineName}'...", true);
            _isGeneratingFrames = true;
            _frameGenerationCts?.Dispose(); _frameGenerationCts = new CancellationTokenSource();
            var token = _frameGenerationCts.Token;
            var progressReporter = (IProgress<string>)(new Progress<string>(status => SetGenerationStatus(status, true))); // Cast for Report

            List<LightSource> globalLightsCopy = null; List<LightSource> profileLightsCopy = null;
            float specI = 0, specP = 0, effDepth = 0; int width = 0, height = 0; bool prepOk = false;
            try
            {
                globalLightsCopy = _currentSettings?.GlobalLights?.Select(l => l.Clone()).ToList() ?? new List<LightSource>(); profileLightsCopy = _activeProfile?.ProfileLights?.Select(l => l.Clone()).ToList() ?? new List<LightSource>(); specI = _activeProfile.SpecularIntensity; specP = _activeProfile.SpecularPower; effDepth = (_activeProfile.DepthScale > 0 ? _activeProfile.DepthScale : 1.0f) * 10.0f; width = _activeProfileBaseBitmap.Width; height = _activeProfileBaseBitmap.Height; prepOk = true;
            }
            catch { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus("Error preparing lights.", false); }); prepOk = false; }
            if (!prepOk) { _isGeneratingFrames = false; _frameGenerationCts?.Dispose(); _frameGenerationCts = null; return; }

            List<Bitmap> litFramesResult = null; bool success = false;
            try
            {
                litFramesResult = await Task.Run(() => AnimationRenderer.GenerateLitFrames(unlitGeometry, globalLightsCopy, profileLightsCopy, specI, specP, effDepth, width, height, token, progressReporter), token); // Pass Reporter
                token.ThrowIfCancellationRequested();
                if (litFramesResult != null)
                {
                    this.Invoke((MethodInvoker)delegate {
                        if (this.IsDisposed || token.IsCancellationRequested) return;
                        InvalidateLitFrameCache(timelineId);
                        _litFrameCache[timelineId] = litFramesResult;
                        _litCacheProfileId = _activeProfile.Id; _litCacheLightingSignature = newLightingSig;
                        if (_currentlyPlayingTimelineId == timelineId) { if (previewForm != null && !previewForm.IsDisposed) { previewForm.UpdatePlaybackFrames(litFramesResult); } else { _currentlyPlayingTimelineId = null; } }
                        success = true;
                    });
                }
                else { InvalidateLitFrameCache(timelineId); success = false; }
            }
            catch (OperationCanceledException) { InvalidateLitFrameCache(timelineId); success = false; SetGenerationStatus("Re-light Cancelled.", false); }
            catch (Exception ex) { MessageBox.Show($"Error re-lighting '{timelineName}':\n{ex.Message}"); InvalidateLitFrameCache(timelineId); success = false; }
            finally { if (!_frameGenerationCts?.IsCancellationRequested ?? true) { this.Invoke((MethodInvoker)delegate { if (!this.IsDisposed) SetGenerationStatus(success ? "Ready" : "Re-light Failed", false); }); } _isGeneratingFrames = false; _frameGenerationCts?.Dispose(); _frameGenerationCts = null; }
        }

        private void ShowControlPanel()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        private void LightEditorForm_SettingsChanged(object sender, EventArgs e)
        {
            string oldSignature = _litCacheLightingSignature;
            InvalidateLitFrameCache();

            if (_currentSettings != null)
            { SettingsManager.SaveSettings(_currentSettings); }

            TriggerPreviewRefresh();

            if (_currentlyPlayingTimelineId.HasValue)
            {
                _ = TriggerBackgroundRelight(_currentlyPlayingTimelineId.Value, oldSignature);
            }
        }

        private void SetGenerationStatus(string message, bool busy)
        {
            if (statusStrip1.InvokeRequired) { try { statusStrip1.BeginInvoke(new Action(() => SetGenerationStatus(message, busy))); } catch { } return; }
            if (this.IsDisposed || !statusStrip1.IsHandleCreated || statusStrip1.IsDisposed) return;

            try
            {
                toolStripStatusLabel.Text = message;
                bool enableControls = !busy;
                btnCancelGeneration.Visible = busy;
                if (busy && !(message?.StartsWith("Cancelling") ?? false) && !(message?.StartsWith("Finished") ?? false) && !(message?.StartsWith("Ready") ?? false) && !(message?.StartsWith("Error") ?? false))
                { if (!btnCancelGeneration.IsDisposed && !btnCancelGeneration.Enabled) btnCancelGeneration.Enabled = true; }
                else { if (!btnCancelGeneration.IsDisposed && btnCancelGeneration.Enabled) btnCancelGeneration.Enabled = false; }

                Action updateControlsAction = () => {
                    if (this.IsDisposed) return;
                    if (cmbActiveProfile != null && !cmbActiveProfile.IsDisposed) cmbActiveProfile.Enabled = (_currentSettings?.Profiles?.Any(IsProfileValid) ?? false) && enableControls;
                    bool profileImagesValid = _activeProfile != null && _activeProfileBaseBitmap != null && _activeProfileDepthBitmap != null; bool animationsExist = (_activeProfile?.Animations?.Any() ?? false); bool canInteractAnimList = enableControls && profileImagesValid && animationsExist;
                    if (cmbSelectAnimation != null && !cmbSelectAnimation.IsDisposed) cmbSelectAnimation.Enabled = canInteractAnimList; if (btnPlayAnimation != null && !btnPlayAnimation.IsDisposed) btnPlayAnimation.Enabled = canInteractAnimList && (cmbSelectAnimation.SelectedItem is AnimationTimeline); if (btnShowSetup != null && !btnShowSetup.IsDisposed) btnShowSetup.Enabled = enableControls; if (btnEditCurrentProfile != null && !btnEditCurrentProfile.IsDisposed) btnEditCurrentProfile.Enabled = enableControls && (_activeProfile != null); if (btnEditLights != null && !btnEditLights.IsDisposed) btnEditLights.Enabled = enableControls; bool canShowPreview = enableControls && profileImagesValid; if (btnShowPreview != null && !btnShowPreview.IsDisposed) btnShowPreview.Enabled = canShowPreview; if (showHidePreviewToolStripMenuItem != null && !showHidePreviewToolStripMenuItem.IsDisposed) { showHidePreviewToolStripMenuItem.Enabled = canShowPreview; }
                };
                if (this.InvokeRequired) { this.BeginInvoke(updateControlsAction); } else { updateControlsAction(); }
            }
            catch { }
        }

        private void UpdateUIState()
        {
            Action updateAction = () => {
                if (this.IsDisposed) return;
                bool previewVisible = (previewForm != null && !previewForm.IsDisposed && previewForm.Visible);
                if (showHidePreviewToolStripMenuItem != null && !showHidePreviewToolStripMenuItem.IsDisposed) { showHidePreviewToolStripMenuItem.Text = previewVisible ? "Hide Preview Window" : "Show Preview Window"; }
                if (enableDisableEffectsToolStripMenuItem != null && !enableDisableEffectsToolStripMenuItem.IsDisposed) { enableDisableEffectsToolStripMenuItem.Checked = areEffectsEnabled; }
                if (showHideControlPanelToolStripMenuItem != null && !showHideControlPanelToolStripMenuItem.IsDisposed) { showHideControlPanelToolStripMenuItem.Text = this.Visible ? "Hide Control Panel" : "Show Control Panel"; }
                if (btnShowPreview != null && !btnShowPreview.IsDisposed) { btnShowPreview.Text = previewVisible ? "Hide Preview Window" : "Show Preview Window"; }

                // --- ADDED: Update Play Button Text ---
                bool isCurrentAnimationLooping = false;
                if (cmbSelectAnimation.SelectedItem is AnimationTimeline selectedAnim && _currentlyLoopingTimelineId.HasValue && selectedAnim.Id == _currentlyLoopingTimelineId.Value)
                {
                    isCurrentAnimationLooping = true;
                }

                if (btnPlayAnimation != null && !btnPlayAnimation.IsDisposed)
                {
                    btnPlayAnimation.Text = isCurrentAnimationLooping ? "Stop Loop" : "Play";
                }
                // --- END ADDED ---

                // Update general control enable state based on generation status
                SetGenerationStatus(toolStripStatusLabel.Text, _isGeneratingFrames);
            };

            if (this.InvokeRequired) { if (!this.IsDisposed) try { this.BeginInvoke(updateAction); } catch { /* Ignore */ } }
            else { if (!this.IsDisposed) updateAction(); }
        }

        private string GenerateLightingSignature()
        {
            if (_activeProfile == null || _currentSettings == null) return $"INVALID_STATE_{DateTime.Now.Ticks}"; var sb = new StringBuilder(); sb.Append($"ProfID:{_activeProfile.Id};"); sb.Append($"DepthScl:{_activeProfile.DepthScale.ToString("R")};"); sb.Append($"SpecI:{_activeProfile.SpecularIntensity.ToString("R")};"); sb.Append($"SpecP:{_activeProfile.SpecularPower.ToString("R")};"); Action<LightSource, string> appendLightDetails = (light, prefix) => { if (light == null || !light.IsEnabled) return; sb.Append($"{prefix}_T:{light.Type};"); sb.Append($"{prefix}_C:{light.Color.ToArgb()};"); sb.Append($"{prefix}_I:{light.Intensity.ToString("R")};"); switch (light.Type) { case LightType.Point: sb.Append($"{prefix}_P:{light.Position.X.ToString("R")},{light.Position.Y.ToString("R")},{light.Position.Z.ToString("R")};"); sb.Append($"{prefix}_D:{light.Direction.X.ToString("R")},{light.Direction.Y.ToString("R")},{light.Direction.Z.ToString("R")};"); sb.Append($"{prefix}_Att:{light.ConstantAttenuation.ToString("R")},{light.LinearAttenuation.ToString("R")},{light.QuadraticAttenuation.ToString("R")};"); sb.Append($"{prefix}_Spot:{light.SpotCutoffAngle.ToString("R")},{light.SpotExponent.ToString("R")};"); break; case LightType.Directional: sb.Append($"{prefix}_D:{light.Direction.X.ToString("R")},{light.Direction.Y.ToString("R")},{light.Direction.Z.ToString("R")};"); break; } }; sb.Append("GlobalLights:["); _currentSettings.GlobalLights?.ForEach(light => appendLightDetails(light, "G")); sb.Append("];"); sb.Append("ProfileLights:["); _activeProfile.ProfileLights?.ForEach(light => appendLightDetails(light, "P")); sb.Append("];"); return sb.ToString();
        }
        private PointF[] CalculateTransformedCorners(AnimationKeyframe keyframe, Size imageSize)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0) return new PointF[4]; float offsetX = keyframe.Transform.X; float offsetY = keyframe.Transform.Y; float scale = Math.Max(0.01f, keyframe.Transform.Z); AnchorPoint anchor = keyframe.Anchor; float w = imageSize.Width; float h = imageSize.Height; PointF tl_orig = new PointF(0, 0); PointF tr_orig = new PointF(w, 0); PointF br_orig = new PointF(w, h); PointF bl_orig = new PointF(0, h); PointF anchorPoint = PointF.Empty; switch (anchor) { case AnchorPoint.Top: anchorPoint = new PointF(w / 2f, 0); break; case AnchorPoint.Bottom: anchorPoint = new PointF(w / 2f, h); break; case AnchorPoint.Left: anchorPoint = new PointF(0, h / 2f); break; case AnchorPoint.Right: anchorPoint = new PointF(w, h / 2f); break; default: anchorPoint = new PointF(w / 2f, h / 2f); break; }
            Func<PointF, PointF> transformPoint = (PointF originalCorner) => { float scaledX = anchorPoint.X + (originalCorner.X - anchorPoint.X) * scale; float scaledY = anchorPoint.Y + (originalCorner.Y - anchorPoint.Y) * scale; float finalX = scaledX + offsetX; float finalY = scaledY + offsetY; finalX -= w / 2f; finalY -= h / 2f; return new PointF(finalX, finalY); }; PointF[] transformedCorners = new PointF[4]; transformedCorners[0] = transformPoint(tl_orig); transformedCorners[1] = transformPoint(tr_orig); transformedCorners[2] = transformPoint(br_orig); transformedCorners[3] = transformPoint(bl_orig); return transformedCorners;
        }
        private RectangleF CalculateAnimationBounds(AnimationTimeline timeline, Size imageSize)
        {
            if (timeline?.Keyframes == null || timeline.Keyframes.Count == 0 || imageSize.Width <= 0 || imageSize.Height <= 0) { return new RectangleF(-imageSize.Width / 2f, -imageSize.Height / 2f, imageSize.Width, imageSize.Height); }
            float minX = float.MaxValue; float minY = float.MaxValue; float maxX = float.MinValue; float maxY = float.MinValue; minX = -imageSize.Width / 2f; minY = -imageSize.Height / 2f; maxX = imageSize.Width / 2f; maxY = imageSize.Height / 2f; foreach (var keyframe in timeline.Keyframes) { PointF[] corners = CalculateTransformedCorners(keyframe, imageSize); foreach (var corner in corners) { minX = Math.Min(minX, corner.X); minY = Math.Min(minY, corner.Y); maxX = Math.Max(maxX, corner.X); maxY = Math.Max(maxY, corner.Y); } }
            float width = (maxX > minX) ? maxX - minX : 0; float height = (maxY > minY) ? maxY - minY : 0; return new RectangleF(minX, minY, width, height);
        }
        private RectangleF CalculateMaxProfileAnimationBounds(AvatarProfile profile, Size imageSize)
        {
            if (profile?.Animations == null || !profile.Animations.Any() || imageSize.Width <= 0 || imageSize.Height <= 0) { return new RectangleF(-imageSize.Width / 2f, -imageSize.Height / 2f, imageSize.Width, imageSize.Height); }
            float minX = -imageSize.Width / 2f; float minY = -imageSize.Height / 2f; float maxX = imageSize.Width / 2f; float maxY = imageSize.Height / 2f; foreach (var timeline in profile.Animations) { RectangleF animBounds = CalculateAnimationBounds(timeline, imageSize); if (!animBounds.IsEmpty) { minX = Math.Min(minX, animBounds.Left); minY = Math.Min(minY, animBounds.Top); maxX = Math.Max(maxX, animBounds.Right); maxY = Math.Max(maxY, animBounds.Bottom); } }
            float overallWidth = (maxX > minX) ? maxX - minX : 0; float overallHeight = (maxY > minY) ? maxY - minY : 0; var maxBounds = new RectangleF(minX, minY, overallWidth, overallHeight); return maxBounds;
        }
        private void InvalidateLitFrameCache() { foreach (var kvp in _litFrameCache) { DisposeBitmapList(kvp.Value); } _litFrameCache.Clear(); _litCacheProfileId = null; _litCacheLightingSignature = null; }
        private void InvalidateLitFrameCache(Guid timelineId) { if (_litFrameCache.TryGetValue(timelineId, out var list)) { DisposeBitmapList(list); _litFrameCache.Remove(timelineId); } }
        private void InvalidateUnlitGeometryCacheForProfile(AvatarProfile profile) { if (profile?.Animations == null || _unlitGeometryCache == null) return; List<Guid> ids = profile.Animations.Select(a => a.Id).ToList(); foreach (Guid id in ids) { if (_unlitGeometryCache.TryGetValue(id, out var list)) { DisposeGeometryList(list); _unlitGeometryCache.Remove(id); } } }
        private void ClearAllAnimationCaches() { foreach (var kvp in _unlitGeometryCache) { DisposeGeometryList(kvp.Value); } _unlitGeometryCache.Clear(); InvalidateLitFrameCache(); }
        private void DisposeBitmapList(List<Bitmap> list) { if (list == null) return; foreach (var bmp in list) { bmp?.Dispose(); } list.Clear(); }
        private void DisposeGeometryList(List<AnimationRenderer.FramePixelData> list) { if (list == null) return; foreach (var geo in list) { geo?.Dispose(); } list.Clear(); }
        private bool IsProfileValid(AvatarProfile profile) { if (profile == null) return false; return !string.IsNullOrEmpty(profile.BaseImagePath) && File.Exists(profile.BaseImagePath) && !string.IsNullOrEmpty(profile.DepthMapPath) && File.Exists(profile.DepthMapPath); }
        private void UpdatePreviewFormWithActiveProfile()
        {
            bool refreshNeeded = false;

            if (previewForm != null && !previewForm.IsDisposed)
            {
                if (_activeProfile != null)
                {
                    bool loaded = previewForm.LoadAvatar(_activeProfile.BaseImagePath, _activeProfile.DepthMapPath);
                    if (loaded)
                    {
                        refreshNeeded = true;
                    }
                }
                else
                {
                    previewForm.LoadAvatar(null, null);
                    previewForm.Hide();
                    refreshNeeded = true;
                }
            }
            else if (_activeProfile != null && previewForm == null)
            {
                // Refresh deferred until shown
            }

            if (refreshNeeded)
            {
                TriggerPreviewRefresh();
            }
        }
        private void TriggerPreviewRefresh() { if (previewForm != null && !previewForm.IsDisposed && _currentSettings != null && _activeProfile != null) { previewForm.RefreshEffects(_currentSettings.GlobalLights, _activeProfile.ProfileLights, _activeProfile.DepthScale, _activeProfile.SpecularIntensity, _activeProfile.SpecularPower); } }
        private void ShowPreviewFormInstance() { if (_isGeneratingFrames) { MessageBox.Show("Wait for caching.", "Busy"); return; } if (_activeProfile == null || !IsProfileValid(_activeProfile)) { MessageBox.Show("No valid profile.", "Setup Required"); return; } if (_activeProfileBaseBitmap == null || _activeProfileDepthBitmap == null) { if (!LoadActiveProfileImages(_activeProfile)) { MessageBox.Show($"Failed img load '{_activeProfile.ProfileName}'.", "Error"); return; } RecalculateAndApplyMaxSize(); } if (previewForm != null && !previewForm.IsDisposed) { if (!previewForm.Visible) { if (!_activeProfileMaxSize.IsEmpty && previewForm.Size != _activeProfileMaxSize) { previewForm.SetFixedSize(_activeProfileMaxSize); } TriggerPreviewRefresh(); previewForm.Visible = true; } previewForm.Activate(); UpdateUIState(); return; } Size initSize = _activeProfileMaxSize.IsEmpty ? (_activeProfileBaseBitmap?.Size ?? new Size(256, 256)) : _activeProfileMaxSize; if (initSize.Width <= 0 || initSize.Height <= 0) initSize = new Size(256, 256); Point initPos = Point.Empty; Point? savedPos = _currentSettings?.PreviewWindowPosition; if (savedPos.HasValue) { initPos = savedPos.Value; } else { Screen scr = Screen.FromPoint(this.Location); int x = scr.WorkingArea.Left + Math.Max(0, (scr.WorkingArea.Width - initSize.Width) / 2); int y = scr.WorkingArea.Top + Math.Max(0, (scr.WorkingArea.Height - initSize.Height) / 2); initPos = new Point(x, y); } previewForm = new PreviewForm(initPos, initSize); previewForm.PositionChanged += PreviewForm_PositionChanged; previewForm.FormClosed += (s, args) => { if (previewForm != null) previewForm.PositionChanged -= PreviewForm_PositionChanged; previewForm = null; UpdateUIState(); }; bool loaded = previewForm.LoadAvatar(_activeProfile.BaseImagePath, _activeProfile.DepthMapPath); if (loaded) { TriggerPreviewRefresh(); previewForm.Show(this); } else { try { previewForm.Close(); previewForm.Dispose(); } catch { } previewForm = null; } UpdateUIState(); }
        private void ShowSetupFormInstance() { if (profileManagerForm == null || profileManagerForm.IsDisposed) { profileManagerForm = new ProfileManagerForm(); profileManagerForm.FormClosed += (s, args) => { profileManagerForm = null; LoadAndApplySettings(); }; profileManagerForm.Show(this); } else { profileManagerForm.Activate(); } }
        private void PreviewForm_PositionChanged(object sender, Point newPosition) { if (_currentSettings != null) { _currentSettings.PreviewWindowPosition = newPosition; SettingsManager.SaveSettings(_currentSettings); } }
        private void HandleProfileEditPreviewRequest(AvatarProfile editedProfileData) { if (previewForm != null && !previewForm.IsDisposed && previewForm.Visible && _activeProfile != null && editedProfileData != null && _activeProfile.Id == editedProfileData.Id) { bool imgChanged = _activeProfileBaseBitmap == null || previewForm.CurrentBasePath == null || _activeProfile.BaseImagePath != previewForm.CurrentBasePath || _activeProfile.DepthMapPath != previewForm.CurrentDepthPath; bool loaded = true; if (imgChanged) { loaded = LoadActiveProfileImages(_activeProfile) && previewForm.LoadAvatar(_activeProfile.BaseImagePath, _activeProfile.DepthMapPath); if (loaded) { RecalculateAndApplyMaxSize(); InvalidateUnlitGeometryCacheForProfile(_activeProfile); } } if (loaded) { TriggerPreviewRefresh(); } } }
        private void RecalculateAndApplyMaxSize() { if (_activeProfile == null || _activeProfileBaseBitmap == null) return; _activeProfileMaxSize = Size.Empty; try { RectangleF bounds = CalculateMaxProfileAnimationBounds(_activeProfile, _activeProfileBaseBitmap.Size); float pad = 20f; float bw = Math.Max(0, bounds.Width); float bh = Math.Max(0, bounds.Height); _activeProfileMaxSize = new Size((int)Math.Ceiling(bw + pad * 2), (int)Math.Ceiling(bh + pad * 2)); _activeProfileMaxSize.Width = Math.Max(_activeProfileMaxSize.Width, _activeProfileBaseBitmap.Width); _activeProfileMaxSize.Height = Math.Max(_activeProfileMaxSize.Height, _activeProfileBaseBitmap.Height); if (previewForm != null && !previewForm.IsDisposed && previewForm.Visible) { previewForm.SetFixedSize(_activeProfileMaxSize); } } catch { _activeProfileMaxSize = _activeProfileBaseBitmap?.Size ?? Size.Empty; if (previewForm != null && !previewForm.IsDisposed && previewForm.Visible && !_activeProfileMaxSize.IsEmpty) { previewForm.SetFixedSize(_activeProfileMaxSize); } } }

        private void ShowHidePreviewMenuItem_Click(object sender, EventArgs e)
        {
            if (_isGeneratingFrames && (previewForm == null || previewForm.IsDisposed || !previewForm.Visible))
            {
                MessageBox.Show("Please wait for animation pre-caching to complete before showing the preview.", "Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_activeProfile == null) { MessageBox.Show("No active profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (previewForm != null && !previewForm.IsDisposed) { previewForm.Visible = !previewForm.Visible; }
            else { ShowPreviewFormInstance(); }
            UpdateUIState();
        }

        private void UpdateAnimationComboBox()
        {
            object selectedValue = cmbSelectAnimation.SelectedValue;
            cmbSelectAnimation.DataSource = null;
            cmbSelectAnimation.Items.Clear();

            var animationsToShow = _activeProfile?.Animations;

            if (animationsToShow != null && animationsToShow.Any())
            {
                var sortedAnimations = animationsToShow.OrderBy(a => a.Name).ToList();
                cmbSelectAnimation.DisplayMember = nameof(AnimationTimeline.Name);
                cmbSelectAnimation.ValueMember = nameof(AnimationTimeline.Id);
                cmbSelectAnimation.DataSource = sortedAnimations;
                if (selectedValue != null) cmbSelectAnimation.SelectedValue = selectedValue;
                if (cmbSelectAnimation.SelectedIndex == -1 && cmbSelectAnimation.Items.Count > 0) cmbSelectAnimation.SelectedIndex = 0;
            }
            else
            {
                cmbSelectAnimation.Items.Add("(No animations in profile)");
                cmbSelectAnimation.SelectedIndex = 0;
            }
            UpdateUIState();
        }



        private void WireUpEvents()
        {
            this.Load += ControlPanelForm_Load;
            this.Resize += ControlPanelForm_Resize;
            this.FormClosing += ControlPanelForm_FormClosing;

            if (_hotkeyWindow != null) { _hotkeyWindow.HotkeyActivated += HandleHotkeyActivated; }

            if (notifyIcon1 != null) notifyIcon1.DoubleClick += NotifyIcon_DoubleClick;
            if (showHideControlPanelToolStripMenuItem != null) showHideControlPanelToolStripMenuItem.Click += ShowHideControlPanelMenuItem_Click;
            if (showHidePreviewToolStripMenuItem != null) showHidePreviewToolStripMenuItem.Click += ShowHidePreviewMenuItem_Click;
            if (enableDisableEffectsToolStripMenuItem != null) enableDisableEffectsToolStripMenuItem.Click += EnableDisableEffectsMenuItem_Click;
            if (exitApplicationToolStripMenuItem != null) exitApplicationToolStripMenuItem.Click += ExitApplicationMenuItem_Click;

            if (btnShowPreview != null) btnShowPreview.Click += BtnShowPreview_Click;
            if (btnShowSetup != null) btnShowSetup.Click += BtnShowSetup_Click;
            if (btnEditCurrentProfile != null) btnEditCurrentProfile.Click += BtnEditCurrentProfile_Click;
            if (btnEditLights != null) btnEditLights.Click += BtnEditLights_Click;
            if (cmbActiveProfile != null) cmbActiveProfile.SelectedIndexChanged += CmbActiveProfile_SelectedIndexChanged;
            if (cmbSelectAnimation != null) cmbSelectAnimation.SelectedIndexChanged += CmbSelectAnimation_SelectedIndexChanged;
            if (btnPlayAnimation != null) btnPlayAnimation.Click += BtnPlayAnimation_Click;
            if (btnCancelGeneration != null) btnCancelGeneration.Click += BtnCancelGeneration_Click;
        }

    }
}
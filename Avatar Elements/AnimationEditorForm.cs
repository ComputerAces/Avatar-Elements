// >>> START MODIFICATION: AnimationEditorForm.cs (Full File - Placeholders Removed)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms; // Added for Keys
using Avatar_Elements.Data;
using Avatar_Elements.Helpers;
using System.Diagnostics;
using Microsoft.VisualBasic; // For InputBox
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Avatar_Elements {
    public partial class AnimationEditorForm : Form {
        // --- Member Variables ---
        private readonly AvatarProfile _profile;
        private readonly AppSettings _appSettings;
        private AnimationTimeline _selectedTimeline = null;
        private AnimationKeyframe _selectedKeyframe = null;
        private AnimationKeyframe _originalKeyframeState = null;
        private bool _isLoading = false;
        private bool _isDirty = false;
        private bool _isKeyframeDirty = false;
        private bool _isListeningForTimelineHotkey = false;

        // --- Constructor ---
        public AnimationEditorForm(AvatarProfile profile, AppSettings settings)
        {
            InitializeComponent();
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _appSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (_profile.Animations == null) { _profile.Animations = new List<AnimationTimeline>(); }
            this.KeyPreview = true;
        }

        // --- Form Load & Closing ---
        private void AnimationEditorForm_Load(object sender, EventArgs e)
        {
            _isLoading = true;
            WireUpEvents();

            // --- Populate Interpolation ComboBox ---
            comboInterpolation.Items.Clear();
            comboInterpolation.Items.AddRange(Enum.GetNames(typeof(InterpolationType)));
            if (comboInterpolation.Items.Count > 0)
                comboInterpolation.SelectedIndex = comboInterpolation.Items.IndexOf("Linear"); // Default to Linear visually
            else
                comboInterpolation.SelectedIndex = -1;
            // --- END ---

            PopulateTimelineList();
            UpdateTimelineButtonStates();
            UpdateTimelineControlsState();
            UpdateKeyframeAreaState();
            ClearAndDisableKeyframeEditor();
            _isLoading = false;
            MarkClean();
        }

        private void AnimationEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isListeningForTimelineHotkey) StopListeningForTimelineHotkey();
            if (!PromptSaveChangesIfDirty()) { e.Cancel = true; }
        }

        private void WireUpEvents()
        {
            this.Load += AnimationEditorForm_Load;
            this.FormClosing += AnimationEditorForm_FormClosing;
            this.KeyDown += AnimationEditorForm_KeyDown;

            listViewTimelines.SelectedIndexChanged += ListViewTimelines_SelectedIndexChanged;
            listViewKeyframes.SelectedIndexChanged += ListViewKeyframes_SelectedIndexChanged;

            btnAddTimeline.Click += BtnAddTimeline_Click;
            btnRemoveTimeline.Click += BtnRemoveTimeline_Click;
            btnRenameTimeline.Click += BtnRenameTimeline_Click;

            // --- ADDED: Copy/Paste Button Events ---
            if (this.Controls.Find("btnCopyTimeline", true).FirstOrDefault() is Button btnCopy)
                btnCopy.Click += BtnCopyTimeline_Click;
            else Debug.WriteLine("WARNING: btnCopyTimeline not found!");

            if (this.Controls.Find("btnPasteTimeline", true).FirstOrDefault() is Button btnPaste)
                btnPaste.Click += BtnPasteTimeline_Click;
            else Debug.WriteLine("WARNING: btnPasteTimeline not found!");
            // --- END ADDED ---

            if (this.Controls.Find("btnSetTimelineHotkey", true).FirstOrDefault() is Button btnSetHK)
                btnSetHK.Click += BtnSetTimelineHotkey_Click;
            else Debug.WriteLine("WARNING: btnSetTimelineHotkey not found!");
            if (this.Controls.Find("btnClearTimelineHotkey", true).FirstOrDefault() is Button btnClearHK)
                btnClearHK.Click += BtnClearTimelineHotkey_Click;
            else Debug.WriteLine("WARNING: btnClearTimelineHotkey not found!");

            // --- ADDED: Checkbox Changed Event ---
            if (this.Controls.Find("chkTimelineLoop", true).FirstOrDefault() is CheckBox chkLoop)
                chkLoop.CheckedChanged += ChkTimelineLoop_CheckedChanged;
            else Debug.WriteLine("WARNING: chkTimelineLoop not found!");
            // --- END ADDED ---

            btnAddKeyframe.Click += BtnAddKeyframe_Click;
            btnRemoveKeyframe.Click += BtnRemoveKeyframe_Click;
            btnMoveKeyframeUp.Click += BtnMoveKeyframeUp_Click;
            btnMoveKeyframeDown.Click += BtnMoveKeyframeDown_Click;

            numTimestamp.ValueChanged += KeyframeEditor_ValueChanged;
            numOffsetX.ValueChanged += KeyframeEditor_ValueChanged;
            numOffsetY.ValueChanged += KeyframeEditor_ValueChanged;
            numScale.ValueChanged += KeyframeEditor_ValueChanged;
            comboAnchor.SelectedIndexChanged += KeyframeEditor_ValueChanged;
            comboInterpolation.SelectedIndexChanged += KeyframeEditor_ValueChanged; // Ensure this is wired

            butEditSave.Click += ButEditSave_Click;
            butEditCancel.Click += ButEditCancel_Click;

            btnSaveChanges.Click += BtnSaveChanges_Click;
            btnClose.Click += BtnClose_Click;

            // --- ADDED: Timer Tick Event ---
            if (this.components.Components.OfType<System.Windows.Forms.Timer>().FirstOrDefault(t => t.Site?.Name == "clipboardMonitorTimer") is System.Windows.Forms.Timer timer)
            {
                timer.Tick += ClipboardMonitorTimer_Tick;
                // Ensure timer is enabled from the start (should be set in designer, but double-check)
                // timer.Enabled = true; // Already set in designer properties
            }
            else Debug.WriteLine("WARNING: clipboardMonitorTimer component not found!");
            // --- END ADDED ---
        }

        // --- ADDED: Timer Tick Event Handler ---
        // In AnimationEditorForm.cs
        private void ClipboardMonitorTimer_Tick(object sender, EventArgs e)
        {
            bool enablePaste = false;
            try
            {
                // Check if the clipboard contains text data.
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();
                    // Basic check: Does it look like a JSON object? (Very lightweight)
                    if (!string.IsNullOrWhiteSpace(clipboardText))
                    {
                        string trimmedText = clipboardText.Trim();
                        if (trimmedText.StartsWith("{") && trimmedText.EndsWith("}"))
                        {
                            enablePaste = true; // Assume it *might* be valid timeline JSON
                        }
                    }
                }
            }
            catch (System.Runtime.InteropServices.ExternalException ex)
            {
                // Handle cases where clipboard access might fail (e.g., locked by another process)
                Debug.WriteLine($"Clipboard access error in timer: {ex.Message}");
                // Keep paste button disabled if we can't check
                enablePaste = false;
            }
            catch (Exception ex) // Catch any other unexpected errors
            {
                Debug.WriteLine($"Unexpected error in clipboard timer: {ex.Message}");
                enablePaste = false;
            }

            // Update button state only if it needs changing
            if (btnPasteTimeline.Enabled != enablePaste)
            {
                // Ensure update happens on UI thread if timer runs elsewhere (though Forms Timer usually ticks on UI thread)
                if (btnPasteTimeline.InvokeRequired)
                {
                    btnPasteTimeline.BeginInvoke(new Action(() => btnPasteTimeline.Enabled = enablePaste));
                }
                else
                {
                    btnPasteTimeline.Enabled = enablePaste;
                }
            }
        }


        // --- Data Population ---
        // MODIFIED: PopulateTimelineList to call RefreshTimelineListItem
        private void PopulateTimelineList()
        {
            _isLoading = true;
            listViewTimelines.BeginUpdate(); // Added for performance
            listViewTimelines.Items.Clear();
            Guid? previouslySelectedId = _selectedTimeline?.Id;
            ListViewItem itemToReselect = null;

            if (_profile.Animations != null)
            {
                foreach (var timeline in _profile.Animations.OrderBy(t => t.Name))
                {
                    var item = new ListViewItem(timeline.Name);
                    // Use helper to populate subitems
                    RefreshTimelineListItem(item, timeline); // Use helper
                    item.Tag = timeline;
                    listViewTimelines.Items.Add(item);
                    if (timeline.Id == previouslySelectedId) itemToReselect = item;
                }
            }

            if (itemToReselect != null)
            {
                itemToReselect.Selected = true;
                itemToReselect.EnsureVisible();
            }
            else
            {
                _selectedTimeline = null; // Explicitly clear selection if item not found
                ClearAndDisableKeyframeEditor();
                PopulateKeyframeList();
                UpdateTimelineControlsState(); // Use new function name
            }
            listViewTimelines.EndUpdate(); // Added for performance
            UpdateTimelineButtonStates();
            _isLoading = false;
        }

        private void PopulateKeyframeList()
        {
            _isLoading = true;
            listViewKeyframes.BeginUpdate();
            listViewKeyframes.Items.Clear();
            AnimationKeyframe previouslySelectedKeyframe = _selectedKeyframe; // Store reference before clear
            ListViewItem itemToReselect = null;

            if (_selectedTimeline?.Keyframes != null)
            {
                _selectedTimeline.SortKeyframes(); // Ensure data is sorted
                for (int i = 0; i < _selectedTimeline.Keyframes.Count; i++)
                {
                    var keyframe = _selectedTimeline.Keyframes[i];
                    var item = new ListViewItem(keyframe.Timestamp.ToString("F2"));
                    while (item.SubItems.Count < 6) item.SubItems.Add("");
                    item.SubItems[1].Text = keyframe.Transform.X.ToString("F1");
                    item.SubItems[2].Text = keyframe.Transform.Y.ToString("F1");
                    item.SubItems[3].Text = keyframe.Transform.Z.ToString("F2");
                    item.SubItems[4].Text = keyframe.Anchor.ToString();
                    item.SubItems[5].Text = keyframe.OutInterpolation.ToString();
                    item.Tag = keyframe; // Store the actual keyframe object
                    item.ToolTipText = $"Index: {i}"; // Add index as tooltip for debugging move logic
                    listViewKeyframes.Items.Add(item);

                    // Check if this is the item we need to reselect
                    if (keyframe == previouslySelectedKeyframe)
                    {
                        itemToReselect = item;
                    }
                }
            }

            // Reselect item after population
            if (itemToReselect != null)
            {
                itemToReselect.Selected = true;
                itemToReselect.EnsureVisible();
            }
            else
            {
                if (_selectedKeyframe != null) // If something *was* selected before
                {
                    ClearAndDisableKeyframeEditor(); // Reset editor state
                    _selectedKeyframe = null; // Ensure reference is cleared
                }
            }

            // Restore sorting if needed (or apply default sort) - Not needed if data is sorted before loop
            // listViewKeyframes.ListViewItemSorter = new ListViewItemComparerByTimestamp();
            // listViewKeyframes.Sort();
            // listViewKeyframes.ListViewItemSorter = null;

            listViewKeyframes.EndUpdate();
            UpdateKeyframeListControlsState(); // Update button enables etc.
            _isLoading = false;
        }

        private void LoadKeyframeToEditor()
        {
            if (_selectedKeyframe == null) { ClearAndDisableKeyframeEditor(); return; }
            _isLoading = true;
            try
            {
                // Use the Clone method to create the original state
                _originalKeyframeState = _selectedKeyframe.Clone();

                numTimestamp.Value = (decimal)Math.Max((float)numTimestamp.Minimum, Math.Min((float)numTimestamp.Maximum, _selectedKeyframe.Timestamp));
                numOffsetX.Value = (decimal)Math.Max((float)numOffsetX.Minimum, Math.Min((float)numOffsetX.Maximum, _selectedKeyframe.Transform.X));
                numOffsetY.Value = (decimal)Math.Max((float)numOffsetY.Minimum, Math.Min((float)numOffsetY.Maximum, _selectedKeyframe.Transform.Y));
                numScale.Value = (decimal)Math.Max((float)numScale.Minimum, Math.Min((float)numScale.Maximum, _selectedKeyframe.Transform.Z));

                // Load Anchor ComboBox
                string anchorString = _selectedKeyframe.Anchor.ToString();
                int anchorIndex = comboAnchor.FindStringExact(anchorString);
                comboAnchor.SelectedIndex = (anchorIndex >= 0) ? anchorIndex : (comboAnchor.Items.Count > 0 ? 0 : -1);


                // Load Interpolation ComboBox
                string interpString = _selectedKeyframe.OutInterpolation.ToString();
                int interpIndex = comboInterpolation.FindStringExact(interpString);
                comboInterpolation.SelectedIndex = (interpIndex >= 0) ? interpIndex : (comboInterpolation.Items.Count > 0 ? 0 : -1);


                groupBoxEditKeyframe.Enabled = true;
                MarkKeyframeClean();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading keyframe: {ex.Message}");
                ClearAndDisableKeyframeEditor();
            }
            finally
            {
                UpdateKeyframeListControlsState(); // Ensure button panel disabled when editor enabled
                _isLoading = false;
            }
        }

        private void ClearAndDisableKeyframeEditor()
        {
            _isLoading = true;
            _selectedKeyframe = null;
            _originalKeyframeState = null;

            numTimestamp.Value = numTimestamp.Minimum; // Often 0
            numOffsetX.Value = 0M;
            numOffsetY.Value = 0M;
            numScale.Value = 1.0M;

            // Reset Anchor ComboBox
            comboAnchor.SelectedIndex = comboAnchor.Items.IndexOf("Center");
            if (comboAnchor.SelectedIndex < 0 && comboAnchor.Items.Count > 0) comboAnchor.SelectedIndex = 0;

            // Reset Interpolation ComboBox
            comboInterpolation.SelectedIndex = comboInterpolation.Items.IndexOf("Linear");
            if (comboInterpolation.SelectedIndex < 0 && comboInterpolation.Items.Count > 0) comboInterpolation.SelectedIndex = 0;

            groupBoxEditKeyframe.Enabled = false;
            MarkKeyframeClean();
            _isLoading = false;
        }

        // --- UI State Updates ---
        private void UpdateTimelineButtonStates()
        {
            bool timelineSelected = listViewTimelines.SelectedItems.Count > 0;
            btnRemoveTimeline.Enabled = timelineSelected;
            btnRenameTimeline.Enabled = timelineSelected;

            // --- ADDED: Enable/Disable Copy Button ---
            btnCopyTimeline.Enabled = timelineSelected;
            // Paste button state is handled by the timer
            // --- END ADDED ---
        }

        private void UpdateKeyframeAreaState()
        {
            bool timelineSelected = listViewTimelines.SelectedItems.Count > 0;
            panelKeyframes.Enabled = timelineSelected;
            groupBoxEditKeyframe.Enabled = false; // Explicitly disable editor when area state changes
            if (!timelineSelected) { listViewKeyframes.Items.Clear(); ClearAndDisableKeyframeEditor(); }
            UpdateKeyframeListControlsState(); // Ensure button panel state is correct
        }

        // RENAMED and MODIFIED: UpdateTimelineControlsState (was UpdateTimelineHotkeyState)
        private void UpdateTimelineControlsState()
        {
            bool timelineSelected = _selectedTimeline != null;
            // Find the checkbox - be defensive
            var chkLoop = this.Controls.Find("chkTimelineLoop", true).FirstOrDefault() as CheckBox;

            // GroupBox and CheckBox enable state depends only on timeline selection
            groupBoxTimelineHotkey.Enabled = timelineSelected;
            if (chkLoop != null) chkLoop.Enabled = timelineSelected;


            if (timelineSelected)
            {
                // Load Hotkey
                txtTimelineHotkeyDisplay.Text = _selectedTimeline.Hotkey?.ToString() ?? "None";
                // Load Loop state
                if (chkLoop != null)
                {
                    // Temporarily disable events while setting value
                    _isLoading = true;
                    chkLoop.Checked = _selectedTimeline.Loop;
                    _isLoading = false;
                }
            }
            else
            {
                // Reset when no timeline is selected
                txtTimelineHotkeyDisplay.Text = "None";
                if (chkLoop != null) chkLoop.Checked = false;
            }

            // Handle hotkey listening mode if active
            if (_isListeningForTimelineHotkey) StopListeningForTimelineHotkey();
        }

        // --- Modified UI State Update for Keyframe Controls ---
        // Renamed from UpdateKeyframeButtonStates
        private void UpdateKeyframeListControlsState()
        {
            bool timelineSelected = _selectedTimeline != null;
            bool keyframeSelected = listViewKeyframes.SelectedItems.Count > 0;
            int selectedIndex = keyframeSelected ? listViewKeyframes.SelectedIndices[0] : -1;
            int keyframeCount = _selectedTimeline?.Keyframes?.Count ?? 0;

            // Enable individual buttons based on selection and position
            btnAddKeyframe.Enabled = timelineSelected;
            // Can remove if selected, >1 keyframes exist, AND it's not the time 0 keyframe
            btnRemoveKeyframe.Enabled = keyframeSelected && keyframeCount > 1 && (_selectedKeyframe?.Timestamp > 0.001f);

            // --- Enable/Disable Move Buttons ---
            // Enable Move Up if selected AND not the first item (index > 0) AND not the time 0 keyframe
            btnMoveKeyframeUp.Enabled = keyframeSelected && selectedIndex > 0 && (_selectedKeyframe?.Timestamp > 0.001f);
            // Enable Move Down if selected AND not the last item
            btnMoveKeyframeDown.Enabled = keyframeSelected && selectedIndex < (keyframeCount - 1);
            // --- END ---

            // Enable the button panel if a timeline is selected AND keyframe editor has no unsaved changes.
            panelKeyframeButtons.Enabled = timelineSelected && !_isKeyframeDirty;
        }

        private void UpdateKeyframeEditorSaveCancelState()
        { butEditSave.Enabled = _isKeyframeDirty; butEditCancel.Enabled = _isKeyframeDirty; }

        // --- Event Handlers ---

        private void BtnMoveKeyframeUp_Click(object sender, EventArgs e)
        {
            AdjustSelectedKeyframeTime(-1); // Pass direction (-1 for up/earlier)
        }

        // --- ADDED: BtnMoveKeyframeDown_Click Handler ---
        // In AnimationEditorForm.cs
        private void BtnMoveKeyframeDown_Click(object sender, EventArgs e)
        {
            AdjustSelectedKeyframeTime(1); // Pass direction (1 for down/later)
        }

        // --- ADDED: Helper Method to Adjust Keyframe Timestamp ---
        // In AnimationEditorForm.cs
        private void AdjustSelectedKeyframeTime(int direction)
        {
            if (_selectedTimeline == null || _selectedKeyframe == null || _isLoading || _isKeyframeDirty) return;
            if (Math.Abs(_selectedKeyframe.Timestamp) < 0.001f && direction < 0) return; // Cannot move time 0 earlier

            // Ensure keyframes are sorted before finding index
            _selectedTimeline.SortKeyframes();
            int currentIndex = _selectedTimeline.Keyframes.IndexOf(_selectedKeyframe);

            if (currentIndex < 0) return; // Should not happen if selected

            float currentTime = _selectedKeyframe.Timestamp;
            float newTime = currentTime;
            const float timeEpsilon = 0.001f; // Minimum difference
            const float nudgeAmount = 0.01f;  // Default nudge

            if (direction < 0 && currentIndex > 0) // Moving Up (Earlier)
            {
                float prevTime = _selectedTimeline.Keyframes[currentIndex - 1].Timestamp;
                // Calculate a time slightly after the previous, capped by current time - epsilon
                newTime = Math.Max(prevTime + timeEpsilon, currentTime - nudgeAmount);
                // Ensure we don't go below the minimum allowed time after 0
                if (newTime < timeEpsilon) newTime = timeEpsilon;
                // Prevent moving closer than epsilon to previous
                if (newTime - prevTime < timeEpsilon)
                {
                    // If nudging fails, try midpoint (only if significant gap exists)
                    if (currentTime - prevTime > 2 * timeEpsilon)
                    {
                        newTime = prevTime + (currentTime - prevTime) * 0.5f;
                    }
                    else
                    {
                        // Not enough space to move
                        System.Media.SystemSounds.Beep.Play(); // Indicate failure
                        return;
                    }
                }

            }
            else if (direction > 0 && currentIndex < _selectedTimeline.Keyframes.Count - 1) // Moving Down (Later)
            {
                float nextTime = _selectedTimeline.Keyframes[currentIndex + 1].Timestamp;
                // Calculate a time slightly before the next, bounded by current time + epsilon
                newTime = Math.Min(nextTime - timeEpsilon, currentTime + nudgeAmount);
                // Prevent moving closer than epsilon to next
                if (nextTime - newTime < timeEpsilon)
                {
                    // If nudging fails, try midpoint (only if significant gap exists)
                    if (nextTime - currentTime > 2 * timeEpsilon)
                    {
                        newTime = currentTime + (nextTime - currentTime) * 0.5f;
                    }
                    else
                    {
                        // Not enough space to move
                        System.Media.SystemSounds.Beep.Play(); // Indicate failure
                        return;
                    }
                }
            }
            else
            {
                return; // Cannot move further up/down
            }

            // Round to appropriate precision (e.g., 2 decimal places like the editor)
            newTime = (float)Math.Round(newTime, 2);

            // Final check for validity and actual change
            if (newTime > 0 && Math.Abs(newTime - currentTime) > timeEpsilon / 2.0f) // Check if it's a meaningful change
            {
                _selectedKeyframe.Timestamp = newTime;
                MarkDirty(); // Mark overall changes
                _selectedTimeline.SortKeyframes(); // Re-sort the underlying data
                PopulateKeyframeList(); // Refresh the list view (this will re-select the item)
                                        // Since PopulateKeyframeList reselects, LoadKeyframeToEditor will be called automatically
                                        // Need to update editor value directly if Populate doesn't trigger selection change somehow
                _isLoading = true; // Prevent value change event
                numTimestamp.Value = (decimal)Math.Max((float)numTimestamp.Minimum, Math.Min((float)numTimestamp.Maximum, _selectedKeyframe.Timestamp));
                _isLoading = false;

                Debug.WriteLine($"Moved keyframe {currentIndex} timestamp to {newTime:F2}");
            }
            else
            {
                System.Media.SystemSounds.Beep.Play(); // Indicate no move possible or too small change
                Debug.WriteLine($"Could not move keyframe {currentIndex}. New time {newTime:F2} too close or invalid.");
            }
        }

        private void ListViewTimelines_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            // Prompt user about unsaved keyframe changes before changing timeline selection
            if (!PromptSaveKeyframeChanges())
            {
                // User cancelled the save prompt, revert selection
                _isLoading = true;
                if (_selectedTimeline != null)
                {
                    // Try to find and re-select the previously selected item
                    var previousItem = listViewTimelines.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == _selectedTimeline);
                    if (previousItem != null) previousItem.Selected = true;
                }
                else
                {
                    listViewTimelines.SelectedItems.Clear(); // Clear selection if none was selected before
                }
                _isLoading = false;
                return; // Stop processing selection change
            }

            // Proceed with selection change if changes were saved or discarded
            if (listViewTimelines.SelectedItems.Count > 0)
            {
                _selectedTimeline = listViewTimelines.SelectedItems[0].Tag as AnimationTimeline;
                labelKeyframes.Text = $"Keyframes for '{_selectedTimeline?.Name ?? "..."}':"; // Update label
            }
            else
            {
                _selectedTimeline = null;
                labelKeyframes.Text = "Keyframes:"; // Reset label
            }

            UpdateKeyframeAreaState();       // Enable/disable the panel containing the keyframe list/buttons
            PopulateKeyframeList();         // Populate (or clear) the keyframe list for the selection
            UpdateTimelineControlsState();    // <<< Use RENAMED function to update hotkey AND loop checkbox state
            UpdateTimelineButtonStates();   // Update Add/Remove/Rename button states
        }

        private void ChkTimelineLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading || _selectedTimeline == null) return;

            // Find the checkbox - be defensive
            var chkLoop = sender as CheckBox;
            if (chkLoop == null) return;

            // Update the selected timeline object
            _selectedTimeline.Loop = chkLoop.Checked;
            MarkDirty(); // Mark overall changes as dirty

            // Refresh the corresponding list view item
            var item = listViewTimelines.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
            if (item != null)
            {
                RefreshTimelineListItem(item, _selectedTimeline);
            }

            Debug.WriteLine($"Timeline '{_selectedTimeline.Name}' Loop property set to: {_selectedTimeline.Loop}");
        }

        // NEW: Helper to refresh a timeline list view item
        private void RefreshTimelineListItem(ListViewItem item, AnimationTimeline timeline)
        {
            if (item == null || timeline == null) return;

            // Ensure enough subitems exist
            while (item.SubItems.Count < 3) item.SubItems.Add("");

            item.SubItems[0].Text = timeline.Name; // Same as item.Text
            item.SubItems[1].Text = timeline.Loop ? "Yes" : "No"; // Update Loop column
            item.SubItems[2].Text = timeline.GetDuration().ToString("F2");
        }

        private void ListViewKeyframes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoading) return; if (!PromptSaveKeyframeChanges()) {/* Revert selection */ _isLoading = true; if (_selectedKeyframe != null) { var pi = listViewKeyframes.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == _selectedKeyframe); if (pi != null) pi.Selected = true; } else { listViewKeyframes.SelectedItems.Clear(); } _isLoading = false; return; }
            if (listViewKeyframes.SelectedItems.Count > 0) { _selectedKeyframe = listViewKeyframes.SelectedItems[0].Tag as AnimationKeyframe; } else { _selectedKeyframe = null; }
            LoadKeyframeToEditor(); UpdateKeyframeListControlsState(); // Call renamed method
        }

        private void BtnAddTimeline_Click(object sender, EventArgs e)
        {
            if (!PromptSaveKeyframeChanges()) return; string newName = Interaction.InputBox("Enter name:", "New Timeline", $"Animation {_profile.Animations.Count + 1}"); if (string.IsNullOrWhiteSpace(newName)) return; if (_profile.Animations.Any(t => t.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))) { MessageBox.Show($"Timeline '{newName}' already exists.", "Duplicate"); return; }
            var newTimeline = new AnimationTimeline(newName); _profile.Animations.Add(newTimeline); MarkDirty(); PopulateTimelineList(); var newItem = listViewTimelines.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == newTimeline); if (newItem != null) { newItem.Selected = true; newItem.EnsureVisible(); listViewTimelines.Focus(); }
        }

        private void BtnRemoveTimeline_Click(object sender, EventArgs e)
        { if (_selectedTimeline == null) return; if (!PromptSaveKeyframeChanges()) return; if (MessageBox.Show($"Delete timeline '{_selectedTimeline.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { ClearAndDisableKeyframeEditor(); listViewKeyframes.Items.Clear(); UpdateTimelineControlsState(); _profile.Animations.Remove(_selectedTimeline); _selectedTimeline = null; MarkDirty(); PopulateTimelineList(); } }

        private void BtnRenameTimeline_Click(object sender, EventArgs e)
        { if (_selectedTimeline == null) return; if (!PromptSaveKeyframeChanges()) return; string currentName = _selectedTimeline.Name; string newName = Interaction.InputBox("Enter new name:", "Rename", currentName); if (string.IsNullOrWhiteSpace(newName) || newName == currentName) return; if (_profile.Animations.Any(t => t.Id != _selectedTimeline.Id && t.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))) { MessageBox.Show($"Timeline '{newName}' already exists.", "Duplicate"); return; } _selectedTimeline.Name = newName; MarkDirty(); PopulateTimelineList(); }

        private void BtnAddKeyframe_Click(object sender, EventArgs e)
        {
            if (_selectedTimeline == null) return; if (!PromptSaveKeyframeChanges()) return; float newTime = 0.0f; AnimationKeyframe lastKey = _selectedTimeline.Keyframes.OrderByDescending(k => k.Timestamp).FirstOrDefault(); if (lastKey != null) { newTime = lastKey.Timestamp + 0.1f; } // Add slightly after last
            // Ensure new time isn't conflicting before adding
            if (_selectedTimeline.Keyframes.Any(kf => Math.Abs(kf.Timestamp - newTime) < 0.001f)) { newTime += 0.01f; } // Add slightly more time if exact conflict
            var newKey = new AnimationKeyframe(newTime, lastKey?.Transform.X ?? 0f, lastKey?.Transform.Y ?? 0f, lastKey?.Transform.Z ?? 1f, lastKey?.Anchor ?? AnchorPoint.Center);
            _selectedTimeline.Keyframes.Add(newKey); _selectedTimeline.SortKeyframes(); MarkDirty(); PopulateKeyframeList(); var newItem = listViewKeyframes.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == newKey); if (newItem != null) { newItem.Selected = true; newItem.EnsureVisible(); listViewKeyframes.Focus(); LoadKeyframeToEditor(); }
        }

        private void BtnRemoveKeyframe_Click(object sender, EventArgs e)
        {
            if (_selectedTimeline == null || _selectedKeyframe == null) return;
            // Stricter check for time 0 keyframe
            if (_selectedTimeline.Keyframes.Count <= 1) { MessageBox.Show("Cannot remove the only keyframe.", "Action Denied"); return; }
            if (Math.Abs(_selectedKeyframe.Timestamp - 0.0f) < 0.001f) { MessageBox.Show("Cannot remove the keyframe at time 0.0.", "Action Denied"); return; }
            if (MessageBox.Show($"Delete keyframe at {_selectedKeyframe.Timestamp:F2}s?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _selectedTimeline.Keyframes.Remove(_selectedKeyframe); _selectedKeyframe = null; MarkDirty(); ClearAndDisableKeyframeEditor(); PopulateKeyframeList(); }
        }

        private void MoveSelectedKeyframe(int dir) { MessageBox.Show("Move Up/Down disabled."); }

        private void KeyframeEditor_ValueChanged(object sender, EventArgs e) { if (_isLoading) return; MarkKeyframeDirty(); }

        private void ButEditSave_Click(object sender, EventArgs e)
        {
            if (_selectedKeyframe == null) return;
            if (ApplyEditorToKeyframe())
            {
                _selectedTimeline?.SortKeyframes();
                MarkDirty();
                MarkKeyframeClean();
                // Refresh item display WITHOUT repopulating entire list unnecessarily
                var item = listViewKeyframes.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
                if (item != null) RefreshKeyframeListItem(item);
                // Now re-sort the ListView items based on the underlying sorted data tags
                listViewKeyframes.ListViewItemSorter = new ListViewItemComparerByTimestamp();
                listViewKeyframes.Sort();
                listViewKeyframes.ListViewItemSorter = null; // Remove sorter
                                                             // Re-select item after sort
                item?.EnsureVisible();
                if (item != null) item.Selected = true; // Re-select


                UpdateKeyframeEditorSaveCancelState();
                listViewKeyframes.Focus();
            }
            else { MessageBox.Show("Failed to apply changes. Please check values.", "Error"); }
        }

        // Helper Class for sorting ListViewItems by timestamp in Tag
        private class ListViewItemComparerByTimestamp : System.Collections.IComparer {
            public int Compare(object x, object y)
            {
                AnimationKeyframe kfX = (x as ListViewItem)?.Tag as AnimationKeyframe;
                AnimationKeyframe kfY = (y as ListViewItem)?.Tag as AnimationKeyframe;
                if (kfX == null && kfY == null) return 0;
                if (kfX == null) return -1;
                if (kfY == null) return 1;
                return kfX.Timestamp.CompareTo(kfY.Timestamp);
            }
        }


        private void ButEditCancel_Click(object sender, EventArgs e)
        {
            if (_selectedKeyframe == null || _originalKeyframeState == null) return;
            RevertKeyframeEditor();
            MarkKeyframeClean();
            // Keep editor open, allow user to make different changes or select different item
            // UpdateKeyframeListControlsState(); // Ensure button panel is still disabled
            // UpdateKeyframeEditorSaveCancelState(); // Done by MarkKeyframeClean
        }

        private void BtnSetTimelineHotkey_Click(object sender, EventArgs e)
        {
            if (_selectedTimeline == null) return;
            if (_isListeningForTimelineHotkey) { StopListeningForTimelineHotkey(); }
            else
            {
                _isListeningForTimelineHotkey = true;
                txtTimelineHotkeyDisplay.Text = "[Press Keys...]";
                btnSetTimelineHotkey.Text = "Listening...";
                // Disable other controls to prevent interference
                SetInputControlsEnabled(false); // Call helper
                this.Activate(); // Try to ensure form has focus
                this.Focus();
            }
        }

        private void BtnClearTimelineHotkey_Click(object sender, EventArgs e)
        {
            if (_selectedTimeline == null) return;
            if (_isListeningForTimelineHotkey) StopListeningForTimelineHotkey();
            _selectedTimeline.Hotkey = new HotkeyConfig();
            UpdateTimelineControlsState(); MarkDirty();
        }

        private void AnimationEditorForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isListeningForTimelineHotkey)
            {
                e.Handled = true; e.SuppressKeyPress = true;
                if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin || e.KeyCode == Keys.Apps) return;
                if (e.KeyCode == Keys.Escape) { StopListeningForTimelineHotkey(true); return; }
                if (!e.Control && !e.Alt && !e.Shift) { MessageBox.Show("Hotkey must include Ctrl, Alt, or Shift.", "Invalid Hotkey"); return; }
                var capturedHotkey = new HotkeyConfig { Key = e.KeyCode, Control = e.Control, Alt = e.Alt, Shift = e.Shift };
                if (IsTimelineHotkeyValid(capturedHotkey)) { _selectedTimeline.Hotkey = capturedHotkey; MarkDirty(); StopListeningForTimelineHotkey(false); UpdateTimelineControlsState(); }
                else { StopListeningForTimelineHotkey(true); }
            }
        }

        private void StopListeningForTimelineHotkey(bool revertDisplay = false)
        {
            _isListeningForTimelineHotkey = false;
            btnSetTimelineHotkey.Text = "Set...";
            if (revertDisplay) UpdateTimelineControlsState();
            SetInputControlsEnabled(true); // Re-enable controls
        }

        // Added Helper to Enable/Disable Input Controls during hotkey listen
        private void SetInputControlsEnabled(bool enabled)
        {
            // Ensure running on UI thread
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => SetInputControlsEnabled(enabled))); return; }

            try
            {
                listViewTimelines.Enabled = enabled;
                // Only enable keyframe panel if a timeline is selected AND we are enabling controls
                panelKeyframes.Enabled = enabled && (_selectedTimeline != null);
                // Only enable editor if a keyframe is selected AND we are enabling controls
                groupBoxEditKeyframe.Enabled = enabled && (_selectedKeyframe != null);

                // Enable/disable buttons (consider their specific logic too)
                panelTimelineButtons.Enabled = enabled;
                UpdateTimelineButtonStates(); // Re-apply specific enables

                // Keyframe buttons are inside panelKeyframes, handled above

                btnClose.Enabled = enabled;
                btnSaveChanges.Enabled = enabled && _isDirty;

                // Enable hotkey clear button only when enabling AND timeline selected
                var clearBtn = this.Controls.Find("btnClearTimelineHotkey", true).FirstOrDefault() as Button;
                if (clearBtn != null) clearBtn.Enabled = enabled && (_selectedTimeline != null);
                // Set button itself is handled by its own logic (changes text)
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SetInputControlsEnabled: {ex.Message}");
            }
        }


        private bool IsTimelineHotkeyValid(HotkeyConfig hotkeyToCheck)
        {
            if (hotkeyToCheck == null || hotkeyToCheck.Key == Keys.None) return true;
            if (_appSettings.GlobalHideShowHotkey != null && _appSettings.GlobalHideShowHotkey.Key != Keys.None) { if (HotkeysMatch(_appSettings.GlobalHideShowHotkey, hotkeyToCheck)) { MessageBox.Show($"Hotkey '{hotkeyToCheck}' conflicts with Global.", "Conflict"); return false; } }
            if (_profile.Animations != null) { foreach (var otherTimeline in _profile.Animations) { if (otherTimeline == _selectedTimeline) continue; if (otherTimeline.Hotkey != null && otherTimeline.Hotkey.Key != Keys.None) { if (HotkeysMatch(otherTimeline.Hotkey, hotkeyToCheck)) { MessageBox.Show($"Hotkey '{hotkeyToCheck}' conflicts with '{otherTimeline.Name}'.", "Conflict"); return false; } } } }
            return true;
        }

        private bool HotkeysMatch(HotkeyConfig hk1, HotkeyConfig hk2)
        { if (hk1 == null || hk2 == null) return false; return hk1.Key == hk2.Key && hk1.Control == hk2.Control && hk1.Alt == hk2.Alt && hk1.Shift == hk2.Shift; }

        private void BtnSaveChanges_Click(object sender, EventArgs e)
        {
            if (!PromptSaveKeyframeChanges()) return;
            try
            {
                if (_profile?.Animations != null) { foreach (var tl in _profile.Animations) { tl.SortKeyframes(); } }
                SettingsManager.SaveSettings(_appSettings);
                MarkClean();
                MessageBox.Show("Animation changes saved.", "Saved");
                this.DialogResult = DialogResult.OK; // Signal success
            }
            catch (Exception ex) { MessageBox.Show($"Error saving settings:\n{ex.Message}", "Error"); }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        { this.Close(); } // FormClosing handles dirty check

        private void MarkDirty() { _isDirty = true; btnSaveChanges.Enabled = true; this.Text = "Animation Editor*"; }
        private void MarkClean() { _isDirty = false; btnSaveChanges.Enabled = false; this.Text = "Animation Editor"; }
        // --- Modified Helper Methods for Dirty State ---
        private void MarkKeyframeDirty()
        {
            if (_isLoading) return; // Don't mark dirty if loading values
            if (!_isKeyframeDirty) // Only update UI if state actually changes
            {
                _isKeyframeDirty = true;
                UpdateKeyframeEditorSaveCancelState(); // Update Save/Cancel buttons
                UpdateKeyframeListControlsState();     // <<< ADDED: Update list buttons (disable panel)
                groupBoxEditKeyframe.Text = "Edit Selected Keyframe*";
            }
        }
        private void MarkKeyframeClean()
        {
            // Always reset state regardless of previous state
            _isKeyframeDirty = false;
            UpdateKeyframeEditorSaveCancelState(); // Update Save/Cancel buttons
            UpdateKeyframeListControlsState();     // <<< ADDED: Update list buttons (enable panel)
            groupBoxEditKeyframe.Text = "Edit Selected Keyframe";
        }

        // --- ADDED: Copy Timeline Button Event Handler ---
        // In AnimationEditorForm.cs
        private void BtnCopyTimeline_Click(object sender, EventArgs e)
        {
            if (_selectedTimeline == null)
            {
                MessageBox.Show("Please select a timeline to copy.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Create a clean copy for serialization, perhaps clearing runtime state if any existed
                // For now, just clone or use the existing object is likely fine
                AnimationTimeline timelineToCopy = _selectedTimeline; // Or a clone if needed

                // Serialize to indented JSON
                string jsonString = JsonConvert.SerializeObject(timelineToCopy, Formatting.Indented);

                // Set to clipboard
                Clipboard.SetText(jsonString);

                // Optional: Feedback to user
                // statusStripLabel.Text = $"Timeline '{timelineToCopy.Name}' copied to clipboard.";
                Debug.WriteLine($"Timeline '{timelineToCopy.Name}' copied to clipboard.");
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"JSON Serialization Error copying timeline: {jsonEx.Message}");
                MessageBox.Show($"Failed to copy timeline due to a serialization error:\n{jsonEx.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Error copying timeline: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred while copying the timeline:\n{ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- ADDED: Paste Timeline Button Event Handler ---
        // In AnimationEditorForm.cs
        private void BtnPasteTimeline_Click(object sender, EventArgs e)
        {
            if (!PromptSaveKeyframeChanges()) return; // Check for unsaved keyframe edits

            string clipboardText = string.Empty;
            try
            {
                if (Clipboard.ContainsText())
                {
                    clipboardText = Clipboard.GetText();
                }
                else
                {
                    MessageBox.Show("Clipboard does not contain text data.", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    MessageBox.Show("Clipboard text is empty.", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AnimationTimeline pastedTimeline = null;
                // Deserialize (wrapped in try-catch)
                try
                {
                    pastedTimeline = JsonConvert.DeserializeObject<AnimationTimeline>(clipboardText);
                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"JSON Deserialization Error pasting timeline: {jsonEx.Message}");
                    MessageBox.Show($"Failed to paste timeline. Clipboard data is not valid JSON or does not match the expected format.\n\nDetails: {jsonEx.Message}", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Deep Validation
                string validationError;
                if (!ValidatePastedTimeline(pastedTimeline, out validationError))
                {
                    MessageBox.Show($"Failed to paste timeline. Invalid data found:\n\n{validationError}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // --- Integration ---
                // 1. Generate New ID
                pastedTimeline.Id = Guid.NewGuid();

                // 2. Clear Hotkey
                pastedTimeline.Hotkey = new HotkeyConfig(); // Reset to Keys.None

                // 3. Handle Name Conflict (Append " (Copy)" strategy)
                string originalName = pastedTimeline.Name;
                int copyCount = 1;
                while (_profile.Animations.Any(t => t.Name.Equals(pastedTimeline.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    pastedTimeline.Name = $"{originalName} (Copy {copyCount++})";
                }

                // 4. Add to Profile
                _profile.Animations.Add(pastedTimeline);
                MarkDirty(); // Mark overall changes

                // 5. Refresh UI
                PopulateTimelineList();
                // Select the newly added item
                var newItem = listViewTimelines.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == pastedTimeline);
                if (newItem != null)
                {
                    newItem.Selected = true;
                    newItem.EnsureVisible();
                    listViewTimelines.Focus();
                }

                Debug.WriteLine($"Timeline '{pastedTimeline.Name}' (original: '{originalName}') pasted successfully.");

            }
            catch (System.Runtime.InteropServices.ExternalException clipEx)
            {
                Debug.WriteLine($"Clipboard access error on paste: {clipEx.Message}");
                MessageBox.Show($"Could not access clipboard data. It might be in use by another application.\n\nDetails: {clipEx.Message}", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error pasting timeline: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred while pasting:\n{ex.Message}", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- ADDED: Validation Helper Method ---
        // In AnimationEditorForm.cs
        private bool ValidatePastedTimeline(AnimationTimeline timeline, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (timeline == null)
            {
                errorMessage = "Clipboard data could not be deserialized into a timeline.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(timeline.Name))
            {
                // Allow pasting unnamed timelines, assign default later if needed.
                // Or enforce name here:
                // errorMessage = "Timeline name cannot be empty.";
                // return false;
                timeline.Name = "Pasted Timeline"; // Assign default if blank
            }

            if (timeline.Keyframes == null)
            {
                errorMessage = "Timeline keyframes data is missing.";
                return false;
            }

            foreach (var kf in timeline.Keyframes)
            {
                if (kf == null)
                {
                    errorMessage = "Timeline contains invalid (null) keyframe data.";
                    return false;
                }
                if (kf.Timestamp < 0)
                {
                    errorMessage = $"Keyframe timestamp cannot be negative ({kf.Timestamp:F2}).";
                    return false;
                }
                if (kf.Transform.Z <= 0) // Scale (Z component) must be positive
                {
                    errorMessage = $"Keyframe scale must be positive ({kf.Transform.Z:F2}).";
                    return false;
                }
                if (!Enum.IsDefined(typeof(AnchorPoint), kf.Anchor))
                {
                    errorMessage = $"Keyframe contains an invalid anchor point value ({kf.Anchor}).";
                    return false;
                }
                // Could add more checks for Offset range if desired
            }

            // Validate Hotkey structure (even if we clear it later, ensure it was valid)
            if (timeline.Hotkey == null)
            {
                // If null, assign a default empty one. Deserializer might do this anyway.
                timeline.Hotkey = new HotkeyConfig();
            }
            // Check if the Key enum value is valid
            if (!Enum.IsDefined(typeof(Keys), timeline.Hotkey.Key))
            {
                // If key is invalid, reset the hotkey to None safely
                errorMessage = $"Pasted timeline contained an invalid hotkey key value ({timeline.Hotkey.Key}). Hotkey will be cleared.";
                timeline.Hotkey = new HotkeyConfig(); // Clear it now
                                                      // We don't return false here, just warn and clear.
                                                      // MessageBox.Show(errorMessage, "Paste Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Optional immediate warning
                errorMessage = string.Empty; // Clear error message as we handled it
            }


            // If we reached here, basic validation passed
            return true;
        }

        private bool PromptSaveKeyframeChanges() { if (!_isKeyframeDirty || _selectedKeyframe == null) return true; var r = MessageBox.Show($"Save changes to keyframe at {_selectedKeyframe.Timestamp:F2}s?", "Unsaved", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning); if (r == DialogResult.Yes) { ButEditSave_Click(null, EventArgs.Empty); return !_isKeyframeDirty; } else if (r == DialogResult.No) { RevertKeyframeEditor(); MarkKeyframeClean(); return true; } else { return false; } }
        private bool PromptSaveChangesIfDirty() { if (!PromptSaveKeyframeChanges()) return false; if (!_isDirty) return true; var r = MessageBox.Show("Save changes to animations?", "Unsaved", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning); if (r == DialogResult.Yes) { BtnSaveChanges_Click(null, EventArgs.Empty); return !_isDirty; } else if (r == DialogResult.No) { return true; } else { return false; } }

        private bool ApplyEditorToKeyframe()
        {
            if (_selectedKeyframe == null) return false;
            try
            {
                float nts = (float)numTimestamp.Value;
                // Check for timestamp conflict only if timestamp has changed
                if (Math.Abs(nts - _selectedKeyframe.Timestamp) > 0.001f) // Use original state for checks? No, check against current state.
                {
                    if (nts < 0) // Cannot be negative
                    {
                        MessageBox.Show($"Timestamp cannot be negative.", "Invalid Timestamp");
                        numTimestamp.Focus();
                        return false;
                    }
                    // Prevent changing *to* 0 if it wasn't originally 0
                    if (Math.Abs(nts - 0.0f) < 0.001f && Math.Abs(_originalKeyframeState?.Timestamp ?? -1f) > 0.001f)
                    {
                        MessageBox.Show($"Cannot change timestamp to 0.", "Invalid Timestamp");
                        numTimestamp.Focus();
                        return false;
                    }
                    // Prevent changing *from* 0 if it was originally 0
                    if (Math.Abs(_originalKeyframeState?.Timestamp ?? -1f) < 0.001f && nts > 0.001f)
                    {
                        MessageBox.Show($"Cannot change timestamp away from 0 for the initial keyframe.", "Invalid Timestamp");
                        numTimestamp.Focus();
                        return false;
                    }
                    // Check conflict with *other* keyframes
                    if (_selectedTimeline.Keyframes.Any(k => k != _selectedKeyframe && Math.Abs(k.Timestamp - nts) < 0.001f))
                    {
                        MessageBox.Show($"Time {nts:F2}s conflicts with another keyframe.", "Timestamp Conflict");
                        numTimestamp.Focus();
                        return false;
                    }
                }


                _selectedKeyframe.Timestamp = nts;
                _selectedKeyframe.Transform = new Vector3((float)numOffsetX.Value, (float)numOffsetY.Value, (float)numScale.Value);

                // Save Anchor
                if (comboAnchor.SelectedItem != null && Enum.TryParse<AnchorPoint>(comboAnchor.SelectedItem.ToString(), out var anchor))
                {
                    _selectedKeyframe.Anchor = anchor;
                }
                else { _selectedKeyframe.Anchor = AnchorPoint.Center; } // Default fallback

                // Save Interpolation
                if (comboInterpolation.SelectedItem != null && Enum.TryParse<InterpolationType>(comboInterpolation.SelectedItem.ToString(), out var interp))
                {
                    _selectedKeyframe.OutInterpolation = interp;
                }
                else { _selectedKeyframe.OutInterpolation = InterpolationType.Linear; } // Default fallback

                // Update original state to reflect saved changes
                _originalKeyframeState = _selectedKeyframe.Clone();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error ApplyEditor: {ex.Message}");
                MessageBox.Show($"Error applying keyframe changes: {ex.Message}");
                return false;
            }
        }

        private void RevertKeyframeEditor()
        {
            if (_originalKeyframeState == null) return; // Nothing to revert to
            _isLoading = true;
            try
            {
                // Revert basic properties
                numTimestamp.Value = (decimal)Math.Max((float)numTimestamp.Minimum, Math.Min((float)numTimestamp.Maximum, _originalKeyframeState.Timestamp));
                numOffsetX.Value = (decimal)Math.Max((float)numOffsetX.Minimum, Math.Min((float)numOffsetX.Maximum, _originalKeyframeState.Transform.X));
                numOffsetY.Value = (decimal)Math.Max((float)numOffsetY.Minimum, Math.Min((float)numOffsetY.Maximum, _originalKeyframeState.Transform.Y));
                numScale.Value = (decimal)Math.Max((float)numScale.Minimum, Math.Min((float)numScale.Maximum, _originalKeyframeState.Transform.Z));

                // Revert Anchor ComboBox
                comboAnchor.SelectedItem = _originalKeyframeState.Anchor.ToString();
                if (comboAnchor.SelectedIndex < 0 && comboAnchor.Items.Count > 0) comboAnchor.SelectedIndex = 0; // Fallback if string not found

                // Revert Interpolation ComboBox
                comboInterpolation.SelectedItem = _originalKeyframeState.OutInterpolation.ToString();
                if (comboInterpolation.SelectedIndex < 0 && comboInterpolation.Items.Count > 0) comboInterpolation.SelectedIndex = 0; // Fallback if string not found
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void RefreshKeyframeListItem(ListViewItem item)
        {
            if (item?.Tag is AnimationKeyframe kf)
            {
                _isLoading = true; // Prevent selection change events if any
                                   // Use BeginUpdate/EndUpdate for potential performance gain if refreshing many items (though usually just one)
                                   // item.ListView?.BeginUpdate();
                item.Text = kf.Timestamp.ToString("F2");
                while (item.SubItems.Count < 6) item.SubItems.Add(""); // Ensure enough subitems (now 6)
                item.SubItems[1].Text = kf.Transform.X.ToString("F1");
                item.SubItems[2].Text = kf.Transform.Y.ToString("F1");
                item.SubItems[3].Text = kf.Transform.Z.ToString("F2");
                item.SubItems[4].Text = kf.Anchor.ToString();
                item.SubItems[5].Text = kf.OutInterpolation.ToString(); // Updated Interpolation Display
                                                                        // item.ListView?.EndUpdate();
                _isLoading = false;
            }
        }

    }
}
// <<< END MODIFICATION: AnimationEditorForm.cs (Full File)
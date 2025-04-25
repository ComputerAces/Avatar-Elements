using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avatar_Elements.Data; // Assuming data structures are here
using System.Diagnostics; // For Debug.WriteLine

namespace Avatar_Elements {
    public partial class LightEditorForm : Form {
        // --- Member Variables ---
        private readonly AppSettings _appSettings;
        private readonly AvatarProfile _currentProfile; // Can be null if editing global only
        private LightSource _currentlyEditingLight = null;
        private ListView _activeListView = null; // Track which listview has selection
        private bool _isDirty = false; // Track unsaved changes in the editor
        private bool _loadingData = false; // Prevent recursive events while loading editor
        private LightSource _originalEditingLightState = null;

        /// <summary>
        /// Event raised when light settings have potentially changed and need saving/refreshing.
        /// </summary>
        public event EventHandler SettingsChanged;

        // --- Constructor ---
        public LightEditorForm(AppSettings settings, AvatarProfile profile = null)
        {
            InitializeComponent();

            _appSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            _currentProfile = profile; // Profile can be null

            // Basic Input Validation Setup (more could be added)
            SetupNumericUpDownValidation();

            WireUpEvents();
        }

        // --- Form Load ---
        private void LightEditorForm_Load(object sender, EventArgs e)
        {
            _loadingData = true; // Prevent events during initial load

            // Populate ListViews
            PopulateListView(listViewGlobalLights, _appSettings.GlobalLights);
            if (_currentProfile != null)
            {
                // Profile was provided - populate its lights and enable tab
                PopulateListView(listViewProfileLights, _currentProfile.ProfileLights);
                tabPageProfile.Text = $"Profile Lights ({_currentProfile.ProfileName})"; // Set tab text
                tabPageProfile.Enabled = true; // Enable tab
                                               // Enable profile-specific buttons
                btnAddProfileLight.Enabled = true;
                UpdateListViewButtonStates(listViewProfileLights, btnEditProfileLight, btnRemoveProfileLight);

            }
            else
            {
                // No profile provided - disable profile tab
                listViewProfileLights.Items.Clear();
                tabPageProfile.Text = "Profile Lights (None)"; // Update text
                tabPageProfile.Enabled = false; // Disable tab
                // Disable profile-specific buttons
                btnAddProfileLight.Enabled = false;
                btnEditProfileLight.Enabled = false;
                btnRemoveProfileLight.Enabled = false;
            }

            // Set initial state
            ClearAndDisableEditor();
            UpdateListViewButtonStates(listViewGlobalLights, btnEditGlobal, btnRemoveGlobal);
            // Profile button states already handled above based on _currentProfile presence

            // Select default combo item
            if (cmbLightType.Items.Count > 0)
                cmbLightType.SelectedIndex = 0; // Default to Point

            _loadingData = false;
            _isDirty = false; // No changes initially
            btnSaveChanges.Enabled = false;
        }

        // --- Added Move Button Handlers ---

        private void BtnMoveGlobalUp_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(listViewGlobalLights, _appSettings.GlobalLights, -1);
        }

        private void BtnMoveGlobalDown_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(listViewGlobalLights, _appSettings.GlobalLights, 1);
        }

        private void BtnMoveProfileUp_Click(object sender, EventArgs e)
        {
            if (_currentProfile != null)
                MoveSelectedItem(listViewProfileLights, _currentProfile.ProfileLights, -1);
        }

        private void BtnMoveProfileDown_Click(object sender, EventArgs e)
        {
            if (_currentProfile != null)
                MoveSelectedItem(listViewProfileLights, _currentProfile.ProfileLights, 1);
        }

        // --- Added Helper Method for Moving Items ---
        private void MoveSelectedItem(ListView listView, List<LightSource> lightList, int direction)
        {
            if (listView.SelectedItems.Count == 0) return;
            if (lightList == null) return;

            int selectedIndex = listView.SelectedIndices[0];
            ListViewItem selectedItem = listView.SelectedItems[0];
            LightSource lightToMove = selectedItem.Tag as LightSource;

            if (lightToMove == null) return; // Should not happen

            int newIndex = selectedIndex + direction;

            // Check bounds
            if (newIndex < 0 || newIndex >= lightList.Count) return;

            // Move in the underlying list
            lightList.RemoveAt(selectedIndex);
            lightList.Insert(newIndex, lightToMove);

            // Move in the ListView
            listView.Items.RemoveAt(selectedIndex);
            listView.Items.Insert(newIndex, selectedItem);

            // Re-select the moved item
            selectedItem.Selected = true;
            selectedItem.EnsureVisible();
            listView.Focus();

            // Mark dirty and notify parent
            MarkDirty();
            RaiseSettingsChanged();

            // Update button states (selection hasn't changed, but position has)
            // Determine which set of buttons to update based on the list view
            if (listView == listViewGlobalLights)
                UpdateListViewButtonStates(listViewGlobalLights, btnEditGlobal, btnRemoveGlobal, btnMoveGlobalUp, btnMoveGlobalDown);
            else if (listView == listViewProfileLights)
                UpdateListViewButtonStates(listViewProfileLights, btnEditProfileLight, btnRemoveProfileLight, btnMoveProfileUp, btnMoveProfileDown);
        }

        // --- Event Wiring ---
        // In LightEditorForm.cs
        private void WireUpEvents()
        {
            this.Load += LightEditorForm_Load;
            this.FormClosing += LightEditorForm_FormClosing;

            // ListViews
            listViewGlobalLights.SelectedIndexChanged += ListViewGlobalLights_SelectedIndexChanged;
            listViewProfileLights.SelectedIndexChanged += ListViewProfileLights_SelectedIndexChanged;
            listViewGlobalLights.ItemCheck += ListView_ItemCheck;
            listViewProfileLights.ItemCheck += ListView_ItemCheck;

            // Tab Control
            tabControlLights.SelectedIndexChanged += TabControlLights_SelectedIndexChanged;

            // List Buttons (Add/Edit/Remove)
            btnAddGlobal.Click += BtnAddGlobal_Click;
            btnEditGlobal.Click += BtnEditGlobal_Click;
            btnRemoveGlobal.Click += BtnRemoveGlobal_Click;
            btnAddProfileLight.Click += BtnAddProfileLight_Click;
            btnEditProfileLight.Click += BtnEditProfileLight_Click;
            btnRemoveProfileLight.Click += BtnRemoveProfileLight_Click;

            // Move Buttons
            btnMoveGlobalUp.Click += BtnMoveGlobalUp_Click;
            btnMoveGlobalDown.Click += BtnMoveGlobalDown_Click;
            btnMoveProfileUp.Click += BtnMoveProfileUp_Click;
            btnMoveProfileDown.Click += BtnMoveProfileDown_Click;

            // Editor Controls Wiring
            cmbLightType.Items.Clear(); // Clear designer items
            // >>> Ensure this uses the updated LightType enum including Tint <<<
            cmbLightType.Items.AddRange(Enum.GetNames(typeof(LightType)));
            // >>> Select default index AFTER populating <<<
            if (cmbLightType.Items.Count > 0) cmbLightType.SelectedIndex = 1; // Default to Point (index 1)

            cmbLightType.SelectedIndexChanged += EditorControl_ValueChanged;
            chkEnabled.CheckedChanged += EditorControl_ValueChanged;
            btnChooseColor.Click += BtnChooseColor_Click;
            panelColorPreview.Click += BtnChooseColor_Click;
            btnNormalizeDir.Click += BtnNormalizeDir_Click;
            numSpotAngle.ValueChanged += EditorControl_ValueChanged;

            // Hook up NumericUpDowns
            foreach (Control ctl in groupBoxEditLight.Controls)
                if (ctl is NumericUpDown num) num.ValueChanged += EditorControl_ValueChanged;
            foreach (Control ctl in panelPoint.Controls)
                if (ctl is NumericUpDown num) num.ValueChanged += EditorControl_ValueChanged;
            foreach (Control ctl in panelDirection.Controls)
                if (ctl is NumericUpDown num) num.ValueChanged += EditorControl_ValueChanged;

            // Bottom Buttons
            btnSaveChanges.Click += BtnSaveChanges_Click;
            btnClose.Click += BtnClose_Click;
        }

        private void SetupNumericUpDownValidation()
        {
            // Ensure consistent behavior for potentially invalid user input
            Action<object, CancelEventArgs> validateDecimal = (sender, e) => {
                NumericUpDown num = sender as NumericUpDown;
                if (num == null) return;
                // Very basic validation: Check if text can be parsed to decimal within range.
                // More robust validation might be needed depending on culture settings.
                if (!decimal.TryParse(num.Text, out decimal val) || val < num.Minimum || val > num.Maximum)
                {
                    // Optionally provide feedback or revert to previous valid value
                    // For simplicity, we might rely on the control's built-in clamping
                    Debug.WriteLine($"Validation issue detected for {num.Name}, Text: {num.Text}");
                }
            };

            // Hook up validation (optional, NumericUpDown handles basic range)
            // numPosX.Validating += new CancelEventHandler(validateDecimal);
            // numPosY.Validating += new CancelEventHandler(validateDecimal);
            // ... etc for all relevant NumericUpDowns ...
        }


        // In LightEditorForm.cs
        private void PopulateListView(ListView listView, List<LightSource> lights)
        {
            listView.Items.Clear();
            if (lights == null) return;

            foreach (var light in lights)
            {
                ListViewItem item = new ListViewItem("");
                item.Checked = light.IsEnabled;
                item.SubItems.Add(light.Type.ToString());

                // Details Column
                string details = "";
                if (light.Type == LightType.Ambient)
                {
                    details = "(Ambient)";
                }
                else if (light.Type == LightType.Point)
                {
                    details = $"Pos: {light.Position.ToString()}";
                    if (light.SpotCutoffAngle < 90) details += $" Spot({light.SpotCutoffAngle:F1}°, {light.SpotExponent:F1})";
                }
                else if (light.Type == LightType.Directional)
                {
                    details = $"Dir: {light.Direction.ToString()}";
                }
                else if (light.Type == LightType.Tint) // <<< ADDED Tint Check
                {
                    details = "(Tint)";
                }
                item.SubItems.Add(details);

                // Color Column
                item.SubItems.Add($"#{light.Color.R:X2}{light.Color.G:X2}{light.Color.B:X2}");
                item.SubItems[3].ForeColor = light.Color;

                // Intensity Column
                item.SubItems.Add($"{light.Intensity:F2}");

                item.Tag = light;
                listView.Items.Add(item);
            }
        }

        // In LightEditorForm.cs
        private void RefreshListViewItem(ListViewItem item)
        {
            if (item == null || !(item.Tag is LightSource light)) return;

            _loadingData = true;

            item.Checked = light.IsEnabled;
            item.SubItems[1].Text = light.Type.ToString();

            // Details Column
            string details = "";
            if (light.Type == LightType.Ambient)
            {
                details = "(Ambient)";
            }
            else if (light.Type == LightType.Point)
            {
                details = $"Pos: {light.Position.ToString()}";
                if (light.SpotCutoffAngle < 90) details += $" Spot({light.SpotCutoffAngle:F1}°, {light.SpotExponent:F1})";
            }
            else if (light.Type == LightType.Directional)
            {
                details = $"Dir: {light.Direction.ToString()}";
            }
            else if (light.Type == LightType.Tint) // <<< ADDED Tint Check
            {
                details = "(Tint)";
            }
            item.SubItems[2].Text = details;

            // Color Column
            item.SubItems[3].Text = $"#{light.Color.R:X2}{light.Color.G:X2}{light.Color.B:X2}";
            item.SubItems[3].ForeColor = light.Color;

            // Intensity Column
            item.SubItems[4].Text = $"{light.Intensity:F2}";

            _loadingData = false;
        }

        private void LoadLightToEditor(LightSource light)
        {
            if (light == null)
            {
                ClearAndDisableEditor();
                return;
            }

            // --- Store Original State ---
            _originalEditingLightState = light.Clone(); // Store a copy for potential reset
            Debug.WriteLine($"Stored original state for light: {GetLightIdentifier(_originalEditingLightState)}");
            // --- End Store ---

            _loadingData = true;

            _currentlyEditingLight = light; // Keep editing the actual object in the list

            try
            {
                // (Load controls from 'light' object - code remains the same as before)
                chkEnabled.Checked = light.IsEnabled;
                cmbLightType.SelectedItem = light.Type.ToString();
                panelColorPreview.BackColor = light.Color;
                numIntensity.Value = (decimal)Math.Max((float)numIntensity.Minimum, Math.Min((float)numIntensity.Maximum, light.Intensity));
                numPosX.Value = (decimal)Math.Max((float)numPosX.Minimum, Math.Min((float)numPosX.Maximum, light.Position.X));
                numPosY.Value = (decimal)Math.Max((float)numPosY.Minimum, Math.Min((float)numPosY.Maximum, light.Position.Y));
                numPosZ.Value = (decimal)Math.Max((float)numPosZ.Minimum, Math.Min((float)numPosZ.Maximum, light.Position.Z));
                numAttConst.Value = (decimal)Math.Max((float)numAttConst.Minimum, Math.Min((float)numAttConst.Maximum, light.ConstantAttenuation));
                numAttLin.Value = (decimal)Math.Max((float)numAttLin.Minimum, Math.Min((float)numAttLin.Maximum, light.LinearAttenuation));
                numAttQuad.Value = (decimal)Math.Max((float)numAttQuad.Minimum, Math.Min((float)numAttQuad.Maximum, light.QuadraticAttenuation));
                numSpotAngle.Value = (decimal)Math.Max((float)numSpotAngle.Minimum, Math.Min((float)numSpotAngle.Maximum, light.SpotCutoffAngle));
                numSpotExponent.Value = (decimal)Math.Max((float)numSpotExponent.Minimum, Math.Min((float)numSpotExponent.Maximum, light.SpotExponent));
                numDirX.Value = (decimal)Math.Max((float)numDirX.Minimum, Math.Min((float)numDirX.Maximum, light.Direction.X));
                numDirY.Value = (decimal)Math.Max((float)numDirY.Minimum, Math.Min((float)numDirY.Maximum, light.Direction.Y));
                numDirZ.Value = (decimal)Math.Max((float)numDirZ.Minimum, Math.Min((float)numDirZ.Maximum, light.Direction.Z));

                groupBoxEditLight.Enabled = true;
            }
            catch (Exception ex) { /* ... error handling ... */ }
            finally
            {
                _loadingData = false;
            }

            UpdateEditorVisibility();

            _isDirty = false; // Reset dirty flag when loading (no UI changes made *yet*)
            btnSaveChanges.Enabled = false; // Disable Save until a change is made
        }

        // In LightEditorForm.cs
        private bool SaveLightFromEditor(LightSource light)
        {
            // This method saves from the temporary state in the editor controls
            // back to the light object reference. It's used by BtnSaveChanges_Click
            // and potentially on form closing.
            // ApplyEditorValuesToLightObject handles the real-time updates.

            if (light == null || !groupBoxEditLight.Enabled) return false; // Added check for enabled groupbox

            try
            {
                // Read common properties
                light.IsEnabled = chkEnabled.Checked;
                if (cmbLightType.SelectedItem != null)
                    light.Type = (LightType)Enum.Parse(typeof(LightType), cmbLightType.SelectedItem.ToString());
                else return false; // Cannot save without a type selected

                light.Color = panelColorPreview.BackColor;
                light.Intensity = (float)numIntensity.Value;

                // Read type-specific properties only if relevant panels are visible
                if (light.Type == LightType.Point)
                {
                    if (panelPoint.Visible) // Should always be visible if type is Point
                    {
                        light.Position = new Vector3((float)numPosX.Value, (float)numPosY.Value, (float)numPosZ.Value);
                        light.ConstantAttenuation = (float)numAttConst.Value;
                        light.LinearAttenuation = (float)numAttLin.Value;
                        light.QuadraticAttenuation = (float)numAttQuad.Value;
                        light.SpotCutoffAngle = (float)numSpotAngle.Value;
                        light.SpotExponent = (float)numSpotExponent.Value;
                    }
                    if (panelDirection.Visible) // Direction needed for spotlight axis
                    {
                        light.Direction = new Vector3((float)numDirX.Value, (float)numDirY.Value, (float)numDirZ.Value);
                    }
                }
                else if (light.Type == LightType.Directional)
                {
                    if (panelDirection.Visible) // Should always be visible if type is Directional
                    {
                        light.Direction = new Vector3((float)numDirX.Value, (float)numDirY.Value, (float)numDirZ.Value);
                    }
                }
                // For Ambient and Tint, only common properties (Enabled, Type, Color, Intensity) are saved.
                // Other properties like Position, Direction etc. retain their previous values in the object
                // but are ignored by the rendering logic based on the Type.

                Debug.WriteLine($"Saved light state from editor: Type={light.Type}, Enabled={light.IsEnabled}...");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving light properties: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Reads values from editor controls and applies them DIRECTLY to the provided LightSource object.
        /// Does NOT clear the dirty flag. Returns true if successful.
        /// </summary>
        // In LightEditorForm.cs
        private bool ApplyEditorValuesToLightObject(LightSource light)
        {
            // This method applies editor values directly to the object for real-time preview.
            if (light == null || !groupBoxEditLight.Enabled) return false;

            try
            {
                light.IsEnabled = chkEnabled.Checked;
                if (cmbLightType.SelectedItem == null) return false; // Need a type
                LightType selectedType = (LightType)Enum.Parse(typeof(LightType), cmbLightType.SelectedItem.ToString());
                light.Type = selectedType; // Update type first

                light.Color = panelColorPreview.BackColor;
                light.Intensity = (float)numIntensity.Value;

                // Apply specific properties ONLY if the type matches
                if (selectedType == LightType.Point)
                {
                    light.Position = new Vector3((float)numPosX.Value, (float)numPosY.Value, (float)numPosZ.Value);
                    light.ConstantAttenuation = (float)numAttConst.Value;
                    light.LinearAttenuation = (float)numAttLin.Value;
                    light.QuadraticAttenuation = (float)numAttQuad.Value;
                    light.SpotCutoffAngle = (float)numSpotAngle.Value;
                    light.SpotExponent = (float)numSpotExponent.Value;
                    // Update direction only if panel is visible (it should be for Point)
                    if (panelDirection.Visible)
                    {
                        light.Direction = new Vector3((float)numDirX.Value, (float)numDirY.Value, (float)numDirZ.Value);
                    }
                }
                else if (selectedType == LightType.Directional)
                {
                    // Update direction only if panel is visible (it should be for Directional)
                    if (panelDirection.Visible)
                    {
                        light.Direction = new Vector3((float)numDirX.Value, (float)numDirY.Value, (float)numDirZ.Value);
                    }
                }
                // No position/direction/attenuation/spot properties applied for Ambient or Tint

                // Debug.WriteLine($"Applied editor values to light object: {GetLightIdentifier(light)}"); // Can be noisy
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying editor values: {ex.Message}");
                return false;
            }
        }

        private void ClearAndDisableEditor()
        {
            _loadingData = true;
            _currentlyEditingLight = null;
            _originalEditingLightState = null; // <<< ADDED: Clear original state tracking

            // (Reset controls - code remains the same)
            chkEnabled.Checked = true;
            if (cmbLightType.Items.Count > 0) cmbLightType.SelectedIndex = 0; else cmbLightType.SelectedIndex = -1;
            panelColorPreview.BackColor = Color.White;
            numIntensity.Value = 1.0m;
            numPosX.Value = 0m; numPosY.Value = 0m; numPosZ.Value = 0m;
            numAttConst.Value = 1.0m; numAttLin.Value = 0m; numAttQuad.Value = 0m;
            numSpotAngle.Value = 90.0m; numSpotExponent.Value = 0m;
            numDirX.Value = 0m; numDirY.Value = 0m; numDirZ.Value = -1.0m;

            groupBoxEditLight.Enabled = false;
            UpdateEditorVisibility();
            _loadingData = false;
            _isDirty = false;
            btnSaveChanges.Enabled = false;
        }

        // In LightEditorForm.cs
        private void UpdateEditorVisibility()
        {
            if (_loadingData) return;

            LightType selectedType = LightType.Point; // Default if parsing fails or selection is null
            if (cmbLightType.SelectedItem != null)
            {
                Enum.TryParse<LightType>(cmbLightType.SelectedItem.ToString(), out selectedType);
            }

            // Determine type flags
            bool isAmbient = selectedType == LightType.Ambient;
            bool isPoint = selectedType == LightType.Point;
            bool isDirectional = selectedType == LightType.Directional;
            bool isTint = selectedType == LightType.Tint; // <<< ADDED Tint Check

            // --- Set Panel Visibility ---
            panelPoint.Visible = isPoint;
            // Show Direction panel if it's Point (for spot axis) OR Directional
            panelDirection.Visible = isPoint || isDirectional;

            // --- Enable/Disable specific common controls ---
            // Intensity is usually relevant for Ambient, Point, Directional, and Tint (as blend factor)
            // Let's assume Intensity is always relevant for now. Color is also always relevant.
            // Position/Direction/Attenuation/Spot controls are handled by panel visibility.

            Debug.WriteLine($"UpdateEditorVisibility: Type='{selectedType}', pnlPoint.Visible={panelPoint.Visible}, pnlDir.Visible={panelDirection.Visible}");
        }

        // Overload to handle Move buttons
        private void UpdateListViewButtonStates(ListView listView, Button editButton, Button removeButton, Button moveUpButton, Button moveDownButton)
        {
            bool itemSelected = listView.SelectedItems.Count > 0;
            editButton.Enabled = itemSelected;
            removeButton.Enabled = itemSelected;

            if (itemSelected)
            {
                int selectedIndex = listView.SelectedIndices[0];
                moveUpButton.Enabled = selectedIndex > 0; // Can't move first item up
                moveDownButton.Enabled = selectedIndex < listView.Items.Count - 1; // Can't move last item down
            }
            else
            {
                moveUpButton.Enabled = false;
                moveDownButton.Enabled = false;
            }
        }

        // Keep the original overload if called elsewhere, or remove if not needed
        private void UpdateListViewButtonStates(ListView listView, Button editButton, Button removeButton)
        {
            bool itemSelected = listView.SelectedItems.Count > 0;
            editButton.Enabled = itemSelected;
            removeButton.Enabled = itemSelected;
            // Assuming move buttons aren't relevant here or handled separately
        }


        // --- Event Handlers ---

        private void ListView_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // This event fires BEFORE the Checked property is actually changed.
            if (_loadingData) return; // Don't respond to programmatic changes

            ListView listView = sender as ListView;
            if (listView == null || e.Index < 0 || e.Index >= listView.Items.Count) return;

            ListViewItem item = listView.Items[e.Index];
            if (item.Tag is LightSource light)
            {
                // Get the NEW check state
                bool newEnabledState = (e.NewValue == CheckState.Checked);

                // Update the object immediately
                if (light.IsEnabled != newEnabledState)
                {
                    light.IsEnabled = newEnabledState;
                    MarkDirty(); // Mark that settings overall need saving

                    // If this light is currently being edited, update the editor checkbox too
                    // This ensures consistency if the user checks/unchecks the item being edited.
                    if (_currentlyEditingLight == light)
                    {
                        _loadingData = true; // Prevent feedback loop
                        chkEnabled.Checked = light.IsEnabled;
                        _loadingData = false;
                        // Since the editor value changed programmatically, it *might* have reset
                        // the dirty flag if MarkDirty() was only called by UI interaction.
                        // Call MarkDirty again ensures Save Changes button is enabled.
                        MarkDirty();
                    }

                    Debug.WriteLine($"Light '{GetLightIdentifier(light)}' IsEnabled set to {light.IsEnabled} via ListView Checkbox.");

                    // --- ADDED: Trigger immediate refresh ---
                    // Notify the ControlPanelForm that settings have changed,
                    // so it can update the PreviewForm in quasi-real-time.
                    RaiseSettingsChanged();
                    // --- END ADDED ---
                }
            }
        }

        private void ListViewGlobalLights_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingData) return;
            PromptSaveChangesIfDirty();

            _activeListView = listViewGlobalLights;
            // Pass the specific move buttons for this list view
            UpdateListViewButtonStates(listViewGlobalLights, btnEditGlobal, btnRemoveGlobal, btnMoveGlobalUp, btnMoveGlobalDown);

            if (listViewGlobalLights.SelectedItems.Count > 0)
            {
                if (listViewProfileLights.SelectedItems.Count > 0) listViewProfileLights.SelectedItems[0].Selected = false;
                ClearAndDisableEditor();
            }
            else
            {
                if (listViewProfileLights.SelectedItems.Count == 0) ClearAndDisableEditor();
            }
        }

        private void ListViewProfileLights_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingData || _currentProfile == null) return;
            PromptSaveChangesIfDirty();

            _activeListView = listViewProfileLights;
            // Pass the specific move buttons for this list view
            UpdateListViewButtonStates(listViewProfileLights, btnEditProfileLight, btnRemoveProfileLight, btnMoveProfileUp, btnMoveProfileDown);

            if (listViewProfileLights.SelectedItems.Count > 0)
            {
                if (listViewGlobalLights.SelectedItems.Count > 0) listViewGlobalLights.SelectedItems[0].Selected = false;
                ClearAndDisableEditor();
            }
            else
            {
                if (listViewGlobalLights.SelectedItems.Count == 0) ClearAndDisableEditor();
            }
        }



        private void TabControlLights_SelectedIndexChanged(object sender, EventArgs e)
        {
            // When switching tabs, clear editor selection if it came from the other tab's listview
            PromptSaveChangesIfDirty();

            if (tabControlLights.SelectedTab == tabPageGlobal && listViewProfileLights.SelectedItems.Count > 0)
            {
                listViewProfileLights.SelectedItems[0].Selected = false;
                ClearAndDisableEditor();
                _activeListView = listViewGlobalLights; // Assume global list is now active context
            }
            else if (tabControlLights.SelectedTab == tabPageProfile && listViewGlobalLights.SelectedItems.Count > 0)
            {
                listViewGlobalLights.SelectedItems[0].Selected = false;
                ClearAndDisableEditor();
                _activeListView = listViewProfileLights; // Assume profile list is now active context
            }
            // Update buttons for the newly visible list
            if (tabControlLights.SelectedTab == tabPageGlobal)
                UpdateListViewButtonStates(listViewGlobalLights, btnEditGlobal, btnRemoveGlobal);
            else if (tabControlLights.SelectedTab == tabPageProfile)
                UpdateListViewButtonStates(listViewProfileLights, btnEditProfileLight, btnRemoveProfileLight);
        }


        // --- Editor Control Event Handlers ---

        private void EditorControl_ValueChanged(object sender, EventArgs e)
        {
            if (_loadingData) return; // Don't react while initially loading data

            MarkDirty(); // Mark that changes need saving eventually

            // --- Trigger Real-time Preview ---
            if (_currentlyEditingLight != null)
            {
                // Apply current editor values directly to the object being edited
                // This allows the changes to be reflected when RaiseSettingsChanged triggers a refresh
                ApplyEditorValuesToLightObject(_currentlyEditingLight);

                // Now notify ControlPanelForm to refresh the preview
                RaiseSettingsChanged();
            }
            // --- End Real-time ---

            // Update editor panel visibility if necessary
            if (sender == cmbLightType || sender == numSpotAngle)
            {
                UpdateEditorVisibility();
            }
        }

        private void BtnChooseColor_Click(object sender, EventArgs e)
        {
            // Ensure the editor groupbox is enabled before allowing color change
            if (!groupBoxEditLight.Enabled || _currentlyEditingLight == null) return; // Also check if editing a light

            colorDialog1.Color = panelColorPreview.BackColor; // Start dialog with current color
            if (colorDialog1.ShowDialog(this) == DialogResult.OK)
            {
                // Check if the color actually changed
                if (panelColorPreview.BackColor != colorDialog1.Color)
                {
                    panelColorPreview.BackColor = colorDialog1.Color; // Update UI
                    MarkDirty(); // Mark that changes need saving eventually

                    // --- ADDED: Trigger Real-time Preview Update ---
                    // Apply current editor values (including the new color) to the object
                    ApplyEditorValuesToLightObject(_currentlyEditingLight);
                    // Notify ControlPanelForm to refresh the preview
                    RaiseSettingsChanged();
                    // --- END ADDED ---
                }
            }
        }

        private void BtnNormalizeDir_Click(object sender, EventArgs e)
        {
            if (!groupBoxEditLight.Enabled) return;

            try
            {
                Vector3 dir = new Vector3(
                    (float)numDirX.Value,
                    (float)numDirY.Value,
                    (float)numDirZ.Value
                );

                dir.Normalize(); // Normalize the vector

                // Update controls (prevents triggering ValueChanged repeatedly)
                _loadingData = true;
                numDirX.Value = (decimal)dir.X;
                numDirY.Value = (decimal)dir.Y;
                numDirZ.Value = (decimal)dir.Z;
                _loadingData = false;

                MarkDirty(); // Normalizing is considered a change
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error normalizing vector: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // --- Add/Edit/Remove Button Handlers ---

        private void BtnAddGlobal_Click(object sender, EventArgs e)
        {
            PromptSaveChangesIfDirty();
            AddNewLight(listViewGlobalLights, _appSettings.GlobalLights);
        }

        private void BtnAddProfileLight_Click(object sender, EventArgs e)
        {
            PromptSaveChangesIfDirty();
            if (_currentProfile != null)
            {
                AddNewLight(listViewProfileLights, _currentProfile.ProfileLights);
            }
        }

        private void AddNewLight(ListView listView, List<LightSource> lightList)
        {
            LightSource newLight = new LightSource(); // Create with defaults
            lightList.Add(newLight);

            // Add to ListView
            ListViewItem item = new ListViewItem(""); // Checkbox handles text
            item.Checked = newLight.IsEnabled;
            item.SubItems.Add(newLight.Type.ToString());
            item.SubItems.Add($"Pos: {newLight.Position.ToString()}"); // Default details
            item.SubItems.Add($"#{newLight.Color.R:X2}{newLight.Color.G:X2}{newLight.Color.B:X2}");
            item.SubItems[3].ForeColor = newLight.Color;
            item.SubItems.Add($"{newLight.Intensity:F2}");
            item.Tag = newLight;
            listView.Items.Add(item);

            // Select and load into editor
            item.Selected = true;
            item.EnsureVisible();
            listView.Focus();
            LoadLightToEditor(newLight);
            MarkDirty(); // Adding is a change that needs saving
            RaiseSettingsChanged(); // Notify parent form
        }


        private void BtnEditGlobal_Click(object sender, EventArgs e)
        {
            if (listViewGlobalLights.SelectedItems.Count > 0)
            {
                PromptSaveChangesIfDirty();
                LightSource selectedLight = listViewGlobalLights.SelectedItems[0].Tag as LightSource;
                LoadLightToEditor(selectedLight);
            }
        }

        private void BtnEditProfileLight_Click(object sender, EventArgs e)
        {
            if (listViewProfileLights.SelectedItems.Count > 0)
            {
                PromptSaveChangesIfDirty();
                LightSource selectedLight = listViewProfileLights.SelectedItems[0].Tag as LightSource;
                LoadLightToEditor(selectedLight);
            }
        }


        private void BtnRemoveGlobal_Click(object sender, EventArgs e)
        {
            RemoveSelectedLight(listViewGlobalLights, _appSettings.GlobalLights);
        }

        private void BtnRemoveProfileLight_Click(object sender, EventArgs e)
        {
            if (_currentProfile != null)
            {
                RemoveSelectedLight(listViewProfileLights, _currentProfile.ProfileLights);
            }
        }

        private void RemoveSelectedLight(ListView listView, List<LightSource> lightList)
        {
            if (listView.SelectedItems.Count == 0) return;

            ListViewItem selectedItem = listView.SelectedItems[0];
            if (!(selectedItem.Tag is LightSource lightToRemove)) return;

            string lightName = GetLightIdentifier(lightToRemove); // Get identifier before removing
            if (MessageBox.Show($"Are you sure you want to remove light '{lightName}'?", "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // If removing the light currently being edited, clear editor first
                if (_currentlyEditingLight == lightToRemove)
                {
                    ClearAndDisableEditor();
                }

                lightList.Remove(lightToRemove);
                listView.Items.Remove(selectedItem);
                MarkDirty(); // Removing requires save
                RaiseSettingsChanged(); // Notify parent form

                // Update button states after removal
                UpdateListViewButtonStates(listView,
                   (listView == listViewGlobalLights ? btnEditGlobal : btnEditProfileLight),
                   (listView == listViewGlobalLights ? btnRemoveGlobal : btnRemoveProfileLight)
                );
            }
        }

        // --- Save/Close Button Handlers ---

        private void BtnSaveChanges_Click(object sender, EventArgs e)
        {
            // Apply changes was already done in real-time via EditorControl_ValueChanged
            // This button now just finalizes the state for the current edit session.
            if (_currentlyEditingLight != null && groupBoxEditLight.Enabled)
            {
                // Ensure the underlying object IS actually up-to-date one last time
                // (though it should be already due to EditorControl_ValueChanged)
                if (!ApplyEditorValuesToLightObject(_currentlyEditingLight))
                {
                    // If applying fails somehow here, maybe show error and don't clear dirty flag?
                    MessageBox.Show("Failed to apply final values. Please check controls.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                // Find the ListViewItem and refresh its text display
                ListViewItem itemToRefresh = FindListViewItem(_currentlyEditingLight);
                if (itemToRefresh != null)
                {
                    RefreshListViewItem(itemToRefresh);
                }

                _isDirty = false; // Changes are now considered "saved" for this session
                btnSaveChanges.Enabled = false; // Disable button until new changes
                _originalEditingLightState = _currentlyEditingLight.Clone(); // Update original state baseline

                // RaiseSettingsChanged(); // No need to raise again, last EditorControl_ValueChanged did

                Debug.WriteLine($"'Saved' changes for light: {GetLightIdentifier(_currentlyEditingLight)}");
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK; // Assuming OK means "Close, I'm done"
            this.Close();
        }

        // --- Form Closing ---
        private void LightEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isDirty && _currentlyEditingLight != null && _originalEditingLightState != null)
            {
                var result = MessageBox.Show($"Apply changes made to light '{GetLightIdentifier(_originalEditingLightState)}' before closing?",
                                             "Unsaved Changes",
                                             MessageBoxButtons.YesNoCancel,
                                             MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // Attempt to apply changes. If successful, allow close. If not, cancel close.
                    if (!ApplyEditorValuesToLightObject(_currentlyEditingLight)) // Use helper to just apply state
                    {
                        e.Cancel = true; // Prevent closing if save fails
                    }
                    else
                    {
                        // Changes successfully applied to object, allow close.
                        // Parent will get the latest state via the list reference.
                        // Optionally refresh list item one last time? Not strictly needed if closing.
                        // RaiseSettingsChanged(); // Raise one last time maybe? Or assume parent handles on close? Let's raise it.
                        RaiseSettingsChanged();
                    }
                }
                else if (result == DialogResult.No)
                {
                    // Discard changes by restoring original state, allow close
                    RestoreOriginalLightState();
                    RaiseSettingsChanged(); // Ensure preview reflects discarded changes
                }
                else // Cancel
                {
                    e.Cancel = true; // Prevent closing
                }
            }
            // If not dirty, or user chose Yes/No, the form closes naturally.
        }

        // --- Helper Methods ---

        private void MarkDirty()
        {
            if (_loadingData) return; // Don't mark dirty while initially loading data
            _isDirty = true;
            btnSaveChanges.Enabled = true; // Enable Apply button when changes are made
        }

        private void PromptSaveChangesIfDirty()
        {
            if (_isDirty && _currentlyEditingLight != null && _originalEditingLightState != null)
            {
                var result = MessageBox.Show($"Apply changes made to light '{GetLightIdentifier(_originalEditingLightState)}' first?",
                                             "Apply Changes",
                                             MessageBoxButtons.YesNoCancel,
                                             MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    BtnSaveChanges_Click(null, EventArgs.Empty);
                    // If save fails inside BtnSaveChanges, dirty remains true, but context switch proceeds
                }
                else if (result == DialogResult.No)
                {
                    // --- Restore original state ---
                    Debug.WriteLine($"Discarding changes, restoring original state for {GetLightIdentifier(_originalEditingLightState)}");
                    // Apply original values back to the object being edited
                    RestoreOriginalLightState(); // Use helper
                    LoadLightToEditor(_currentlyEditingLight); // Reload editor to show original state
                    RaiseSettingsChanged(); // Update preview back to original state
                    _isDirty = false; // Discarded changes
                    btnSaveChanges.Enabled = false;
                    // --- End Restore ---
                }
                // If Cancel, do nothing - user stays in current edit context implicitly
            }
        }

        /// <summary>
        /// Helper to restore the currently edited light back to its originally loaded state.
        /// </summary>
        private void RestoreOriginalLightState()
        {
            if (_currentlyEditingLight == null || _originalEditingLightState == null) return;

            // Copy properties from the stored original state back to the actual object
            _currentlyEditingLight.IsEnabled = _originalEditingLightState.IsEnabled;
            _currentlyEditingLight.Type = _originalEditingLightState.Type;
            _currentlyEditingLight.Position = _originalEditingLightState.Position;
            _currentlyEditingLight.Direction = _originalEditingLightState.Direction;
            _currentlyEditingLight.Color = _originalEditingLightState.Color;
            _currentlyEditingLight.Intensity = _originalEditingLightState.Intensity;
            _currentlyEditingLight.ConstantAttenuation = _originalEditingLightState.ConstantAttenuation;
            _currentlyEditingLight.LinearAttenuation = _originalEditingLightState.LinearAttenuation;
            _currentlyEditingLight.QuadraticAttenuation = _originalEditingLightState.QuadraticAttenuation;
            _currentlyEditingLight.SpotCutoffAngle = _originalEditingLightState.SpotCutoffAngle;
            _currentlyEditingLight.SpotExponent = _originalEditingLightState.SpotExponent;

            // Refresh the corresponding list view item as well
            ListViewItem item = FindListViewItem(_currentlyEditingLight);
            if (item != null) RefreshListViewItem(item);
        }


        private ListViewItem FindListViewItem(LightSource light)
        {
            foreach (ListViewItem item in listViewGlobalLights.Items)
            {
                if (item.Tag == light) return item;
            }
            foreach (ListViewItem item in listViewProfileLights.Items)
            {
                if (item.Tag == light) return item;
            }
            return null;
        }

        private string GetLightIdentifier(LightSource light)
        {
            // Create a simple identifier for messages, etc.
            if (light == null) return "None";
            // Example: "Point at (0.1, -0.2, 0.5)" or "Directional (0,0,-1)"
            string details = light.Type == LightType.Point ? light.Position.ToString() : light.Direction.ToString();
            return $"{light.Type} {details}";
        }

        protected virtual void RaiseSettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
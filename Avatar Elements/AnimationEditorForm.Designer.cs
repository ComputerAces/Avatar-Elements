
namespace Avatar_Elements {
    partial class AnimationEditorForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnimationEditorForm));
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.panelTimelineButtons = new System.Windows.Forms.Panel();
            this.btnPasteTimeline = new System.Windows.Forms.Button();
            this.btnCopyTimeline = new System.Windows.Forms.Button();
            this.btnRenameTimeline = new System.Windows.Forms.Button();
            this.btnRemoveTimeline = new System.Windows.Forms.Button();
            this.btnAddTimeline = new System.Windows.Forms.Button();
            this.listViewTimelines = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.labelTimelines = new System.Windows.Forms.Label();
            this.groupBoxTimelineHotkey = new System.Windows.Forms.GroupBox();
            this.btnClearTimelineHotkey = new System.Windows.Forms.Button();
            this.btnSetTimelineHotkey = new System.Windows.Forms.Button();
            this.txtTimelineHotkeyDisplay = new System.Windows.Forms.TextBox();
            this.labelHotkeyDisplay = new System.Windows.Forms.Label();
            this.panelKeyframeEditor = new System.Windows.Forms.Panel();
            this.groupBoxEditKeyframe = new System.Windows.Forms.GroupBox();
            this.comboInterpolation = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.butEditSave = new System.Windows.Forms.Button();
            this.butEditCancel = new System.Windows.Forms.Button();
            this.comboAnchor = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numScale = new System.Windows.Forms.NumericUpDown();
            this.labelScale = new System.Windows.Forms.Label();
            this.numOffsetY = new System.Windows.Forms.NumericUpDown();
            this.labelOffsetY = new System.Windows.Forms.Label();
            this.numOffsetX = new System.Windows.Forms.NumericUpDown();
            this.labelOffsetX = new System.Windows.Forms.Label();
            this.numTimestamp = new System.Windows.Forms.NumericUpDown();
            this.labelTimestamp = new System.Windows.Forms.Label();
            this.panelKeyframes = new System.Windows.Forms.Panel();
            this.panelKeyframeButtons = new System.Windows.Forms.Panel();
            this.btnMoveKeyframeDown = new System.Windows.Forms.Button();
            this.btnMoveKeyframeUp = new System.Windows.Forms.Button();
            this.btnRemoveKeyframe = new System.Windows.Forms.Button();
            this.btnAddKeyframe = new System.Windows.Forms.Button();
            this.listViewKeyframes = new System.Windows.Forms.ListView();
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.labelKeyframes = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.clipboardMonitorTimer = new System.Windows.Forms.Timer(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.chkTimelineLoop = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.panelTimelineButtons.SuspendLayout();
            this.groupBoxTimelineHotkey.SuspendLayout();
            this.panelKeyframeEditor.SuspendLayout();
            this.groupBoxEditKeyframe.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimestamp)).BeginInit();
            this.panelKeyframes.SuspendLayout();
            this.panelKeyframeButtons.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.panelTimelineButtons);
            this.splitContainerMain.Panel1.Controls.Add(this.listViewTimelines);
            this.splitContainerMain.Panel1.Controls.Add(this.labelTimelines);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.groupBoxTimelineHotkey);
            this.splitContainerMain.Panel2.Controls.Add(this.panelKeyframeEditor);
            this.splitContainerMain.Panel2.Controls.Add(this.panelKeyframes);
            this.splitContainerMain.Size = new System.Drawing.Size(652, 473);
            this.splitContainerMain.SplitterDistance = 269;
            this.splitContainerMain.TabIndex = 0;
            // 
            // panelTimelineButtons
            // 
            this.panelTimelineButtons.Controls.Add(this.btnPasteTimeline);
            this.panelTimelineButtons.Controls.Add(this.btnCopyTimeline);
            this.panelTimelineButtons.Controls.Add(this.btnRenameTimeline);
            this.panelTimelineButtons.Controls.Add(this.btnRemoveTimeline);
            this.panelTimelineButtons.Controls.Add(this.btnAddTimeline);
            this.panelTimelineButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelTimelineButtons.Location = new System.Drawing.Point(0, 404);
            this.panelTimelineButtons.Name = "panelTimelineButtons";
            this.panelTimelineButtons.Size = new System.Drawing.Size(269, 69);
            this.panelTimelineButtons.TabIndex = 2;
            // 
            // btnPasteTimeline
            // 
            this.btnPasteTimeline.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPasteTimeline.Enabled = false;
            this.btnPasteTimeline.Location = new System.Drawing.Point(178, 8);
            this.btnPasteTimeline.Name = "btnPasteTimeline";
            this.btnPasteTimeline.Size = new System.Drawing.Size(75, 23);
            this.btnPasteTimeline.TabIndex = 4;
            this.btnPasteTimeline.Text = "Paste";
            this.btnPasteTimeline.UseVisualStyleBackColor = true;
            // 
            // btnCopyTimeline
            // 
            this.btnCopyTimeline.Enabled = false;
            this.btnCopyTimeline.Location = new System.Drawing.Point(9, 8);
            this.btnCopyTimeline.Name = "btnCopyTimeline";
            this.btnCopyTimeline.Size = new System.Drawing.Size(75, 23);
            this.btnCopyTimeline.TabIndex = 3;
            this.btnCopyTimeline.Text = "Copy";
            this.btnCopyTimeline.UseVisualStyleBackColor = true;
            // 
            // btnRenameTimeline
            // 
            this.btnRenameTimeline.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnRenameTimeline.Location = new System.Drawing.Point(94, 38);
            this.btnRenameTimeline.Name = "btnRenameTimeline";
            this.btnRenameTimeline.Size = new System.Drawing.Size(75, 23);
            this.btnRenameTimeline.TabIndex = 2;
            this.btnRenameTimeline.Text = "Rename";
            this.btnRenameTimeline.UseVisualStyleBackColor = true;
            // 
            // btnRemoveTimeline
            // 
            this.btnRemoveTimeline.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveTimeline.Location = new System.Drawing.Point(178, 37);
            this.btnRemoveTimeline.Name = "btnRemoveTimeline";
            this.btnRemoveTimeline.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveTimeline.TabIndex = 1;
            this.btnRemoveTimeline.Text = "Remove";
            this.btnRemoveTimeline.UseVisualStyleBackColor = true;
            // 
            // btnAddTimeline
            // 
            this.btnAddTimeline.Location = new System.Drawing.Point(9, 37);
            this.btnAddTimeline.Name = "btnAddTimeline";
            this.btnAddTimeline.Size = new System.Drawing.Size(75, 23);
            this.btnAddTimeline.TabIndex = 0;
            this.btnAddTimeline.Text = "Add";
            this.btnAddTimeline.UseVisualStyleBackColor = true;
            // 
            // listViewTimelines
            // 
            this.listViewTimelines.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.listViewTimelines.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewTimelines.FullRowSelect = true;
            this.listViewTimelines.HideSelection = false;
            this.listViewTimelines.Location = new System.Drawing.Point(0, 23);
            this.listViewTimelines.MultiSelect = false;
            this.listViewTimelines.Name = "listViewTimelines";
            this.listViewTimelines.Size = new System.Drawing.Size(269, 450);
            this.listViewTimelines.TabIndex = 1;
            this.listViewTimelines.UseCompatibleStateImageBehavior = false;
            this.listViewTimelines.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Loop";
            this.columnHeader2.Width = 50;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Duration (s)";
            this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader3.Width = 70;
            // 
            // labelTimelines
            // 
            this.labelTimelines.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelTimelines.Location = new System.Drawing.Point(0, 0);
            this.labelTimelines.Name = "labelTimelines";
            this.labelTimelines.Size = new System.Drawing.Size(269, 23);
            this.labelTimelines.TabIndex = 0;
            this.labelTimelines.Text = "Timelines :";
            this.labelTimelines.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBoxTimelineHotkey
            // 
            this.groupBoxTimelineHotkey.Controls.Add(this.chkTimelineLoop);
            this.groupBoxTimelineHotkey.Controls.Add(this.label3);
            this.groupBoxTimelineHotkey.Controls.Add(this.btnClearTimelineHotkey);
            this.groupBoxTimelineHotkey.Controls.Add(this.btnSetTimelineHotkey);
            this.groupBoxTimelineHotkey.Controls.Add(this.txtTimelineHotkeyDisplay);
            this.groupBoxTimelineHotkey.Controls.Add(this.labelHotkeyDisplay);
            this.groupBoxTimelineHotkey.Location = new System.Drawing.Point(2, 168);
            this.groupBoxTimelineHotkey.Name = "groupBoxTimelineHotkey";
            this.groupBoxTimelineHotkey.Size = new System.Drawing.Size(377, 79);
            this.groupBoxTimelineHotkey.TabIndex = 3;
            this.groupBoxTimelineHotkey.TabStop = false;
            this.groupBoxTimelineHotkey.Text = "Timeline Hotkey";
            // 
            // btnClearTimelineHotkey
            // 
            this.btnClearTimelineHotkey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearTimelineHotkey.Location = new System.Drawing.Point(311, 16);
            this.btnClearTimelineHotkey.Name = "btnClearTimelineHotkey";
            this.btnClearTimelineHotkey.Size = new System.Drawing.Size(54, 23);
            this.btnClearTimelineHotkey.TabIndex = 4;
            this.btnClearTimelineHotkey.Text = "Clear";
            this.btnClearTimelineHotkey.UseVisualStyleBackColor = true;
            // 
            // btnSetTimelineHotkey
            // 
            this.btnSetTimelineHotkey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetTimelineHotkey.Location = new System.Drawing.Point(256, 16);
            this.btnSetTimelineHotkey.Name = "btnSetTimelineHotkey";
            this.btnSetTimelineHotkey.Size = new System.Drawing.Size(49, 23);
            this.btnSetTimelineHotkey.TabIndex = 3;
            this.btnSetTimelineHotkey.Text = "Set ....";
            this.btnSetTimelineHotkey.UseVisualStyleBackColor = true;
            // 
            // txtTimelineHotkeyDisplay
            // 
            this.txtTimelineHotkeyDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTimelineHotkeyDisplay.Location = new System.Drawing.Point(95, 18);
            this.txtTimelineHotkeyDisplay.Name = "txtTimelineHotkeyDisplay";
            this.txtTimelineHotkeyDisplay.ReadOnly = true;
            this.txtTimelineHotkeyDisplay.Size = new System.Drawing.Size(155, 20);
            this.txtTimelineHotkeyDisplay.TabIndex = 2;
            // 
            // labelHotkeyDisplay
            // 
            this.labelHotkeyDisplay.Location = new System.Drawing.Point(11, 16);
            this.labelHotkeyDisplay.Name = "labelHotkeyDisplay";
            this.labelHotkeyDisplay.Size = new System.Drawing.Size(78, 23);
            this.labelHotkeyDisplay.TabIndex = 1;
            this.labelHotkeyDisplay.Text = "Current :";
            this.labelHotkeyDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panelKeyframeEditor
            // 
            this.panelKeyframeEditor.Controls.Add(this.groupBoxEditKeyframe);
            this.panelKeyframeEditor.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelKeyframeEditor.Location = new System.Drawing.Point(0, 253);
            this.panelKeyframeEditor.Name = "panelKeyframeEditor";
            this.panelKeyframeEditor.Size = new System.Drawing.Size(379, 220);
            this.panelKeyframeEditor.TabIndex = 2;
            // 
            // groupBoxEditKeyframe
            // 
            this.groupBoxEditKeyframe.Controls.Add(this.comboInterpolation);
            this.groupBoxEditKeyframe.Controls.Add(this.label2);
            this.groupBoxEditKeyframe.Controls.Add(this.panel1);
            this.groupBoxEditKeyframe.Controls.Add(this.comboAnchor);
            this.groupBoxEditKeyframe.Controls.Add(this.label1);
            this.groupBoxEditKeyframe.Controls.Add(this.numScale);
            this.groupBoxEditKeyframe.Controls.Add(this.labelScale);
            this.groupBoxEditKeyframe.Controls.Add(this.numOffsetY);
            this.groupBoxEditKeyframe.Controls.Add(this.labelOffsetY);
            this.groupBoxEditKeyframe.Controls.Add(this.numOffsetX);
            this.groupBoxEditKeyframe.Controls.Add(this.labelOffsetX);
            this.groupBoxEditKeyframe.Controls.Add(this.numTimestamp);
            this.groupBoxEditKeyframe.Controls.Add(this.labelTimestamp);
            this.groupBoxEditKeyframe.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBoxEditKeyframe.Enabled = false;
            this.groupBoxEditKeyframe.Location = new System.Drawing.Point(0, 3);
            this.groupBoxEditKeyframe.Name = "groupBoxEditKeyframe";
            this.groupBoxEditKeyframe.Size = new System.Drawing.Size(379, 217);
            this.groupBoxEditKeyframe.TabIndex = 0;
            this.groupBoxEditKeyframe.TabStop = false;
            this.groupBoxEditKeyframe.Text = "Edit Selected Keyframe";
            // 
            // comboInterpolation
            // 
            this.comboInterpolation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboInterpolation.FormattingEnabled = true;
            this.comboInterpolation.Items.AddRange(new object[] {
            "Center",
            "Bottom",
            "Top",
            "Left",
            "Right"});
            this.comboInterpolation.Location = new System.Drawing.Point(122, 147);
            this.comboInterpolation.Name = "comboInterpolation";
            this.comboInterpolation.Size = new System.Drawing.Size(121, 21);
            this.comboInterpolation.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(10, 145);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 23);
            this.label2.TabIndex = 11;
            this.label2.Text = "Out Interpolation :";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.butEditSave);
            this.panel1.Controls.Add(this.butEditCancel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(3, 176);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(373, 38);
            this.panel1.TabIndex = 10;
            // 
            // butEditSave
            // 
            this.butEditSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.butEditSave.Location = new System.Drawing.Point(286, 11);
            this.butEditSave.Name = "butEditSave";
            this.butEditSave.Size = new System.Drawing.Size(75, 23);
            this.butEditSave.TabIndex = 1;
            this.butEditSave.Text = "Save";
            this.butEditSave.UseVisualStyleBackColor = true;
            // 
            // butEditCancel
            // 
            this.butEditCancel.Location = new System.Drawing.Point(10, 10);
            this.butEditCancel.Name = "butEditCancel";
            this.butEditCancel.Size = new System.Drawing.Size(75, 23);
            this.butEditCancel.TabIndex = 0;
            this.butEditCancel.Text = "Cancel";
            this.butEditCancel.UseVisualStyleBackColor = true;
            // 
            // comboAnchor
            // 
            this.comboAnchor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAnchor.FormattingEnabled = true;
            this.comboAnchor.Items.AddRange(new object[] {
            "Center",
            "Bottom",
            "Top",
            "Left",
            "Right"});
            this.comboAnchor.Location = new System.Drawing.Point(122, 120);
            this.comboAnchor.Name = "comboAnchor";
            this.comboAnchor.Size = new System.Drawing.Size(121, 21);
            this.comboAnchor.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(10, 118);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 23);
            this.label1.TabIndex = 8;
            this.label1.Text = "Anchor :";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numScale
            // 
            this.numScale.DecimalPlaces = 2;
            this.numScale.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numScale.Location = new System.Drawing.Point(122, 94);
            this.numScale.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numScale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numScale.Name = "numScale";
            this.numScale.Size = new System.Drawing.Size(120, 20);
            this.numScale.TabIndex = 7;
            this.numScale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // labelScale
            // 
            this.labelScale.Location = new System.Drawing.Point(10, 91);
            this.labelScale.Name = "labelScale";
            this.labelScale.Size = new System.Drawing.Size(106, 23);
            this.labelScale.TabIndex = 6;
            this.labelScale.Text = "Scale :";
            this.labelScale.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numOffsetY
            // 
            this.numOffsetY.DecimalPlaces = 1;
            this.numOffsetY.Location = new System.Drawing.Point(122, 68);
            this.numOffsetY.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numOffsetY.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numOffsetY.Name = "numOffsetY";
            this.numOffsetY.Size = new System.Drawing.Size(120, 20);
            this.numOffsetY.TabIndex = 5;
            // 
            // labelOffsetY
            // 
            this.labelOffsetY.Location = new System.Drawing.Point(10, 65);
            this.labelOffsetY.Name = "labelOffsetY";
            this.labelOffsetY.Size = new System.Drawing.Size(106, 23);
            this.labelOffsetY.TabIndex = 4;
            this.labelOffsetY.Text = "Y Offset :";
            this.labelOffsetY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numOffsetX
            // 
            this.numOffsetX.DecimalPlaces = 1;
            this.numOffsetX.Location = new System.Drawing.Point(122, 45);
            this.numOffsetX.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numOffsetX.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numOffsetX.Name = "numOffsetX";
            this.numOffsetX.Size = new System.Drawing.Size(120, 20);
            this.numOffsetX.TabIndex = 3;
            // 
            // labelOffsetX
            // 
            this.labelOffsetX.Location = new System.Drawing.Point(10, 42);
            this.labelOffsetX.Name = "labelOffsetX";
            this.labelOffsetX.Size = new System.Drawing.Size(106, 23);
            this.labelOffsetX.TabIndex = 2;
            this.labelOffsetX.Text = "X Offset :";
            this.labelOffsetX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numTimestamp
            // 
            this.numTimestamp.DecimalPlaces = 2;
            this.numTimestamp.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numTimestamp.Location = new System.Drawing.Point(122, 19);
            this.numTimestamp.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numTimestamp.Name = "numTimestamp";
            this.numTimestamp.Size = new System.Drawing.Size(120, 20);
            this.numTimestamp.TabIndex = 1;
            // 
            // labelTimestamp
            // 
            this.labelTimestamp.Location = new System.Drawing.Point(10, 16);
            this.labelTimestamp.Name = "labelTimestamp";
            this.labelTimestamp.Size = new System.Drawing.Size(106, 23);
            this.labelTimestamp.TabIndex = 0;
            this.labelTimestamp.Text = "Time (s) :";
            this.labelTimestamp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panelKeyframes
            // 
            this.panelKeyframes.Controls.Add(this.panelKeyframeButtons);
            this.panelKeyframes.Controls.Add(this.listViewKeyframes);
            this.panelKeyframes.Controls.Add(this.labelKeyframes);
            this.panelKeyframes.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelKeyframes.Location = new System.Drawing.Point(0, 0);
            this.panelKeyframes.Name = "panelKeyframes";
            this.panelKeyframes.Size = new System.Drawing.Size(379, 168);
            this.panelKeyframes.TabIndex = 1;
            // 
            // panelKeyframeButtons
            // 
            this.panelKeyframeButtons.Controls.Add(this.btnMoveKeyframeDown);
            this.panelKeyframeButtons.Controls.Add(this.btnMoveKeyframeUp);
            this.panelKeyframeButtons.Controls.Add(this.btnRemoveKeyframe);
            this.panelKeyframeButtons.Controls.Add(this.btnAddKeyframe);
            this.panelKeyframeButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelKeyframeButtons.Enabled = false;
            this.panelKeyframeButtons.Location = new System.Drawing.Point(0, 130);
            this.panelKeyframeButtons.Name = "panelKeyframeButtons";
            this.panelKeyframeButtons.Size = new System.Drawing.Size(379, 38);
            this.panelKeyframeButtons.TabIndex = 2;
            // 
            // btnMoveKeyframeDown
            // 
            this.btnMoveKeyframeDown.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnMoveKeyframeDown.Location = new System.Drawing.Point(191, 9);
            this.btnMoveKeyframeDown.Name = "btnMoveKeyframeDown";
            this.btnMoveKeyframeDown.Size = new System.Drawing.Size(75, 23);
            this.btnMoveKeyframeDown.TabIndex = 4;
            this.btnMoveKeyframeDown.Text = "Down";
            this.btnMoveKeyframeDown.UseVisualStyleBackColor = true;
            // 
            // btnMoveKeyframeUp
            // 
            this.btnMoveKeyframeUp.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnMoveKeyframeUp.Location = new System.Drawing.Point(110, 9);
            this.btnMoveKeyframeUp.Name = "btnMoveKeyframeUp";
            this.btnMoveKeyframeUp.Size = new System.Drawing.Size(75, 23);
            this.btnMoveKeyframeUp.TabIndex = 3;
            this.btnMoveKeyframeUp.Text = "Up";
            this.btnMoveKeyframeUp.UseVisualStyleBackColor = true;
            // 
            // btnRemoveKeyframe
            // 
            this.btnRemoveKeyframe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveKeyframe.Location = new System.Drawing.Point(289, 9);
            this.btnRemoveKeyframe.Name = "btnRemoveKeyframe";
            this.btnRemoveKeyframe.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveKeyframe.TabIndex = 2;
            this.btnRemoveKeyframe.Text = "Remove";
            this.btnRemoveKeyframe.UseVisualStyleBackColor = true;
            // 
            // btnAddKeyframe
            // 
            this.btnAddKeyframe.Location = new System.Drawing.Point(13, 9);
            this.btnAddKeyframe.Name = "btnAddKeyframe";
            this.btnAddKeyframe.Size = new System.Drawing.Size(75, 23);
            this.btnAddKeyframe.TabIndex = 1;
            this.btnAddKeyframe.Text = "Add";
            this.btnAddKeyframe.UseVisualStyleBackColor = true;
            // 
            // listViewKeyframes
            // 
            this.listViewKeyframes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9});
            this.listViewKeyframes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewKeyframes.FullRowSelect = true;
            this.listViewKeyframes.HideSelection = false;
            this.listViewKeyframes.Location = new System.Drawing.Point(0, 23);
            this.listViewKeyframes.MultiSelect = false;
            this.listViewKeyframes.Name = "listViewKeyframes";
            this.listViewKeyframes.Size = new System.Drawing.Size(379, 145);
            this.listViewKeyframes.TabIndex = 1;
            this.listViewKeyframes.UseCompatibleStateImageBehavior = false;
            this.listViewKeyframes.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Time (s)";
            this.columnHeader4.Width = 70;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "X Offset";
            this.columnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader5.Width = 70;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Y Offset";
            this.columnHeader6.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader6.Width = 70;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Scale";
            this.columnHeader7.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader7.Width = 70;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Anchor";
            this.columnHeader8.Width = 70;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Interpolation";
            this.columnHeader9.Width = 90;
            // 
            // labelKeyframes
            // 
            this.labelKeyframes.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelKeyframes.Location = new System.Drawing.Point(0, 0);
            this.labelKeyframes.Name = "labelKeyframes";
            this.labelKeyframes.Size = new System.Drawing.Size(379, 23);
            this.labelKeyframes.TabIndex = 0;
            this.labelKeyframes.Text = "Keyframes :";
            this.labelKeyframes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnSaveChanges);
            this.panel2.Controls.Add(this.btnClose);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 471);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(652, 38);
            this.panel2.TabIndex = 1;
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveChanges.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSaveChanges.Location = new System.Drawing.Point(508, 8);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(129, 23);
            this.btnSaveChanges.TabIndex = 2;
            this.btnSaveChanges.Text = "Save Changes";
            this.btnSaveChanges.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(12, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(129, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // clipboardMonitorTimer
            // 
            this.clipboardMonitorTimer.Interval = 500;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(11, 41);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(78, 23);
            this.label3.TabIndex = 5;
            this.label3.Text = "Loop :";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkTimelineLoop
            // 
            this.chkTimelineLoop.AutoSize = true;
            this.chkTimelineLoop.Location = new System.Drawing.Point(95, 45);
            this.chkTimelineLoop.Name = "chkTimelineLoop";
            this.chkTimelineLoop.Size = new System.Drawing.Size(99, 17);
            this.chkTimelineLoop.TabIndex = 6;
            this.chkTimelineLoop.Text = "Loop Animation";
            this.chkTimelineLoop.UseVisualStyleBackColor = true;
            // 
            // AnimationEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(652, 509);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.splitContainerMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AnimationEditorForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Animation Editor";
            this.Load += new System.EventHandler(this.AnimationEditorForm_Load);
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.panelTimelineButtons.ResumeLayout(false);
            this.groupBoxTimelineHotkey.ResumeLayout(false);
            this.groupBoxTimelineHotkey.PerformLayout();
            this.panelKeyframeEditor.ResumeLayout(false);
            this.groupBoxEditKeyframe.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimestamp)).EndInit();
            this.panelKeyframes.ResumeLayout(false);
            this.panelKeyframeButtons.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.Panel panelTimelineButtons;
        private System.Windows.Forms.Button btnRenameTimeline;
        private System.Windows.Forms.Button btnRemoveTimeline;
        private System.Windows.Forms.Button btnAddTimeline;
        private System.Windows.Forms.ListView listViewTimelines;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Label labelTimelines;
        private System.Windows.Forms.Panel panelKeyframeEditor;
        private System.Windows.Forms.Panel panelKeyframes;
        private System.Windows.Forms.Panel panelKeyframeButtons;
        private System.Windows.Forms.Button btnMoveKeyframeDown;
        private System.Windows.Forms.Button btnMoveKeyframeUp;
        private System.Windows.Forms.Button btnRemoveKeyframe;
        private System.Windows.Forms.Button btnAddKeyframe;
        private System.Windows.Forms.ListView listViewKeyframes;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.Label labelKeyframes;
        private System.Windows.Forms.GroupBox groupBoxEditKeyframe;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button butEditSave;
        private System.Windows.Forms.Button butEditCancel;
        private System.Windows.Forms.ComboBox comboAnchor;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numScale;
        private System.Windows.Forms.Label labelScale;
        private System.Windows.Forms.NumericUpDown numOffsetY;
        private System.Windows.Forms.Label labelOffsetY;
        private System.Windows.Forms.NumericUpDown numOffsetX;
        private System.Windows.Forms.Label labelOffsetX;
        private System.Windows.Forms.NumericUpDown numTimestamp;
        private System.Windows.Forms.Label labelTimestamp;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.GroupBox groupBoxTimelineHotkey;
        private System.Windows.Forms.Button btnSetTimelineHotkey;
        private System.Windows.Forms.TextBox txtTimelineHotkeyDisplay;
        private System.Windows.Forms.Label labelHotkeyDisplay;
        private System.Windows.Forms.Button btnClearTimelineHotkey;
        private System.Windows.Forms.Button btnPasteTimeline;
        private System.Windows.Forms.Button btnCopyTimeline;
        private System.Windows.Forms.Timer clipboardMonitorTimer;
        private System.Windows.Forms.ComboBox comboInterpolation;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.CheckBox chkTimelineLoop;
        private System.Windows.Forms.Label label3;
    }
}
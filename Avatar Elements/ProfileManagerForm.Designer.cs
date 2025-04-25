
namespace Avatar_Elements {
    partial class ProfileManagerForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfileManagerForm));
            this.listViewAvatarProfiles = new System.Windows.Forms.ListView();
            this.colProfileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colHotkey = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colImagePath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnAddProfile = new System.Windows.Forms.Button();
            this.btnEditProfile = new System.Windows.Forms.Button();
            this.btnRemoveProfile = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkStartMinimized = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtGlobalHideShowHotkey = new System.Windows.Forms.TextBox();
            this.btnSetGlobalHideShowHotkey = new System.Windows.Forms.Button();
            this.butClose = new System.Windows.Forms.Button();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listViewAvatarProfiles
            // 
            this.listViewAvatarProfiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewAvatarProfiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colProfileName,
            this.colHotkey,
            this.colImagePath});
            this.listViewAvatarProfiles.FullRowSelect = true;
            this.listViewAvatarProfiles.GridLines = true;
            this.listViewAvatarProfiles.HideSelection = false;
            this.listViewAvatarProfiles.Location = new System.Drawing.Point(12, 12);
            this.listViewAvatarProfiles.MultiSelect = false;
            this.listViewAvatarProfiles.Name = "listViewAvatarProfiles";
            this.listViewAvatarProfiles.Size = new System.Drawing.Size(598, 213);
            this.listViewAvatarProfiles.TabIndex = 0;
            this.listViewAvatarProfiles.UseCompatibleStateImageBehavior = false;
            this.listViewAvatarProfiles.View = System.Windows.Forms.View.Details;
            // 
            // colProfileName
            // 
            this.colProfileName.Text = "Profile Name";
            this.colProfileName.Width = 150;
            // 
            // colHotkey
            // 
            this.colHotkey.Text = "Hotkey";
            this.colHotkey.Width = 120;
            // 
            // colImagePath
            // 
            this.colImagePath.Text = "Base Image Path";
            this.colImagePath.Width = 300;
            // 
            // btnAddProfile
            // 
            this.btnAddProfile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddProfile.Location = new System.Drawing.Point(12, 231);
            this.btnAddProfile.Name = "btnAddProfile";
            this.btnAddProfile.Size = new System.Drawing.Size(131, 23);
            this.btnAddProfile.TabIndex = 1;
            this.btnAddProfile.Text = "Add New...";
            this.btnAddProfile.UseVisualStyleBackColor = true;
            // 
            // btnEditProfile
            // 
            this.btnEditProfile.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnEditProfile.Enabled = false;
            this.btnEditProfile.Location = new System.Drawing.Point(246, 231);
            this.btnEditProfile.Name = "btnEditProfile";
            this.btnEditProfile.Size = new System.Drawing.Size(131, 23);
            this.btnEditProfile.TabIndex = 2;
            this.btnEditProfile.Text = "Edit Selected...";
            this.btnEditProfile.UseVisualStyleBackColor = true;
            // 
            // btnRemoveProfile
            // 
            this.btnRemoveProfile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveProfile.Enabled = false;
            this.btnRemoveProfile.Location = new System.Drawing.Point(479, 231);
            this.btnRemoveProfile.Name = "btnRemoveProfile";
            this.btnRemoveProfile.Size = new System.Drawing.Size(131, 23);
            this.btnRemoveProfile.TabIndex = 3;
            this.btnRemoveProfile.Text = "Remove Selected";
            this.btnRemoveProfile.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btnSetGlobalHideShowHotkey);
            this.groupBox1.Controls.Add(this.txtGlobalHideShowHotkey);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.chkStartMinimized);
            this.groupBox1.Location = new System.Drawing.Point(12, 260);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(598, 49);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Global Settings";
            // 
            // chkStartMinimized
            // 
            this.chkStartMinimized.AutoSize = true;
            this.chkStartMinimized.Location = new System.Drawing.Point(6, 19);
            this.chkStartMinimized.Name = "chkStartMinimized";
            this.chkStartMinimized.Size = new System.Drawing.Size(220, 17);
            this.chkStartMinimized.TabIndex = 0;
            this.chkStartMinimized.Text = "Start application minimized to system tray.";
            this.chkStartMinimized.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(232, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(156, 23);
            this.label1.TabIndex = 1;
            this.label1.Text = "Global Hide/Show Hotkey :";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtGlobalHideShowHotkey
            // 
            this.txtGlobalHideShowHotkey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGlobalHideShowHotkey.Location = new System.Drawing.Point(394, 17);
            this.txtGlobalHideShowHotkey.Name = "txtGlobalHideShowHotkey";
            this.txtGlobalHideShowHotkey.ReadOnly = true;
            this.txtGlobalHideShowHotkey.Size = new System.Drawing.Size(136, 20);
            this.txtGlobalHideShowHotkey.TabIndex = 2;
            // 
            // btnSetGlobalHideShowHotkey
            // 
            this.btnSetGlobalHideShowHotkey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetGlobalHideShowHotkey.Location = new System.Drawing.Point(536, 15);
            this.btnSetGlobalHideShowHotkey.Name = "btnSetGlobalHideShowHotkey";
            this.btnSetGlobalHideShowHotkey.Size = new System.Drawing.Size(56, 23);
            this.btnSetGlobalHideShowHotkey.TabIndex = 3;
            this.btnSetGlobalHideShowHotkey.Text = "Set...";
            this.btnSetGlobalHideShowHotkey.UseVisualStyleBackColor = true;
            // 
            // butClose
            // 
            this.butClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.butClose.Location = new System.Drawing.Point(12, 318);
            this.butClose.Name = "butClose";
            this.butClose.Size = new System.Drawing.Size(75, 23);
            this.butClose.TabIndex = 5;
            this.butClose.Text = "Close";
            this.butClose.UseVisualStyleBackColor = true;
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveSettings.Location = new System.Drawing.Point(529, 318);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(75, 23);
            this.btnSaveSettings.TabIndex = 6;
            this.btnSaveSettings.Text = "Save";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            // 
            // ProfileManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(622, 353);
            this.Controls.Add(this.btnSaveSettings);
            this.Controls.Add(this.butClose);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnRemoveProfile);
            this.Controls.Add(this.btnEditProfile);
            this.Controls.Add(this.btnAddProfile);
            this.Controls.Add(this.listViewAvatarProfiles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProfileManagerForm";
            this.Text = "Profile Manager";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listViewAvatarProfiles;
        private System.Windows.Forms.ColumnHeader colProfileName;
        private System.Windows.Forms.ColumnHeader colHotkey;
        private System.Windows.Forms.ColumnHeader colImagePath;
        private System.Windows.Forms.Button btnAddProfile;
        private System.Windows.Forms.Button btnEditProfile;
        private System.Windows.Forms.Button btnRemoveProfile;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnSetGlobalHideShowHotkey;
        private System.Windows.Forms.TextBox txtGlobalHideShowHotkey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkStartMinimized;
        private System.Windows.Forms.Button butClose;
        private System.Windows.Forms.Button btnSaveSettings;
    }
}
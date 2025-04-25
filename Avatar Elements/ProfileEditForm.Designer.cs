
namespace Avatar_Elements {
    partial class ProfileEditForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfileEditForm));
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtProfileName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtBaseImagePath = new System.Windows.Forms.TextBox();
            this.btnBrowseBaseImage = new System.Windows.Forms.Button();
            this.btnBrowseDepthMap = new System.Windows.Forms.Button();
            this.txtDepthMapPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.picBaseImagePreview = new System.Windows.Forms.PictureBox();
            this.picDepthMapPreview = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lblValidationStatus = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtProfileHotkey = new System.Windows.Forms.TextBox();
            this.btnSetProfileHotkey = new System.Windows.Forms.Button();
            this.btnClearProfileHotkey = new System.Windows.Forms.Button();
            this.labelDepthScale = new System.Windows.Forms.Label();
            this.numDepthScale = new System.Windows.Forms.NumericUpDown();
            this.labelSpecularIntensity = new System.Windows.Forms.Label();
            this.numSpecularIntensity = new System.Windows.Forms.NumericUpDown();
            this.labelSpecularPower = new System.Windows.Forms.Label();
            this.numSpecularPower = new System.Windows.Forms.NumericUpDown();
            this.btnEditAnimations = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picBaseImagePreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDepthMapPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDepthScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSpecularIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSpecularPower)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(334, 334);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(12, 334);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 23);
            this.label1.TabIndex = 2;
            this.label1.Text = "Profile Name :";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtProfileName
            // 
            this.txtProfileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProfileName.Location = new System.Drawing.Point(138, 11);
            this.txtProfileName.Name = "txtProfileName";
            this.txtProfileName.Size = new System.Drawing.Size(271, 20);
            this.txtProfileName.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(15, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(116, 23);
            this.label2.TabIndex = 4;
            this.label2.Text = "Base Avatar Image :";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtBaseImagePath
            // 
            this.txtBaseImagePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBaseImagePath.Location = new System.Drawing.Point(138, 39);
            this.txtBaseImagePath.Name = "txtBaseImagePath";
            this.txtBaseImagePath.ReadOnly = true;
            this.txtBaseImagePath.Size = new System.Drawing.Size(219, 20);
            this.txtBaseImagePath.TabIndex = 5;
            // 
            // btnBrowseBaseImage
            // 
            this.btnBrowseBaseImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseBaseImage.Location = new System.Drawing.Point(363, 37);
            this.btnBrowseBaseImage.Name = "btnBrowseBaseImage";
            this.btnBrowseBaseImage.Size = new System.Drawing.Size(46, 23);
            this.btnBrowseBaseImage.TabIndex = 6;
            this.btnBrowseBaseImage.Text = "...";
            this.btnBrowseBaseImage.UseVisualStyleBackColor = true;
            // 
            // btnBrowseDepthMap
            // 
            this.btnBrowseDepthMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseDepthMap.Location = new System.Drawing.Point(363, 63);
            this.btnBrowseDepthMap.Name = "btnBrowseDepthMap";
            this.btnBrowseDepthMap.Size = new System.Drawing.Size(46, 23);
            this.btnBrowseDepthMap.TabIndex = 9;
            this.btnBrowseDepthMap.Text = "...";
            this.btnBrowseDepthMap.UseVisualStyleBackColor = true;
            // 
            // txtDepthMapPath
            // 
            this.txtDepthMapPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDepthMapPath.Location = new System.Drawing.Point(138, 65);
            this.txtDepthMapPath.Name = "txtDepthMapPath";
            this.txtDepthMapPath.ReadOnly = true;
            this.txtDepthMapPath.Size = new System.Drawing.Size(219, 20);
            this.txtDepthMapPath.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(15, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(116, 23);
            this.label3.TabIndex = 7;
            this.label3.Text = "Base Avatar Image :";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(104, 201);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 23);
            this.label4.TabIndex = 10;
            this.label4.Text = "Base Image";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // picBaseImagePreview
            // 
            this.picBaseImagePreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.picBaseImagePreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picBaseImagePreview.Location = new System.Drawing.Point(107, 227);
            this.picBaseImagePreview.Name = "picBaseImagePreview";
            this.picBaseImagePreview.Size = new System.Drawing.Size(97, 97);
            this.picBaseImagePreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picBaseImagePreview.TabIndex = 11;
            this.picBaseImagePreview.TabStop = false;
            // 
            // picDepthMapPreview
            // 
            this.picDepthMapPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.picDepthMapPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picDepthMapPreview.Location = new System.Drawing.Point(223, 227);
            this.picDepthMapPreview.Name = "picDepthMapPreview";
            this.picDepthMapPreview.Size = new System.Drawing.Size(97, 97);
            this.picDepthMapPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picDepthMapPreview.TabIndex = 13;
            this.picDepthMapPreview.TabStop = false;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(220, 201);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 23);
            this.label5.TabIndex = 12;
            this.label5.Text = "Depth Image";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblValidationStatus
            // 
            this.lblValidationStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValidationStatus.Location = new System.Drawing.Point(93, 415);
            this.lblValidationStatus.Name = "lblValidationStatus";
            this.lblValidationStatus.Size = new System.Drawing.Size(235, 23);
            this.lblValidationStatus.TabIndex = 14;
            this.lblValidationStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(15, 89);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(116, 23);
            this.label6.TabIndex = 15;
            this.label6.Text = "Profile Hotkey :";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtProfileHotkey
            // 
            this.txtProfileHotkey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProfileHotkey.Location = new System.Drawing.Point(138, 91);
            this.txtProfileHotkey.Name = "txtProfileHotkey";
            this.txtProfileHotkey.ReadOnly = true;
            this.txtProfileHotkey.Size = new System.Drawing.Size(167, 20);
            this.txtProfileHotkey.TabIndex = 16;
            // 
            // btnSetProfileHotkey
            // 
            this.btnSetProfileHotkey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetProfileHotkey.Location = new System.Drawing.Point(311, 89);
            this.btnSetProfileHotkey.Name = "btnSetProfileHotkey";
            this.btnSetProfileHotkey.Size = new System.Drawing.Size(46, 23);
            this.btnSetProfileHotkey.TabIndex = 17;
            this.btnSetProfileHotkey.Text = "Set...";
            this.btnSetProfileHotkey.UseVisualStyleBackColor = true;
            // 
            // btnClearProfileHotkey
            // 
            this.btnClearProfileHotkey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearProfileHotkey.Location = new System.Drawing.Point(363, 89);
            this.btnClearProfileHotkey.Name = "btnClearProfileHotkey";
            this.btnClearProfileHotkey.Size = new System.Drawing.Size(46, 23);
            this.btnClearProfileHotkey.TabIndex = 19;
            this.btnClearProfileHotkey.Text = "Clear";
            this.btnClearProfileHotkey.UseVisualStyleBackColor = true;
            // 
            // labelDepthScale
            // 
            this.labelDepthScale.Location = new System.Drawing.Point(15, 114);
            this.labelDepthScale.Name = "labelDepthScale";
            this.labelDepthScale.Size = new System.Drawing.Size(116, 23);
            this.labelDepthScale.TabIndex = 20;
            this.labelDepthScale.Text = "Depth Scale :";
            this.labelDepthScale.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numDepthScale
            // 
            this.numDepthScale.DecimalPlaces = 2;
            this.numDepthScale.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numDepthScale.Location = new System.Drawing.Point(138, 117);
            this.numDepthScale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numDepthScale.Name = "numDepthScale";
            this.numDepthScale.Size = new System.Drawing.Size(120, 20);
            this.numDepthScale.TabIndex = 21;
            this.numDepthScale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // labelSpecularIntensity
            // 
            this.labelSpecularIntensity.Location = new System.Drawing.Point(15, 140);
            this.labelSpecularIntensity.Name = "labelSpecularIntensity";
            this.labelSpecularIntensity.Size = new System.Drawing.Size(116, 23);
            this.labelSpecularIntensity.TabIndex = 22;
            this.labelSpecularIntensity.Text = "Specular Intensity :";
            this.labelSpecularIntensity.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numSpecularIntensity
            // 
            this.numSpecularIntensity.DecimalPlaces = 2;
            this.numSpecularIntensity.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numSpecularIntensity.Location = new System.Drawing.Point(137, 143);
            this.numSpecularIntensity.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numSpecularIntensity.Name = "numSpecularIntensity";
            this.numSpecularIntensity.Size = new System.Drawing.Size(120, 20);
            this.numSpecularIntensity.TabIndex = 23;
            this.numSpecularIntensity.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // labelSpecularPower
            // 
            this.labelSpecularPower.Location = new System.Drawing.Point(15, 166);
            this.labelSpecularPower.Name = "labelSpecularPower";
            this.labelSpecularPower.Size = new System.Drawing.Size(116, 23);
            this.labelSpecularPower.TabIndex = 24;
            this.labelSpecularPower.Text = "Specular Power :";
            this.labelSpecularPower.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numSpecularPower
            // 
            this.numSpecularPower.DecimalPlaces = 1;
            this.numSpecularPower.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numSpecularPower.Location = new System.Drawing.Point(138, 169);
            this.numSpecularPower.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numSpecularPower.Name = "numSpecularPower";
            this.numSpecularPower.Size = new System.Drawing.Size(120, 20);
            this.numSpecularPower.TabIndex = 25;
            this.numSpecularPower.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
            // 
            // btnEditAnimations
            // 
            this.btnEditAnimations.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnEditAnimations.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnEditAnimations.Location = new System.Drawing.Point(164, 334);
            this.btnEditAnimations.Name = "btnEditAnimations";
            this.btnEditAnimations.Size = new System.Drawing.Size(94, 23);
            this.btnEditAnimations.TabIndex = 26;
            this.btnEditAnimations.Text = "Animations...";
            this.btnEditAnimations.UseVisualStyleBackColor = true;
            // 
            // ProfileEditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(421, 369);
            this.Controls.Add(this.btnEditAnimations);
            this.Controls.Add(this.numSpecularPower);
            this.Controls.Add(this.labelSpecularPower);
            this.Controls.Add(this.numSpecularIntensity);
            this.Controls.Add(this.labelSpecularIntensity);
            this.Controls.Add(this.numDepthScale);
            this.Controls.Add(this.labelDepthScale);
            this.Controls.Add(this.btnClearProfileHotkey);
            this.Controls.Add(this.btnSetProfileHotkey);
            this.Controls.Add(this.txtProfileHotkey);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblValidationStatus);
            this.Controls.Add(this.picDepthMapPreview);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.picBaseImagePreview);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnBrowseDepthMap);
            this.Controls.Add(this.txtDepthMapPath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnBrowseBaseImage);
            this.Controls.Add(this.txtBaseImagePath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtProfileName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProfileEditForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit Profile";
            ((System.ComponentModel.ISupportInitialize)(this.picBaseImagePreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDepthMapPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDepthScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSpecularIntensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSpecularPower)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtProfileName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtBaseImagePath;
        private System.Windows.Forms.Button btnBrowseBaseImage;
        private System.Windows.Forms.Button btnBrowseDepthMap;
        private System.Windows.Forms.TextBox txtDepthMapPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.PictureBox picBaseImagePreview;
        private System.Windows.Forms.PictureBox picDepthMapPreview;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblValidationStatus;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtProfileHotkey;
        private System.Windows.Forms.Button btnSetProfileHotkey;
        private System.Windows.Forms.Button btnClearProfileHotkey;
        private System.Windows.Forms.Label labelDepthScale;
        private System.Windows.Forms.NumericUpDown numDepthScale;
        private System.Windows.Forms.Label labelSpecularIntensity;
        private System.Windows.Forms.NumericUpDown numSpecularIntensity;
        private System.Windows.Forms.Label labelSpecularPower;
        private System.Windows.Forms.NumericUpDown numSpecularPower;
        private System.Windows.Forms.Button btnEditAnimations;
    }
}
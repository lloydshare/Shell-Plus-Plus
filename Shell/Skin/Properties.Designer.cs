namespace CustomForm
{
    partial class Properties
    {
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
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            lRadius = new Label();
            nRadius = new NumericUpDown();
            checkRound = new CheckBox();
            checkDrop = new CheckBox();
            checkThemeButton = new CheckBox();
            groupBox1 = new GroupBox();
            radioLight = new RadioButton();
            radioDark = new RadioButton();
            tabPage2 = new TabPage();
            label4 = new Label();
            cbLayout = new ComboBox();
            PictureFileName = new Label();
            lbOpacity = new Label();
            cbPic = new CheckBox();
            buttonSelectPic = new Button();
            label3 = new Label();
            tbOpacity = new TrackBar();
            groupBox3 = new GroupBox();
            lbOpacityLight = new Label();
            lbOpacityDark = new Label();
            label2 = new Label();
            label1 = new Label();
            tbOpacityLight = new TrackBar();
            tbOpacityDark = new TrackBar();
            checkBGBenabled = new CheckBox();
            checkBBar = new CheckBox();
            tabPage3 = new TabPage();
            groupBox4 = new GroupBox();
            label13 = new Label();
            label12 = new Label();
            nLeft = new NumericUpDown();
            nTop = new NumericUpDown();
            groupBox2 = new GroupBox();
            label11 = new Label();
            label10 = new Label();
            nHeight = new NumericUpDown();
            nWidth = new NumericUpDown();
            tabPage4 = new TabPage();
            tabControl2 = new TabControl();
            tabPage5 = new TabPage();
            tabPage6 = new TabPage();
            previewBox = new PictureBox();
            buttonSave = new Button();
            buttonCancel = new Button();
            selectPictureDialog = new OpenFileDialog();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nRadius).BeginInit();
            groupBox1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tbOpacity).BeginInit();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tbOpacityLight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tbOpacityDark).BeginInit();
            tabPage3.SuspendLayout();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nLeft).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nTop).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nHeight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nWidth).BeginInit();
            tabPage4.SuspendLayout();
            tabControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)previewBox).BeginInit();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Location = new Point(10, 29);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(384, 256);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(lRadius);
            tabPage1.Controls.Add(nRadius);
            tabPage1.Controls.Add(checkRound);
            tabPage1.Controls.Add(checkDrop);
            tabPage1.Controls.Add(checkThemeButton);
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(376, 228);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Options";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // lRadius
            // 
            lRadius.AutoSize = true;
            lRadius.Enabled = false;
            lRadius.Location = new Point(133, 116);
            lRadius.Name = "lRadius";
            lRadius.Size = new Size(45, 15);
            lRadius.TabIndex = 8;
            lRadius.Text = "Radius:";
            // 
            // nRadius
            // 
            nRadius.BorderStyle = BorderStyle.FixedSingle;
            nRadius.Enabled = false;
            nRadius.Location = new Point(194, 112);
            nRadius.Name = "nRadius";
            nRadius.Size = new Size(44, 23);
            nRadius.TabIndex = 7;
            // 
            // checkRound
            // 
            checkRound.AutoSize = true;
            checkRound.FlatStyle = FlatStyle.Flat;
            checkRound.Location = new Point(19, 116);
            checkRound.Name = "checkRound";
            checkRound.Size = new Size(105, 19);
            checkRound.TabIndex = 5;
            checkRound.Text = "Rounded Edges";
            checkRound.UseVisualStyleBackColor = true;
            checkRound.CheckedChanged += checkRound_CheckedChanged;
            // 
            // checkDrop
            // 
            checkDrop.AutoSize = true;
            checkDrop.FlatStyle = FlatStyle.Flat;
            checkDrop.Location = new Point(139, 64);
            checkDrop.Name = "checkDrop";
            checkDrop.Size = new Size(112, 19);
            checkDrop.TabIndex = 4;
            checkDrop.Text = "Window Shadow";
            checkDrop.UseVisualStyleBackColor = true;
            // 
            // checkThemeButton
            // 
            checkThemeButton.AutoSize = true;
            checkThemeButton.FlatStyle = FlatStyle.Flat;
            checkThemeButton.Location = new Point(139, 39);
            checkThemeButton.Name = "checkThemeButton";
            checkThemeButton.Size = new Size(98, 19);
            checkThemeButton.TabIndex = 1;
            checkThemeButton.Text = "Theme Button";
            checkThemeButton.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(radioLight);
            groupBox1.Controls.Add(radioDark);
            groupBox1.FlatStyle = FlatStyle.Flat;
            groupBox1.Location = new Point(19, 16);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(105, 85);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Theme";
            // 
            // radioLight
            // 
            radioLight.AutoSize = true;
            radioLight.Location = new Point(19, 47);
            radioLight.Name = "radioLight";
            radioLight.Size = new Size(52, 19);
            radioLight.TabIndex = 1;
            radioLight.TabStop = true;
            radioLight.Tag = "theme";
            radioLight.Text = "Light";
            radioLight.UseVisualStyleBackColor = true;
            // 
            // radioDark
            // 
            radioDark.AutoSize = true;
            radioDark.Location = new Point(19, 22);
            radioDark.Name = "radioDark";
            radioDark.Size = new Size(49, 19);
            radioDark.TabIndex = 0;
            radioDark.TabStop = true;
            radioDark.Tag = "theme";
            radioDark.Text = "Dark";
            radioDark.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(label4);
            tabPage2.Controls.Add(cbLayout);
            tabPage2.Controls.Add(PictureFileName);
            tabPage2.Controls.Add(lbOpacity);
            tabPage2.Controls.Add(cbPic);
            tabPage2.Controls.Add(buttonSelectPic);
            tabPage2.Controls.Add(label3);
            tabPage2.Controls.Add(tbOpacity);
            tabPage2.Controls.Add(groupBox3);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(376, 228);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Background";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(168, 188);
            label4.Name = "label4";
            label4.Size = new Size(46, 15);
            label4.TabIndex = 15;
            label4.Text = "Layout:";
            // 
            // cbLayout
            // 
            cbLayout.DisplayMember = "Tile";
            cbLayout.Enabled = false;
            cbLayout.FormattingEnabled = true;
            cbLayout.Items.AddRange(new object[] { "None", "Tile", "Center", "Stretch", "Zoom" });
            cbLayout.Location = new Point(220, 185);
            cbLayout.Name = "cbLayout";
            cbLayout.Size = new Size(121, 23);
            cbLayout.TabIndex = 14;
            // 
            // PictureFileName
            // 
            PictureFileName.AutoSize = true;
            PictureFileName.Location = new Point(22, 264);
            PictureFileName.Name = "PictureFileName";
            PictureFileName.Size = new Size(0, 15);
            PictureFileName.TabIndex = 13;
            // 
            // lbOpacity
            // 
            lbOpacity.AutoSize = true;
            lbOpacity.Location = new Point(275, 167);
            lbOpacity.Name = "lbOpacity";
            lbOpacity.Size = new Size(17, 15);
            lbOpacity.TabIndex = 12;
            lbOpacity.Text = "%";
            // 
            // cbPic
            // 
            cbPic.AutoSize = true;
            cbPic.Location = new Point(18, 187);
            cbPic.Name = "cbPic";
            cbPic.Size = new Size(63, 19);
            cbPic.TabIndex = 11;
            cbPic.Text = "Picture";
            cbPic.UseVisualStyleBackColor = true;
            cbPic.CheckedChanged += cbPic_CheckedChanged;
            // 
            // buttonSelectPic
            // 
            buttonSelectPic.Enabled = false;
            buttonSelectPic.Location = new Point(87, 185);
            buttonSelectPic.Name = "buttonSelectPic";
            buttonSelectPic.Size = new Size(75, 23);
            buttonSelectPic.TabIndex = 10;
            buttonSelectPic.Text = "Select";
            buttonSelectPic.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(11, 144);
            label3.Name = "label3";
            label3.Size = new Size(51, 15);
            label3.TabIndex = 9;
            label3.Text = "Opacity:";
            // 
            // tbOpacity
            // 
            tbOpacity.Location = new Point(68, 144);
            tbOpacity.Name = "tbOpacity";
            tbOpacity.Size = new Size(259, 45);
            tbOpacity.TabIndex = 8;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(lbOpacityLight);
            groupBox3.Controls.Add(lbOpacityDark);
            groupBox3.Controls.Add(label2);
            groupBox3.Controls.Add(label1);
            groupBox3.Controls.Add(tbOpacityLight);
            groupBox3.Controls.Add(tbOpacityDark);
            groupBox3.Controls.Add(checkBGBenabled);
            groupBox3.Controls.Add(checkBBar);
            groupBox3.Location = new Point(6, 6);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(364, 132);
            groupBox3.TabIndex = 4;
            groupBox3.TabStop = false;
            groupBox3.Text = "Background Blur";
            // 
            // lbOpacityLight
            // 
            lbOpacityLight.AutoSize = true;
            lbOpacityLight.Location = new Point(269, 116);
            lbOpacityLight.Name = "lbOpacityLight";
            lbOpacityLight.Size = new Size(17, 15);
            lbOpacityLight.TabIndex = 9;
            lbOpacityLight.Text = "%";
            // 
            // lbOpacityDark
            // 
            lbOpacityDark.AutoSize = true;
            lbOpacityDark.Location = new Point(268, 78);
            lbOpacityDark.Name = "lbOpacityDark";
            lbOpacityDark.Size = new Size(17, 15);
            lbOpacityDark.TabIndex = 8;
            lbOpacityDark.Text = "%";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(16, 91);
            label2.Name = "label2";
            label2.Size = new Size(76, 15);
            label2.TabIndex = 7;
            label2.Text = "Light Theme:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(16, 59);
            label1.Name = "label1";
            label1.Size = new Size(73, 15);
            label1.TabIndex = 6;
            label1.Text = "Dark Theme:";
            // 
            // tbOpacityLight
            // 
            tbOpacityLight.Location = new Point(98, 88);
            tbOpacityLight.Name = "tbOpacityLight";
            tbOpacityLight.Size = new Size(164, 45);
            tbOpacityLight.TabIndex = 5;
            // 
            // tbOpacityDark
            // 
            tbOpacityDark.Location = new Point(98, 59);
            tbOpacityDark.Name = "tbOpacityDark";
            tbOpacityDark.Size = new Size(164, 45);
            tbOpacityDark.TabIndex = 4;
            // 
            // checkBGBenabled
            // 
            checkBGBenabled.AutoSize = true;
            checkBGBenabled.Location = new Point(18, 22);
            checkBGBenabled.Name = "checkBGBenabled";
            checkBGBenabled.Size = new Size(68, 19);
            checkBGBenabled.TabIndex = 2;
            checkBGBenabled.Text = "Enabled";
            checkBGBenabled.UseVisualStyleBackColor = true;
            // 
            // checkBBar
            // 
            checkBBar.AutoSize = true;
            checkBBar.Location = new Point(116, 22);
            checkBBar.Name = "checkBBar";
            checkBBar.Size = new Size(92, 19);
            checkBBar.TabIndex = 3;
            checkBBar.Text = "Blur Title Bar";
            checkBBar.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(groupBox4);
            tabPage3.Controls.Add(groupBox2);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(376, 228);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Layout";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(label13);
            groupBox4.Controls.Add(label12);
            groupBox4.Controls.Add(nLeft);
            groupBox4.Controls.Add(nTop);
            groupBox4.Location = new Point(3, 112);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(200, 100);
            groupBox4.TabIndex = 1;
            groupBox4.TabStop = false;
            groupBox4.Text = "Window Position";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(14, 64);
            label13.Name = "label13";
            label13.Size = new Size(30, 15);
            label13.TabIndex = 4;
            label13.Text = "Left:";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(14, 35);
            label12.Name = "label12";
            label12.Size = new Size(29, 15);
            label12.TabIndex = 3;
            label12.Text = "Top:";
            // 
            // nLeft
            // 
            nLeft.Location = new Point(93, 62);
            nLeft.Name = "nLeft";
            nLeft.Size = new Size(81, 23);
            nLeft.TabIndex = 2;
            // 
            // nTop
            // 
            nTop.Location = new Point(93, 33);
            nTop.Name = "nTop";
            nTop.Size = new Size(81, 23);
            nTop.TabIndex = 1;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(nHeight);
            groupBox2.Controls.Add(nWidth);
            groupBox2.Location = new Point(6, 6);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(197, 100);
            groupBox2.TabIndex = 0;
            groupBox2.TabStop = false;
            groupBox2.Text = "Window Size";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(11, 68);
            label11.Name = "label11";
            label11.Size = new Size(46, 15);
            label11.TabIndex = 3;
            label11.Text = "Height:";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(11, 33);
            label10.Name = "label10";
            label10.Size = new Size(42, 15);
            label10.TabIndex = 2;
            label10.Text = "Width:";
            // 
            // nHeight
            // 
            nHeight.Location = new Point(90, 60);
            nHeight.Name = "nHeight";
            nHeight.Size = new Size(81, 23);
            nHeight.TabIndex = 1;
            // 
            // nWidth
            // 
            nWidth.Location = new Point(90, 31);
            nWidth.Name = "nWidth";
            nWidth.Size = new Size(81, 23);
            nWidth.TabIndex = 0;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(tabControl2);
            tabPage4.Location = new Point(4, 24);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(376, 228);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Theme";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(tabPage5);
            tabControl2.Controls.Add(tabPage6);
            tabControl2.Dock = DockStyle.Fill;
            tabControl2.Location = new Point(3, 3);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(370, 222);
            tabControl2.TabIndex = 0;
            // 
            // tabPage5
            // 
            tabPage5.Location = new Point(4, 24);
            tabPage5.Name = "tabPage5";
            tabPage5.Padding = new Padding(3);
            tabPage5.Size = new Size(362, 194);
            tabPage5.TabIndex = 0;
            tabPage5.Text = "Light";
            tabPage5.UseVisualStyleBackColor = true;
            // 
            // tabPage6
            // 
            tabPage6.Location = new Point(4, 24);
            tabPage6.Name = "tabPage6";
            tabPage6.Padding = new Padding(3);
            tabPage6.Size = new Size(362, 194);
            tabPage6.TabIndex = 1;
            tabPage6.Text = "Dark";
            tabPage6.UseVisualStyleBackColor = true;
            // 
            // previewBox
            // 
            previewBox.Location = new Point(12, 291);
            previewBox.Name = "previewBox";
            previewBox.Size = new Size(194, 94);
            previewBox.TabIndex = 3;
            previewBox.TabStop = false;
            // 
            // buttonSave
            // 
            buttonSave.FlatStyle = FlatStyle.Flat;
            buttonSave.Location = new Point(232, 366);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(75, 23);
            buttonSave.TabIndex = 1;
            buttonSave.Text = "Save";
            buttonSave.UseVisualStyleBackColor = true;
            buttonSave.Click += buttonSave_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.Location = new Point(313, 366);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.TabIndex = 2;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // Properties
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400, 407);
            Controls.Add(previewBox);
            Controls.Add(buttonCancel);
            Controls.Add(buttonSave);
            Controls.Add(tabControl1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Properties";
            ShowDropShadow = true;
            Text = "\"Custom Form Demo\" Properties";
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nRadius).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)tbOpacity).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)tbOpacityLight).EndInit();
            ((System.ComponentModel.ISupportInitialize)tbOpacityDark).EndInit();
            tabPage3.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nLeft).EndInit();
            ((System.ComponentModel.ISupportInitialize)nTop).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nHeight).EndInit();
            ((System.ComponentModel.ISupportInitialize)nWidth).EndInit();
            tabPage4.ResumeLayout(false);
            tabControl2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)previewBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private Button buttonSave;
        private Button buttonCancel;
        private GroupBox groupBox1;
        private RadioButton radioLight;
        private RadioButton radioDark;
        private CheckBox checkThemeButton;
        private CheckBox checkDrop;
        private CheckBox checkRound;
        private GroupBox groupBox3;
        private Label label1;
        private TrackBar tbOpacityLight;
        private TrackBar tbOpacityDark;
        private CheckBox checkBGBenabled;
        private CheckBox checkBBar;
        private Label label2;
        private Label label3;
        private TrackBar tbOpacity;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private Button buttonSelectPic;
        private CheckBox cbPic;
        private Label PictureFileName;
        private Label lbOpacity;
        private Label lbOpacityLight;
        private Label lbOpacityDark;
        private GroupBox groupBox4;
        private GroupBox groupBox2;
        private NumericUpDown nLeft;
        private NumericUpDown nTop;
        private NumericUpDown nHeight;
        private NumericUpDown nWidth;
        private Label label10;
        private Label label13;
        private Label label12;
        private Label label11;
        private PictureBox previewBox;
        private OpenFileDialog selectPictureDialog;
        private ComboBox cbLayout;
        private Label label4;
        private TabControl tabControl2;
        private TabPage tabPage5;
        private TabPage tabPage6;
        private NumericUpDown nRadius;
        private Label lRadius;
    }
}
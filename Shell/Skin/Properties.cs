#pragma warning disable CS0108 

namespace CustomForm
{
    public partial class Properties : Skin
    {
        private readonly SkinManager SkinManager;
        private readonly Skin MainForm;

        private System.Drawing.Image? _selectedBackgroundImage;
        private string? _selectedBackgroundPath;

        #region Constructor
        public Properties()
        {
            InitializeComponent();
            Sizable = false;
            SkinManager = SkinManager.Apply(this);
            MainForm = SkinManager.GetMainForm();

            // Set preview background to black
            previewBox.BackColor = System.Drawing.Color.Black;

            tbOpacity.ValueChanged += tbOpacity_ValueChanged;
            tbOpacity.Minimum = 10;
            tbOpacity.Maximum = 100;

            // Add for Dark/Light opacity trackbars
            tbOpacityDark.Minimum = 0;
            tbOpacityDark.Maximum = 100;
            tbOpacityLight.Minimum = 0;
            tbOpacityLight.Maximum = 100;

            tbOpacityDark.ValueChanged += tbOpacityDark_ValueChanged;
            tbOpacityLight.ValueChanged += tbOpacityLight_ValueChanged;

            // Layout tab setup
            nWidth.Minimum = 100; nWidth.Maximum = 10000; nWidth.Increment = 10;
            nHeight.Minimum = 100; nHeight.Maximum = 10000; nHeight.Increment = 10;
            nLeft.Minimum = 0; nLeft.Maximum = 20000; nLeft.Increment = 10;
            nTop.Minimum = 0; nTop.Maximum = 20000; nTop.Increment = 10;

            nWidth.ValueChanged += Layout_ValueChanged;
            nHeight.ValueChanged += Layout_ValueChanged;
            nLeft.ValueChanged += Layout_ValueChanged;
            nTop.ValueChanged += Layout_ValueChanged;
            previewBox.SizeChanged += (s, e) => UpdatePreview();

            // Also refresh the preview when related options change
            checkRound.CheckedChanged += (s, e) => UpdatePreview();
            checkBGBenabled.CheckedChanged += (s, e) => UpdatePreview();
            checkBBar.CheckedChanged += (s, e) => UpdatePreview();
            radioDark.CheckedChanged += (s, e) => UpdatePreview();
            radioLight.CheckedChanged += (s, e) => UpdatePreview();

            // Picture controls
            buttonSelectPic.Click += buttonSelectPic_Click;
            cbLayout.SelectedIndexChanged += (s, e) => { /* defer applying until Save */ };

            if (MainForm == null)
            {
                MessageBox.Show("Error");
            }
            else
            {
                BindProperties();
            }
        }
        #endregion

        #region Events
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveProperties();
            Close();
        }

        private void tbOpacity_ValueChanged(object sender, EventArgs e)
        {
            lbOpacity.Text = $"{tbOpacity.Value}%";
        }
        private void tbOpacityDark_ValueChanged(object sender, EventArgs e)
        {
            lbOpacityDark.Text = $"{tbOpacityDark.Value}%";
            UpdatePreview();
        }
        private void tbOpacityLight_ValueChanged(object sender, EventArgs e)
        {
            lbOpacityLight.Text = $"{tbOpacityLight.Value}%";
            UpdatePreview();
        }

        private void Layout_ValueChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void cbPic_CheckedChanged(object? sender, EventArgs e)
        {
            buttonSelectPic.Enabled = cbPic.Checked;
            cbLayout.Enabled = cbPic.Checked;

            if (cbPic.Checked)
            {
                if (cbLayout.SelectedIndex < 0)
                    cbLayout.SelectedIndex = 3; // Stretch by default
            }
            else
            {
                PictureFileName.Text = string.Empty;
            }
        }

        private void buttonSelectPic_Click(object? sender, EventArgs e)
        {
            selectPictureDialog.Title = "Select Background Picture";
            selectPictureDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*";
            selectPictureDialog.CheckFileExists = true;
            selectPictureDialog.Multiselect = false;
            if (selectPictureDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    // Dispose previous selection
                    _selectedBackgroundImage?.Dispose();

                    // Load without locking the file
                    using var fs = new System.IO.FileStream(selectPictureDialog.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                    using var temp = System.Drawing.Image.FromStream(fs);
                    _selectedBackgroundImage = new System.Drawing.Bitmap(temp);
                    _selectedBackgroundPath = selectPictureDialog.FileName;

                    PictureFileName.Text = System.IO.Path.GetFileName(_selectedBackgroundPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to load image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _selectedBackgroundImage = null;
                    _selectedBackgroundPath = null;
                    PictureFileName.Text = string.Empty;
                }
            }
        }
        #endregion

        #region Methods
        private void BindProperties()
        {
            if (SkinManager.Theme == SkinManager.Themes.DARK)
            {
                radioDark.Checked = true;
            }
            else
            {
                radioLight.Checked = true;
            }

            checkThemeButton.Checked = MainForm.ThemeButton;
            checkDrop.Checked = MainForm.ShowDropShadow;
            checkRound.Checked = MainForm.RoundedCorners;

            checkBGBenabled.Checked = MainForm.BlurredBackgound;
            checkBBar.Checked = MainForm.BlurredTitleBar;

            tbOpacity.Value = (int)(MainForm.Opacity * 100);
            lbOpacity.Text = $"{tbOpacity.Value}%";

            tbOpacityDark.Value = (int)(MainForm.BlurredOpacityDark / 255.0 * 100);
            lbOpacityDark.Text = $"{tbOpacityDark.Value}%";
            tbOpacityLight.Value = (int)(MainForm.BlurredOpacityLight / 255.0 * 100);
            lbOpacityLight.Text = $"{tbOpacityLight.Value}%";

            // Clamp and bind layout values
            nHeight.Value = Math.Max(nHeight.Minimum, Math.Min(nHeight.Maximum, MainForm.Height));
            nWidth.Value = Math.Max(nWidth.Minimum, Math.Min(nWidth.Maximum, MainForm.Width));
            nTop.Value = Math.Max(nTop.Minimum, Math.Min(nTop.Maximum, MainForm.Top));
            nLeft.Value = Math.Max(nLeft.Minimum, Math.Min(nLeft.Maximum, MainForm.Left));

            // Bind background image state
            var img = MainForm.BackgroundImage;
            cbPic.Checked = img != null;
            buttonSelectPic.Enabled = cbPic.Checked;
            cbLayout.Enabled = cbPic.Checked;
            if (img != null)
            {
                // Keep a clone for saving if user doesn't select a new one
                _selectedBackgroundImage?.Dispose();
                _selectedBackgroundImage = new System.Drawing.Bitmap(img);
                _selectedBackgroundPath = null; // unknown path
                PictureFileName.Text = "(current image)";

                cbLayout.SelectedIndex = (int)MainForm.BackgroundImageLayout;
            }
            else
            {
                PictureFileName.Text = string.Empty;
                if (cbLayout.SelectedIndex < 0) cbLayout.SelectedIndex = 0;
            }

            nRadius.Value = Math.Max(nRadius.Minimum, Math.Min(nRadius.Maximum, MainForm.CornerRadius));

            UpdatePreview();
            Update();
        }

        private void Update()
        {
            if (checkRound.Checked == true)
            {
                nRadius.Enabled = true;
                lRadius.Enabled = true;
            }
            else
            {
                nRadius.Enabled = false;
                lRadius.Enabled = false;
            }
        }

        private void SaveProperties()
        {
            if (radioDark.Checked == true)
            {
                SkinManager.Theme = SkinManager.Themes.DARK;
            }
            if (radioLight.Checked == true)
            {
                SkinManager.Theme = SkinManager.Themes.LIGHT;
            }

            MainForm.ThemeButton = checkThemeButton.Checked;
            MainForm.ShowDropShadow = checkDrop.Checked;
            MainForm.RoundedCorners = checkRound.Checked;

            MainForm.BlurredBackgound = checkBGBenabled.Checked;
            MainForm.BlurredTitleBar = checkBBar.Checked;

            MainForm.Opacity = tbOpacity.Value / 100.0;
            MainForm.BlurredOpacityDark = (byte)(tbOpacityDark.Value / 100.0 * 255);
            MainForm.BlurredOpacityLight = (byte)(tbOpacityLight.Value / 100.0 * 255);

            MainForm.CornerRadius = (int)nRadius.Value;

            // Apply layout
            try
            {
                if (MainForm.Maximized)
                {
                    MainForm.Maximized = false;
                }
                MainForm.Width = (int)nWidth.Value;
                MainForm.Height = (int)nHeight.Value;
                MainForm.Left = (int)nLeft.Value;
                MainForm.Top = (int)nTop.Value;
            }
            catch
            {
                // Ignore invalid assignments
            }

            // Apply background image
            if (cbPic.Checked)
            {
                // Apply selected image if any, else keep existing
                if (_selectedBackgroundImage != null)
                {
                    MainForm.BackgroundImage?.Dispose();
                    MainForm.BackgroundImage = new System.Drawing.Bitmap(_selectedBackgroundImage);
                }
                MainForm.BackgroundImageLayout = cbLayout.SelectedIndex >= 0
                    ? (System.Windows.Forms.ImageLayout)cbLayout.SelectedIndex
                    : System.Windows.Forms.ImageLayout.Stretch;
            }
            else
            {
                // Clear background image
                MainForm.BackgroundImage?.Dispose();
                MainForm.BackgroundImage = null;
            }
        }

        private void UpdatePreview()
        {
            if (previewBox == null || previewBox.Width <= 0 || previewBox.Height <= 0) return;

            int boxW = previewBox.Width;
            int boxH = previewBox.Height;
            var bmp = new System.Drawing.Bitmap(boxW, boxH);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                // Fill background as black (outside desktop)
                g.Clear(System.Drawing.Color.Black);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Determine a desktop surface (use virtual screen size)
                int desktopW = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                int desktopH = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                if (desktopW <= 0 || desktopH <= 0)
                {
                    desktopW = 1920; desktopH = 1080; // fallback
                }

                const int margin = 6; // margin inside preview
                float scale = System.MathF.Min((boxW - margin * 2f) / desktopW, (boxH - margin * 2f) / desktopH);
                int scaledDesktopW = System.Math.Max(1, (int)(desktopW * scale));
                int scaledDesktopH = System.Math.Max(1, (int)(desktopH * scale));
                int desktopX = (boxW - scaledDesktopW) / 2;
                int desktopY = (boxH - scaledDesktopH) / 2;

                // Removed drawing of desktop box to avoid visible rectangle
                // The preview background remains black.
                // var desktopRect = new System.Drawing.Rectangle(desktopX, desktopY, scaledDesktopW - 1, scaledDesktopH - 1);
                // using (var desktopFill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(24, 24, 24)))
                // using (var desktopBorder = new System.Drawing.Pen(System.Drawing.Color.FromArgb(64, 64, 64), 1))
                // {
                //     g.FillRectangle(desktopFill, desktopRect);
                //     g.DrawRectangle(desktopBorder, desktopRect);
                // }

                // Window size and position mapped into desktop
                int targetW = System.Math.Max(1, (int)nWidth.Value);
                int targetH = System.Math.Max(1, (int)nHeight.Value);
                int targetL = System.Math.Max(0, (int)nLeft.Value);
                int targetT = System.Math.Max(0, (int)nTop.Value);

                int rectW = System.Math.Max(1, (int)(targetW * scale));
                int rectH = System.Math.Max(1, (int)(targetH * scale));
                int x = desktopX + (int)(targetL * scale);
                int y = desktopY + (int)(targetT * scale);

                // Rounded corners radius in px
                int radius = checkRound.Checked ? System.Math.Max(3, (int)(10 * scale)) : 0;
                var rect = new System.Drawing.Rectangle(x, y, rectW - 1, rectH - 1);

                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                if (radius > 0)
                {
                    int d = radius * 2;
                    path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
                    path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
                    path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                    path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
                    path.CloseFigure();
                }
                else
                {
                    path.AddRectangle(rect);
                }

                // Base window fill and border
                using var fill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(240, 230, 230, 230));
                using var border = new System.Drawing.Pen(System.Drawing.Color.Black, 1);
                g.FillPath(fill, path);

                // Title bar preview
                int titlePx = System.Math.Max(8, (int)(28 * scale));
                var titleRect = new System.Drawing.Rectangle(x, y, rectW - 1, System.Math.Min(titlePx, rectH - 1));

                // Clip title bar drawing to the rounded window path to avoid square corners overlap
                var gs = g.Save();
                g.SetClip(path);
                if (checkBBar.Checked)
                {
                    // Simulate a translucent blurred title bar
                    using var title = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(120, 200, 200, 200));
                    g.FillRectangle(title, titleRect);
                }
                else
                {
                    using var title = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 200, 200, 200));
                    g.FillRectangle(title, titleRect);
                }
                g.Restore(gs);

                // Background blur overlay depending on theme and slider
                if (checkBGBenabled.Checked)
                {
                    bool isDark = radioDark.Checked;
                    int percent = isDark ? tbOpacityDark.Value : tbOpacityLight.Value;
                    int alpha = System.Math.Clamp((int)System.Math.Round(percent * 2.55), 0, 255);
                    var overlayColor = System.Drawing.Color.FromArgb(alpha, 0, 0, 0);
                    using var overlay = new System.Drawing.SolidBrush(overlayColor);
                    g.FillPath(overlay, path);
                }

                g.DrawPath(border, path);
            }

            previewBox.Image?.Dispose();
            previewBox.Image = bmp;
        }
        #endregion

        private void checkRound_CheckedChanged(object sender, EventArgs e)
        {
            Update();
        }
    }
}

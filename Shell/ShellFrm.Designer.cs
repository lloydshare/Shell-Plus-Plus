using Editor.Controls;

namespace Shell
{
    partial class ShellFrm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            shellBoxWithScrollBar1 = new ShellBoxWithScrollBar();
            openFileDialog1 = new OpenFileDialog();
            SuspendLayout();
            // 
            // shellBoxWithScrollBar1
            // 
            shellBoxWithScrollBar1.Dock = DockStyle.Fill;
            shellBoxWithScrollBar1.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            shellBoxWithScrollBar1.Location = new Point(3, 22);
            shellBoxWithScrollBar1.Margin = new Padding(2);
            shellBoxWithScrollBar1.Name = "shellBoxWithScrollBar1";
            shellBoxWithScrollBar1.Size = new Size(860, 432);
            shellBoxWithScrollBar1.TabIndex = 1;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // ShellFrm
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(866, 457);
            Controls.Add(shellBoxWithScrollBar1);
            Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            isMain = true;
            Name = "ShellFrm";
            Padding = new Padding(3, 22, 3, 3);
            Persistance = true;
            RoundedCorners = true;
            ShowDropShadow = true;
            Text = "Shell Demo";
            ThemeButton = false;
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        public ShellBoxWithScrollBar shellBoxWithScrollBar1;
        private OpenFileDialog openFileDialog1;
    }
}

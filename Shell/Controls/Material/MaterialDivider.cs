namespace MaterialSkin.Controls
{
    using CustomForm;
    using System.ComponentModel;
    using System.Windows.Forms;

    public sealed class MaterialDivider : Control, ISkin
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public SkinManager SkinManager => SkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        public MaterialDivider()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            Height = 1;
            BackColor = SkinManager.DividersColor;
        }
    }
}
namespace CustomForm
{
    // <summary>
    // Defines the<see cref="ISkin" />
    // </summary>

    public interface ISkin
    {
        // <summary>
        // Gets the SkinManager
        // </summary>
        CustomForm.SkinManager SkinManager { get; }

        ///// <summary>
        ///// Gets or sets the MouseState
        ///// </summary>
        //MouseState MouseState { get; set; }
    }

    ///// <summary>
    ///// Defines the MouseState
    ///// </summary>
    //public enum MouseState
    //{
    //    /// <summary>
    //    /// Defines the HOVER
    //    /// </summary>
    //    HOVER,

    //    /// <summary>
    //    /// Defines the DOWN
    //    /// </summary>
    //    DOWN,

    //    /// <summary>
    //    /// Defines the OUT
    //    /// </summary>
    //    OUT
    //}
}
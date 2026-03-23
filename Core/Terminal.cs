using System;
using System.Drawing;
using System.Windows.Forms;

public static class Terminal
{
    ////public static int WindowHeight = (AppDomain.CurrentDomain.GetData("WindowHeight") as Func<int>).Invoke();
    public static int WindowHeight
    {
        get
        {
            var getHeight = AppDomain.CurrentDomain.GetData("WindowHeight") as Func<int>;
            return getHeight?.Invoke() ?? 0;
        }
    }

    public static int WindowWidth = (AppDomain.CurrentDomain.GetData("WindowWidth") as Func<int>).Invoke();

    //public static int WindowHeight()
    //{
    //    var getHeight = AppDomain.CurrentDomain.GetData("WindowHeight") as Func<int>;
    //    return getHeight?.Invoke() ?? 0;
    //}

    public static void AddControl(Control control)
    {
        var addControl = AppDomain.CurrentDomain.GetData("AddControl") as Action<Control>;
        addControl?.Invoke(control);
    }


    public static void Clear()
    {
        (AppDomain.CurrentDomain.GetData("ClearConsole") as Action)?.Invoke();
    }

    public static Color GetConsoleTextColor()
    {
        var getColor = AppDomain.CurrentDomain.GetData("GetConsoleTextColor") as Func<Color>;
        return getColor?.Invoke() ?? Color.White; // default fallback
    }

    public static void SetConsoleTextColor(Color color)
    {
        var setColor = AppDomain.CurrentDomain.GetData("SetConsoleTextColor") as Action<Color>;
        setColor?.Invoke(color); 
    }

    public static void ClearLine()
    {
        ((Action)AppDomain.CurrentDomain.GetData("ClearLine"))?.Invoke();
    }

}
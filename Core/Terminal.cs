using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class Terminal
{
    private static readonly BlockingCollection<Action> _mainThreadQueue = new();

    // Start a dispatcher loop on the main thread
    static Terminal()
    {
        Task.Run(() =>
        {
            foreach (var action in _mainThreadQueue.GetConsumingEnumerable())
            {
                action();
            }
        });
    }

    // Helper to invoke on main thread and get result
    public static T InvokeOnMainThread<T>(Func<T> func)
    {
        if (Thread.CurrentThread.ManagedThreadId == 1) // Already on main thread
            return func();

        var tcs = new TaskCompletionSource<T>();
        _mainThreadQueue.Add(() =>
        {
            try { tcs.SetResult(func()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task.Result;
    }

    public static int WindowWidth = (AppDomain.CurrentDomain.GetData("WindowWidth") as Func<int>).Invoke();
    public static int WindowHeight = (AppDomain.CurrentDomain.GetData("WindowHeight") as Func<int>).Invoke();

    //public static void HideCuror()
    //{
    //    (AppDomain.CurrentDomain.GetData("HideCuror") as Action)?.Invoke();
    //}

    public static bool CursorVisible
    {
        get
        {
            return (AppDomain.CurrentDomain.GetData("GetCursorVisible") as Func<bool>).Invoke();
        }
        set
        {
            (AppDomain.CurrentDomain.GetData("SetCursorVisible") as Action<bool>).Invoke(value);
        }
    } 

    public static ConsoleColor ForegroundColor
    {
        get
        {
            return (AppDomain.CurrentDomain.GetData("GetConsoleColor") as Func<ConsoleColor>).Invoke();
        }
        set
        {
            (AppDomain.CurrentDomain.GetData("SetConsoleColor") as Action<ConsoleColor>).Invoke(value);
        }
    }

    public static void Clear()
    {
        (AppDomain.CurrentDomain.GetData("ClearConsole") as Action)?.Invoke();
    }

    public static Action<bool> SetScrollBarVisible;
    public static Func<bool> GetScrollBarVisible;

    /// <summary>
    /// Locks the console, or the gets current lock status
    /// </summary>
    public static bool ConsoleLock
    {
        get
        {
            return GetScrollBarVisible.Invoke();
        }
        set
        {
            SetScrollBarVisible?.Invoke(!value);
        }
        
        //push up the text and lock scrolling?
        //temp screen of some sort?
        //resize font mode thingy on
    }

    //public static void ConsoleUnLock()
    //{
    //    SetScrollBarVisible?.Invoke(true);
    //    //unlock scrolling and remove the pushed up text?
    //    //resize font mode thingy off
    //}

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

    public static void ProcessMainThreadQueue()
    {
        while (_mainThreadQueue.TryTake(out var action, TimeSpan.Zero))
        {
            action();
        }
    }

    public static void AddControl(Control control)
    {
        var addControl = AppDomain.CurrentDomain.GetData("AddControl") as Action<Control>;
        addControl?.Invoke(control);
    }
}

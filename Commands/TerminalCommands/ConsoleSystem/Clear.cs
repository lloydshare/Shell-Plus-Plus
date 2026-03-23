using System;
using System.Windows.Forms;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     * Clears the current console window.
     */
    public class Clear : ITerminalCommand
    {
        public string Name => "clear";

        public void Execute(string args)
        {
            Terminal.Clear();
        }
    }
}

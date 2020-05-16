using CSScript.Properties;
using System;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет реализацию консольного интерфейса программы.
    /// </summary>
    internal class ConsoleProgram : ProgramBase
    {
        public ConsoleProgram(ProgramModel programModel) : base(programModel)
        {
            ProgramModel.MessageManager.MessageAdded += MessageManager_MessageAdded;
            ProgramModel.FinishedEvent += ProgramModel_FinishedEvent;
        }

        private void ProgramModel_FinishedEvent(object sender, bool autoClose)
        {
            if (!autoClose)
            {
                Console.ReadKey();
            }
        }

        private void MessageManager_MessageAdded(object sender, Message message)
        {
            Debug.Write(message.Text);

            ConsoleColor? foreColor = GetConsoleColor(message.ForeColor);
            foreColor = foreColor ?? GetConsoleColor(Settings.Default.ForeColor);
            if (foreColor != null)
            {
                ConsoleColor stockColor = Console.ForegroundColor;
                Console.ForegroundColor = foreColor.Value;
                Console.Write(message.Text);
                Console.ForegroundColor = stockColor;
            }
            else
            {
                Console.Write(message.Text);
            }
        }

        protected override void StartProgram()
        {
            ConsoleColor? backColor = GetConsoleColor(Settings.Default.BackColor);
            if (backColor != null)
            {
                Console.BackgroundColor = backColor.Value;
                Console.Clear();
            }

            try
            {
                ProgramModel.StartAsync().Join();
            }
            finally
            {
                if (ProgramModel != null)
                {
                    Environment.ExitCode = ProgramModel.ExitCode;
                }
                ProgramModel?.Dispose();
            }
        }

        private ConsoleColor? GetConsoleColor(Color? color)
        {
            if (color == null)
            {
                return null;
            }
            else if (color == Color.Black)
            {
                return ConsoleColor.Black;
            }
            else if (color == Color.DarkBlue)
            {
                return ConsoleColor.DarkBlue;
            }
            else if (color == Color.DarkGreen)
            {
                return ConsoleColor.DarkGreen;
            }
            else if (color == Color.DarkCyan)
            {
                return ConsoleColor.DarkCyan;
            }
            else if (color == Color.DarkRed)
            {
                return ConsoleColor.DarkRed;
            }
            else if (color == Color.DarkMagenta)
            {
                return ConsoleColor.DarkMagenta;
            }
            //else if (color == Color.DarkYellow)
            //{
            //    return ConsoleColor.DarkYellow;
            //}
            else if (color == Color.Gray)
            {
                return ConsoleColor.Gray;
            }
            else if (color == Color.Blue)
            {
                return ConsoleColor.Blue;
            }
            else if (color == Color.Green)
            {
                return ConsoleColor.Green;
            }
            else if (color == Color.Cyan)
            {
                return ConsoleColor.Cyan;
            }
            else if (color == Color.Red)
            {
                return ConsoleColor.Red;
            }
            else if (color == Color.Magenta)
            {
                return ConsoleColor.Magenta;
            }
            else if (color == Color.Yellow)
            {
                return ConsoleColor.Yellow;
            }
            else if (color == Color.White)
            {
                return ConsoleColor.White;
            }
            return null;
        }
    }
}

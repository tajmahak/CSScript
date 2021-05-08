using System;

namespace CSScript.Core
{
    public class LogFragment
    {
        public string Text { get; }
        public ConsoleColor Color { get; }

        public LogFragment(string text, ConsoleColor color) {
            Text = text;
            Color = color;
        }
    }
}

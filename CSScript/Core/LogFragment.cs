using System;

namespace CSScript.Core
{
    public class LogFragment
    {
        public string Text { get; private set; }
        public ConsoleColor Color { get; private set; }

        public LogFragment(string text, ConsoleColor color) {
            Text = text;
            Color = color;
        }
    }
}

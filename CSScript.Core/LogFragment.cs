using System;

namespace CSScript.Core
{
    public class LogFragment
    {
        public string Text { get; }
        public ConsoleColor Color { get; }
        public bool IsError { get; }

        public LogFragment(string text, ConsoleColor color, bool isError) {
            Text = text;
            Color = color;
            IsError = isError;
        }
    }
}

using System;

namespace CSScript.Core
{
    public class Message
    {
        public string Text { get; private set; }
        public ConsoleColor ForeColor { get; private set; }

        public Message(string text, ConsoleColor foreColor)
        {
            Text = text;
            ForeColor = foreColor;
        }
    }
}

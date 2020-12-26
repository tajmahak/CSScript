using System;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет информационное сообщение.
    /// </summary>
    public class Message
    {
        public string Text { get; private set; }

        public DateTime DateTime { get; private set; }

        public ConsoleColor ForeColor { get; private set; }


        public Message(string text, DateTime dateTime, ConsoleColor foreColor)
        {
            Text = text;
            DateTime = dateTime;
            ForeColor = foreColor;
        }
    }
}

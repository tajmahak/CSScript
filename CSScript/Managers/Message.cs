using System;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет информационное сообщение.
    /// </summary>
    public class Message
    {
        public string Text { get; private set; }

        public DateTime DateTime { get; private set; }

        public Color? ForeColor { get; private set; }

        public Message(string text, DateTime dateTime, Color? foreColor)
        {
            Text = text;
            DateTime = dateTime;
            ForeColor = foreColor;
        }
    }
}

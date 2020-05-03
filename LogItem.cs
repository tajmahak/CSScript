using System;
using System.Drawing;

namespace CSScript
{
    internal class LogItem
    {
        public string Text { get; private set; }

        public DateTime DateTime { get; private set; }

        public Color? ForeColor { get; private set; }


        public LogItem(string text, DateTime dateTime, Color? foreColor)
        {
            Text = text;
            DateTime = dateTime;
            ForeColor = foreColor;
        }
    }
}

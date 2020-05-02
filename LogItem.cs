using System.Drawing;

namespace CSScript
{
    internal class LogItem
    {
        public string Text { get; private set; }

        public Color? ForeColor { get; private set; }


        public LogItem(string text, Color? foreColor)
        {
            Text = text;
            ForeColor = foreColor;
        }
    }
}

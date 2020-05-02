using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;

namespace CSScript
{
    /// <summary>
    /// Представляет лог работы программы
    /// </summary>
    internal class Log
    {
        public ReadOnlyCollection<LogItem> Items => items.AsReadOnly();

        private readonly List<LogItem> items = new List<LogItem>();


        public event Action<LogItem> ItemAppened;


        public void Write(string text, Color? foreColor = null)
        {
            LogItem item = new LogItem(text, foreColor);
            items.Add(item);
            ItemAppened?.Invoke(item);
        }

        public void WriteLine(string text, Color? foreColor = null)
        {
            Write(text + Environment.NewLine, foreColor);
        }

        public void WriteLine()
        {
            Write(Environment.NewLine);
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            foreach (LogItem block in items)
            {
                str.Append(block.Text);
            }
            return str.ToString();
        }
    }
}

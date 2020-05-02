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


        public void Add(string text, Color? foreColor = null)
        {
            LogItem item = new LogItem(text, foreColor);
            items.Add(item);
            ItemAppened?.Invoke(item);
        }

        public void AddLine(string text, Color? foreColor = null)
        {
            Add(text + Environment.NewLine, foreColor);
        }

        public void AddLine()
        {
            Add(Environment.NewLine);
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

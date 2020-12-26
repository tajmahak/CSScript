using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SScript.Core
{
    public class MessageColorScheme
    {
        public ConsoleColor BackColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor CaptionColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor CommentColor { get; set; } = ConsoleColor.Green;
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.DarkRed;
        public ConsoleColor ForeColor { get; set; } = ConsoleColor.White;
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.Gray;
        public ConsoleColor SourceCodeColor { get; set; } = ConsoleColor.Cyan;
        public ConsoleColor StackTraceColor { get; set; } = ConsoleColor.Gray;
        public ConsoleColor SuccessColor { get; set; } = ConsoleColor.Green;

        public static MessageColorScheme Default => new MessageColorScheme();
    }
}

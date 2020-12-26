using System;

namespace CSScript.Core
{
    public class MessageColorScheme
    {
        public ConsoleColor Back { get; set; } = ConsoleColor.Black;
        public ConsoleColor Caption { get; set; } = ConsoleColor.Red;
        public ConsoleColor Comment { get; set; } = ConsoleColor.Green;
        public ConsoleColor Error { get; set; } = ConsoleColor.DarkRed;
        public ConsoleColor Fore { get; set; } = ConsoleColor.White;
        public ConsoleColor Info { get; set; } = ConsoleColor.Gray;
        public ConsoleColor SourceCode { get; set; } = ConsoleColor.Cyan;
        public ConsoleColor StackTrace { get; set; } = ConsoleColor.Gray;
        public ConsoleColor Success { get; set; } = ConsoleColor.Green;

        public static MessageColorScheme Default => new MessageColorScheme();
    }
}

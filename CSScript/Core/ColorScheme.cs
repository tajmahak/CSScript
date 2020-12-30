using System;

namespace CSScript.Core
{
    /// <summary>
    /// Цветовая схема для вывода цветного текста.
    /// </summary>
    public class ColorScheme
    {
        public ConsoleColor Back { get; set; } = ConsoleColor.Black;
        public ConsoleColor Caption { get; set; } = ConsoleColor.Red;
        public ConsoleColor Comment { get; set; } = ConsoleColor.Green;
        public ConsoleColor Error { get; set; } = ConsoleColor.DarkRed;
        public ConsoleColor Foreground { get; set; } = ConsoleColor.White;
        public ConsoleColor Info { get; set; } = ConsoleColor.Gray;
        public ConsoleColor SourceCode { get; set; } = ConsoleColor.Cyan;
        public ConsoleColor StackTrace { get; set; } = ConsoleColor.Gray;
        public ConsoleColor Success { get; set; } = ConsoleColor.Green;

        public static ColorScheme Default => new ColorScheme();
    }
}

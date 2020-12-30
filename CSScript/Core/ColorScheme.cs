﻿using System;

namespace CSScript.Core
{
    /// <summary>
    /// Цветовая схема для вывода цветного текста.
    /// </summary>
    public class ColorScheme
    {
        public ConsoleColor Background { get; set; } = ConsoleColor.Black;
        public ConsoleColor Foreground { get; set; } = ConsoleColor.White;
        public ConsoleColor Caption { get; set; } = ConsoleColor.DarkCyan;
        public ConsoleColor Info { get; set; } = ConsoleColor.Gray;
        public ConsoleColor Success { get; set; } = ConsoleColor.Green;
        public ConsoleColor Error { get; set; } = ConsoleColor.DarkRed;

        public static ColorScheme Default => new ColorScheme();
    }
}

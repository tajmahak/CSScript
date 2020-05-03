namespace CSScript
{
    /// <summary>
    /// Структурированные аргументы командной строки
    /// </summary>
    internal class InputArgumentsInfo
    {
        public bool IsEmpty { get; set; }
        public bool HideMode { get; set; }
        public string LogPath { get; set; }
        public string ScriptPath { get; set; }
        public string ScriptArgument { get; set; }
    }
}

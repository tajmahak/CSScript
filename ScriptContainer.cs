﻿using CSScript.Properties;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта в программе 
    /// </summary>
    public abstract class ScriptContainer
    {
        public ScriptContainer(string scriptPath, Settings settings)
        {
            ScriptPath = scriptPath;
            Settings = settings;
        }

        /// <summary>
        /// Запуск выполнения скрипта
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public abstract void StartScript(string[] args);

        // Сущности, используемые в скрипте (public):

        public readonly string ScriptPath;

        public readonly Settings Settings;

        public int ExitCode { get; set; }

        public bool GUIForceExit { get; set; }

        public static void WriteLog(object value, Color? color = null)
        {
            Program.ProgramModel.WriteLog(value.ToString(), color);
        }

        public static void WriteLineLog(object value, Color? color = null)
        {
            value = value ?? string.Empty;
            Program.ProgramModel.WriteLineLog(value.ToString(), color);
        }

        public static void WriteLineLog()
        {
            Program.ProgramModel.WriteLineLog();
        }

        public static Process CreateManagedProcess()
        {
            return Program.ProgramModel.CreateManagedProcess();
        }
    }
}
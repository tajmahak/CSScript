﻿using CSScript.Properties;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет интерфейс для взаимодействия скрипта с программой.
    /// </summary>
    public interface IScriptEnvironment
    {
        string ScriptPath { get; }

        Settings Settings { get; }

        void WriteMessage(object value, Color? foreColor);

        void WriteMessageLine(object value, Color? foreColor);

        void WriteMessageLine();

        Process CreateManagedProcess();
    }
}
﻿using CSScript.Properties;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет среду взаимодействия скрипта с программой.
    /// </summary>
    internal class ProgramScriptEnvironment : IScriptEnvironment
    {
        private readonly ProgramModel programModel;

        public ProgramScriptEnvironment(ProgramModel programModel, string scriptPath)
        {
            this.programModel = programModel;
            ScriptPath = scriptPath;
        }

        public int ExitCode { get; set; }

        public bool GUIForceExit { get; set; }

        public string ScriptPath { get; }

        public Settings Settings => programModel.Settings;

        public Process CreateManagedProcess() => programModel.ProcessManager.CreateManagedProcess();

        public void Write(object value, Color? foreColor = null) => programModel.MessageManager.Write(value, foreColor);

        public void WriteLine(object value, Color? foreColor = null) => programModel.MessageManager.WriteLine(value, foreColor);

        public void WriteLine() => programModel.MessageManager.WriteLine();
    }
}

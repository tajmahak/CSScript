using CSScript.Properties;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет реализацию взаимодействия скрипта с программой.
    /// </summary>
    internal class ProgramScriptEnvironment : IScriptEnvironment
    {
        private readonly ProgramModel programModel;

        public ProgramScriptEnvironment(ProgramModel programModel, string scriptPath, string[] scriptArgs)
        {
            this.programModel = programModel;
            ScriptPath = scriptPath;
            Args = scriptArgs;
        }

        public string[] Args { get; }

        public int ExitCode { get; set; }

        public bool AutoClose { get; set; }

        public string ScriptPath { get; }

        public Settings Settings => programModel.Settings;

        public void Write(object value, Color? foreColor = null) => programModel.MessageManager.Write(value, foreColor);

        public void WriteLine(object value, Color? foreColor = null) => programModel.MessageManager.WriteLine(value, foreColor);

        public void WriteLine() => programModel.MessageManager.WriteLine();

        public string GetInputText() => programModel.GetInputText(null);

        public string GetInputText(string caption) => programModel.GetInputText(caption);

        public Process CreateManagedProcess() => programModel.ProcessManager.CreateManagedProcess();
    }
}

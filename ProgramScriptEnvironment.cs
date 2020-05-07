using CSScript.Properties;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет реализацию для взаимодействия текущей модели программы со скриптом.
    /// </summary>
    internal class ProgramScriptEnvironment : IScriptEnvironment
    {
        private readonly ProgramModel programModel;

        public ProgramScriptEnvironment(ProgramModel programModel, string scriptPath)
        {
            this.programModel = programModel;
            ScriptPath = scriptPath;
        }

        public string ScriptPath { get; }

        public Settings Settings => programModel.Settings;

        public Process CreateManagedProcess()
        {
            return programModel.CreateManagedProcess();
        }

        public void WriteMessage(object value, Color? color)
        {
            if (value != null)
            {
                programModel.WriteMessage(value.ToString(), color);
            }
        }
      
        public void WriteMessageLine(object value, Color? color)
        {
            if (value != null)
            {
                programModel.WriteMessageLine(value.ToString(), color);
            }
        }

        public void WriteMessageLine()
        {
            programModel.WriteMessageLine();
        }
    }
}

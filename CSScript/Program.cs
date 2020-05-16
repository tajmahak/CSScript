using System;

namespace CSScript
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ProgramModel programModel = new ProgramModel(Properties.Settings.Default, args);
            ProgramBase.ExecuteProgram(programModel);
        }

        [STAThread]
        public static void Main(string scriptPath, string[] scriptArguments)
        {
            ProgramModel programModel = new ProgramModel(Properties.Settings.Default, scriptPath, scriptArguments);
            ProgramBase.ExecuteProgram(programModel);
        }
    }
}

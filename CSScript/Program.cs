using System;

namespace CSScript
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            InputArgumentsInfo inputArguments = InputArgumentsInfo.FromProgramArgs(args);
            ProgramModel programModel = new ProgramModel(inputArguments, Properties.Settings.Default);
            ProgramBase.ExecuteProgram(programModel);
        }
    }
}

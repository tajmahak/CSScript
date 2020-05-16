using System;

namespace CSScript
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ProgramModel programModel = new ProgramModel(Properties.Settings.Default, args);
            ProgramBase.ExecuteProgram(programModel);
        }
    }
}

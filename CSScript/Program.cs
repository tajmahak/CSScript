#define GUI_VERSION // - использование графического интерфейса вместо консольного (+ вручную переключить в свойствах проекта тип выходных данных)

using System;
using System.Reflection;

namespace CSScript
{
    internal abstract class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
#if GUI_VERSION
            Program program = new GUIProgram(args);
#else
            Program program = new ConsoleProgram(args);
#endif
            program.StartProgram();
        }

        protected ProgramModel ProgramModel { get; private set; }

        protected Program(string[] args)
        {
            ProgramModel = new ProgramModel(Properties.Settings.Default, args);

            // для подгрузки библиотек рантаймом, которые не подгружаются самостоятельно
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolveEvent;
        }

        protected abstract void StartProgram();

        private Assembly CurrentDomain_AssemblyResolveEvent(object sender, ResolveEventArgs args)
        {
            return ProgramModel.ResolveAssembly(args.Name);
        }
    }
}

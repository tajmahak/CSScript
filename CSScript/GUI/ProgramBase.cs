#define GUI_VERSION // - использование графического интерфейса вместо консольного (+ вручную переключить в свойствах проекта тип выходных данных)

using System;
using System.Reflection;

namespace CSScript
{
    internal abstract class ProgramBase
    {
        public static void ExecuteProgram(ProgramModel programModel)
        {
#if GUI_VERSION
            ProgramBase program = new GUIProgram(programModel);
            program.StartProgram();
#else
            ProgramBase program = new ConsoleProgram(programModel);
            program.StartProgram();
#endif
        }


        protected ProgramModel ProgramModel { get; private set; }

        protected ProgramBase(ProgramModel programModel)
        {
            ProgramModel = programModel;

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
using CSScript.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSScriptStand
{
    class Stand : ScriptContainer
    {
        public Stand(IScriptEnvironment env) : base(env)
        {
        }

        public override void Execute()
        {
            AAA();
        }

        void AAA()
        {
            env.WriteLine("123", colors.Success);
        }
    }
}

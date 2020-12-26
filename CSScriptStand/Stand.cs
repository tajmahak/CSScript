using CSScript.Core;

namespace CSScriptStand
{
    internal class Stand : ScriptContainer
    {
        public Stand(IScriptEnvironment env) : base(env)
        {
        }

        public override void Execute()
        {
            AAA();
        }

        private void AAA()
        {
            env.WriteLine("123", colors.Success);
        }
    }
}

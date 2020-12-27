using System;

namespace CSScript
{
    internal class ScriptRuntimeException : Exception
    {
        public ScriptRuntimeException(Exception innerException) : base(null, innerException) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SScript.Core
{
    public class ScriptHandler
    {
        public MessageManager Messages { get; private set; }

        public ScriptHandler()
        {
            Messages = new MessageManager();
        }



    }
}

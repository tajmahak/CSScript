using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using static __Utils;

namespace CSScriptStand
{
    internal class Stand : CSScript.Core.ScriptContainer
    {
        // true  - выполнение кода без перехвата исключений
        // false - выполнение кода с помощью обработчика CSCript
        public static readonly bool UseSimpleExecutor = true;

        public Stand(CSScript.Core.IScriptContext context) : base(context) {
            // Инициализация (в скрипте он внедняется в класс вместо неработающей конструкции using static)
            __Utils.context = Context;
        }

        public override void Start() {



        }



    }
}

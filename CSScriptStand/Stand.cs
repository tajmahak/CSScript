﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static __Utils;

namespace CSScriptStand
{
    internal class Stand : CSScript.Core.ScriptContainer
    {
        public Stand(CSScript.Core.IScriptContext context) : base(context) {
            // Инициализация (в скрипте он внедняется в класс вместо неработающей конструкции using static)
            __Utils_Init(Context);
        }

        public override void Start() {



        }



    }
}

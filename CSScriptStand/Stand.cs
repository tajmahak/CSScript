﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static __Utils;

namespace CSScriptStand
{
    public class Stand : CSScript.Core.ScriptContainer
    {
        public Stand(CSScript.Core.IScriptContext context) : base(context) {
            // Инициализация (в скрипте он внедняется в класс вместо неработающей конструкции using static)
            __Utils_Init(Context);
            WindowsUtils.__WindowsUtils_Init();
        }

        public override void Start() {



        }



    }
}

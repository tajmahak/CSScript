﻿using System;
using System.Threading;

namespace CSScript.Core
{
    public class ScriptExecutionContext
    {
        public ScriptExecutionContext(ScriptContainer container) {
            Container = container;
        }

        public ScriptContainer Container { get; }

        public Thread Thread { get; private set; }

        public Exception ThreadException { get; private set; }

        public bool Aborted => aborted;

        private volatile bool aborted;

        public void Start() {
            Thread = new Thread(StartContainer);
            Thread.Start();
        }

        public void Join() {
            Thread.Join();
        }

        public void Abort() {
            aborted = true;
            Thread.Abort();
        }

        private void StartContainer() {
            try {
                Container.Start();
            } catch (Exception ex) {
                ThreadException = ex;
            }
        }
    }
}

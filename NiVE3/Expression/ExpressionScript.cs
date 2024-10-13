using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acornima.Ast;
using Jint;

namespace NiVE3.Expression
{
    class ExpressionScript : IDisposable
    {
        public Prepared<Script> Script { get; }

        bool Disposed { get; set; }

        public ExpressionScript(Prepared<Script> script)
        {
            Script = script;
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
            }
        }
    }
}

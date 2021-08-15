using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jisarv.SpeechEngine.Generator.Propagator.Grammer
{
    class InternalCallReturn {
        public Dictionary<string, string> Variables;
        public string Call;
    }

    enum DomainFunctionType
    {
        Answer,
        Ask,
        Common
    }

    class DomainFunction
    {
        public string Name;
        public Node Nodes;
        public DomainFunctionType Type;
        public bool Locked;
        public Func<Dictionary<string, string>, InternalCallReturn> InternalCall;
        public Func<Dictionary<string, string>, string> Analyzer;
    }
}

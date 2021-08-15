using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jisarv.SpeechEngine.Generator.Propagator.Data
{
    class DomainFunctionReturn
    {
        public DomainFunctionReturn(DomainFunctionReturn ret)
        {
            foreach(var s in ret.Variables)
            {
                Variables.Add(s.Key, s.Value);
            }
            Index = ret.Index;
            Finished = ret.Finished;
        }

        public DomainFunctionReturn(int index = 0)
        {
            Index = index;
            NodeCount = 0;
        }

        public Dictionary<string, string> Variables = new Dictionary<string, string>();
        public bool Finished;
        public int Index, NodeCount;
    }
}

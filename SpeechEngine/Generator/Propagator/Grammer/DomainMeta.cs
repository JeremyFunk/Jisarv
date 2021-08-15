using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jisarv.SpeechEngine.Generator.Propagator.Data
{
    class Answer
    {
        public string Name;
        public string Unlock;
    }
    class Question
    {
        public string Name;
        public bool Locked;
    }

    class DomainMeta
    {
        public Answer[] Answers;
        public Question[] Questions;
    }
}

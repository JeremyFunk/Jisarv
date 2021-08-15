namespace Jisarv.SpeechEngine.Generator.Propagator.Grammer {
    class Node {
        public Node Clone()
        {
            Node[] optionalClone = null;
            if(Options != null)
            {
                optionalClone = new Node[Options.Length];
                for (var i = 0; i < Options.Length; i++)
                {
                    optionalClone[i] = Options[i].Clone();
                }
            }

            return new Node { IsVar = IsVar, Optional = Optional, Value = Value?.Clone() as string[], NextNode = NextNode?.Clone(), OptionalPath = OptionalPath?.Clone(), Options = optionalClone };
        }

        public Node NextNode;
        public Node OptionalPath;
        public bool Optional;
        public Node[] Options;
        public string[] Value;
        public bool IsVar;
        public bool IsWildcard;
        public bool IsReference;
        public DomainFunction Reference;
    }
}
using Jisarv.SpeechEngine.Generator.Propagator.Data;
using Jisarv.SpeechEngine.Generator.Propagator.Grammer;
using Jisarv.Util;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Jisarv.SpeechEngine.Generator.Domains {

    abstract class Domain {

        private readonly DomainStructure domain;
        private static DomainFunction[] common;

        public static void LoadCommon(string language)
        {
            string[] ask = IO.LoadFileLines(@"\res\domains\common\" + language + @"\common.gra");

            ParseFunctions(ask, DomainFunctionType.Common, true);
        }

        public Domain(string domainName, string language)
        {
            domain = new DomainStructure();
            domain.Name = domainName;
            ParseDomain(domainName, language);
            var functions = LoadDomainFunctions();
            LinkCalls(domainName, language, functions);
        }

        public abstract Dictionary<string, Func<Dictionary<string, string>, InternalCallReturn>> LoadDomainFunctions();

        //Runtime
        public string Propagate(string text)
        {
            text = text.ToLower();

            foreach (var ask in domain.Ask)
            {
                var res = PropagateNode(text.Split(" "), 0, ask.Nodes, new DomainFunctionReturn());
                var curated = new List<DomainFunctionReturn>();
                var highestNodeCount = 0;
                foreach (var r in res)
                {
                    if (r.Finished)
                    {
                        curated.Add(r);
                        if (r.NodeCount > highestNodeCount)
                        {
                            highestNodeCount = r.NodeCount;
                        }
                    }
                }

                var highestVarCount = 0;
                foreach (var r in res)
                {
                    if (r.Finished)
                    {
                        if (r.NodeCount == highestNodeCount && r.Variables.Count > highestVarCount)
                        {
                            highestVarCount = r.Variables.Count;
                        }
                    }
                }

                DomainFunctionReturn likelyRet = null;
                foreach (var r in res)
                {
                    if (r.Finished)
                    {
                        if (r.NodeCount == highestNodeCount && r.Variables.Count == highestVarCount)
                        {
                            likelyRet = r;
                            break;
                        }
                    }
                }

                Logger.Log("Found " + curated.Count + " Results!");

                if(curated.Count >= 1)
                {
                    if(ask.InternalCall != null)
                    {
                        var responseVars = ask.InternalCall(likelyRet.Variables);
                        var function = FindFunction(responseVars.Call);

                        if(function == null)
                        {
                            Logger.Log("Could not find function " + responseVars.Call, LogLevel.Warning);
                        }
                        else
                        {
                            var response = BuildResponse("", function.Nodes, responseVars.Variables);

                            if (response.Length > 0)
                            {
                                return response[0];
                            }
                        }
                    }
                }
            }
            return "";
        }

        private DomainFunction FindFunction(string name)
        {
            foreach(var n in domain.Answer)
            {
                if(n.Name == name)
                {
                    return n;
                }
            }
            return null;
        }

        private static DomainFunction FindFunctionCommon(string name)
        {
            foreach (var n in common)
            {
                if (n.Name == name)
                {
                    return n;
                }
            }
            return null;
        }

        private DomainFunctionReturn[] PropagateNode(string[] words, int index, Node node, DomainFunctionReturn currentRet)
        {
             currentRet.NodeCount += 1;
             if (node == null)
             {
                 var copiedRet = new DomainFunctionReturn(currentRet);
                 copiedRet.Index = index;
                 copiedRet.Finished = index >= words.Length;
                 return new DomainFunctionReturn[] { copiedRet };
             }


            if (node.Optional)
            {
                var result = PropagateNode(words, index, node.NextNode, currentRet);

                List<DomainFunctionReturn> returns = new List<DomainFunctionReturn>();
                returns.AddRange(result);
                var oResult = PropagateNode(words, index, node.OptionalPath, currentRet);
                foreach (var o in oResult)
                {
                    if (o.Finished)
                    {
                        returns.Add(o);
                    }
                    else
                    {
                        returns.AddRange(PropagateNode(words, o.Index, node.NextNode, o));
                    }
                }
                return returns.ToArray();
            }
            else if (node.IsVar)
            {
                if (node.IsReference)
                {
                    var func = node.Reference;

                    var results = PropagateNode(words, index, func.Nodes, new DomainFunctionReturn());

                    

                    List<DomainFunctionReturn> returns = new List<DomainFunctionReturn>();

                    foreach (var result in results)
                    {
                        var copiedRet = new DomainFunctionReturn(currentRet);
                        copiedRet.Index = result.Index;
                        copiedRet.Finished = result.Finished;
                        copiedRet.NodeCount += result.NodeCount;
                        var varResult = func.Analyzer(result.Variables);
                         copiedRet.Variables.Add(node.Value[0], varResult);

                        if (result.Finished)
                        {
                            returns.Add(copiedRet);
                        }
                        else
                        {
                            var currentReturns = PropagateNode(words, result.Index, node.NextNode, copiedRet);
                            returns.AddRange(currentReturns);
                        }
                    }
                    return returns.ToArray();
                }
                else
                {
                    var copiedRet = new DomainFunctionReturn(currentRet);
                    copiedRet.Variables.Add(node.Value[0], node.Value[1]);

                    var options = PropagateNode(words, index, node.NextNode, copiedRet);
                    return options;
                }
            }
            else if (node.IsWildcard)
            {
                List<DomainFunctionReturn> returns = new List<DomainFunctionReturn>();
                string variables = "";
                for (var i = index; i < index + 3; i++)
                {
                    if (i >= words.Length)
                    {
                        break;
                    }
                    variables += variables == "" ? words[i] : " " + words[i];

                    var copiedRet = new DomainFunctionReturn(currentRet);
                    copiedRet.Variables.Add(node.Value[0], variables);
                    var options = PropagateNode(words, i + 1, node.NextNode, copiedRet);
                    returns.AddRange(options);
                }

                return returns.ToArray();
            }
            else if (node.IsReference)
            {
                var func = node.Reference;
                var results = PropagateNode(words, index, func.Nodes, new DomainFunctionReturn());

                List<DomainFunctionReturn> returns = new List<DomainFunctionReturn>();

                foreach(var result in results)
                {
                    if (result.Finished)
                    {
                        result.Variables.Clear();
                        returns.Add(result);
                    }
                    else
                    {
                        var currentReturns = PropagateNode(words, result.Index, node.NextNode, currentRet);
                        returns.AddRange(currentReturns);
                    }
                }
                return returns.ToArray();
            }
            else
            {
                if (node.Options != null)
                {
                    List<DomainFunctionReturn> nRet = new List<DomainFunctionReturn>();

                    foreach (var n in node.Options)
                    {
                        /*var currentIndex = index;
                        var found = true;
                        foreach (var s in n.Value)
                        {
                            if (currentIndex >= words.Length)
                            {
                                found = false;
                                break;
                            }

                            if (s == words[currentIndex])
                            {
                                currentIndex++;
                            }
                            else
                            {
                                found = false;
                                break;
                            }
                        }*/

                        var nRes = PropagateNode(words, index, n, currentRet);

                        if (nRes.Length > 0)
                        {
                            foreach(var curNRes in nRes)
                            {
                                var nResN = PropagateNode(words, curNRes.Index, node.NextNode, curNRes);
                                nRet.AddRange(nResN);
                            }
                        }
                    }
                    return nRet.ToArray();
                }
                else
                {
                    var currentIndex = index;
                    var found = true;
                    foreach (var s in node.Value)
                    {
                        if (currentIndex >= words.Length)
                        {
                            found = false;
                            break;
                        }

                        if (s == words[currentIndex])
                        {
                            currentIndex++;
                        }
                        else
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return PropagateNode(words, currentIndex, node.NextNode, currentRet);
                    }
                }
            }

            return new DomainFunctionReturn[] { };
        }


        private string[] BuildResponse(string currentResponse, Node currentNode, Dictionary<string, string> repsonseVars)
        {
            if(currentNode == null)
            {
                return new string[] { currentResponse };
            }

            if (currentNode.IsWildcard)
            {
                if (!repsonseVars.ContainsKey(currentNode.Value[0]))
                {
                    return new string[] { };
                }
                return BuildResponse(currentResponse + repsonseVars[currentNode.Value[0]] + " ", currentNode.NextNode, repsonseVars);
            }

            if (currentNode.Optional)
            {
                var curRes = BuildResponse(currentResponse, currentNode.OptionalPath, repsonseVars);

                if(curRes.Length == 0)
                {
                    return BuildResponse(currentResponse, currentNode.NextNode, repsonseVars);
                }

                return curRes;
            }

            string valueText = "";
            foreach(var s in currentNode.Value)
            {
                valueText += s + ' ';
            }

            return BuildResponse(currentResponse + valueText, currentNode.NextNode, repsonseVars);
        }


        //Domain
        protected void ParseDomain(string domainName, string language){
            string[] ask = IO.LoadFileLines(@"\res\domains\" + domainName + @"\" + language + @"\" + domainName + ".ask");
            string[] ans = IO.LoadFileLines(@"\res\domains\" + domainName + @"\" + language + @"\" + domainName + ".ans");

            domain.Ask = ParseFunctions(ask, DomainFunctionType.Ask);
            domain.Answer = ParseFunctions(ans, DomainFunctionType.Answer);
        }

        private static DomainFunction[] ParseFunctions(string[] text, DomainFunctionType type, bool isCommon = false)
        {
            int Convert(string s, bool a)
            {
                return 0;
            }

            List<DomainFunction> functions = new List<DomainFunction>();

            string openFunction = "";
            int paranthesisCount = 0;
            int mode = 0; // 1 = Interpreter, 2 = Analyzer
            //Type functionReturnType = typeof System.Double;
            string currentEvaluatorText = "";
            string currentAnalyzerText = "";
            foreach (string line in text)
            {
                if (line.StartsWith("//"))
                {
                    continue;
                }

                if (line.Contains("{"))
                {
                    paranthesisCount++;
                }

                if (line.Contains("}"))
                {
                    paranthesisCount--;
                    if(paranthesisCount == 0)
                    {
                        mode = 0;
                    }
                }

                if (line.EndsWith(":"))
                {
                    if (openFunction != "")
                    {
                        if(currentAnalyzerText != "")
                        {
                            currentAnalyzerText = "public static string Analyze(Dictionary<string, string> variables){\n" + currentAnalyzerText + "\n}";
                            //Console.WriteLine(currentAnalyzerText);
                            Logger.Log("Loading Function: " + openFunction, LogLevel.Log);

                            var variables = new string[] { };

                            if (openFunction.Contains("(") && !openFunction.EndsWith("():"))
                            {
                                var temp = openFunction.Split("(")[1];
                                temp = temp.Substring(0, temp.Length - 1);
                                variables = temp.Split(",");
                            }

                            Node currentNode = ParseEvaluator(currentEvaluatorText.Trim());
                            var result = CSharpScript.Create<string>(currentAnalyzerText, ScriptOptions.Default.WithImports("System", "System.Collections.Generic")).ContinueWith<Func<Dictionary<string, string>, string>>("Analyze").CreateDelegate().Invoke().GetAwaiter().GetResult();
                            
                            functions.Add(new DomainFunction { Name = openFunction, Nodes = currentNode, Type = type, Analyzer = result, Variables = variables });
                            if (isCommon)
                                common = functions.ToArray();
                        }
                        else
                        {
                            Node currentNode = ParseEvaluator(currentEvaluatorText.Trim());
                            functions.Add(new DomainFunction { Name = openFunction, Nodes = currentNode, Type = type });
                            if (isCommon)
                                common = functions.ToArray();
                        }
                    }
                    openFunction = line.Replace(":", "");
                    currentEvaluatorText = "";
                    currentAnalyzerText = "";
                }
                else if (line.Contains("evaluator{"))
                {
                    mode = 1;
                }
                else if (line.Contains("analyzer{"))
                {
                    mode = 2;
                }
                else if(mode == 1)
                {
                    currentEvaluatorText += line.Trim();
                }
                else if(mode == 2)
                {
                    currentAnalyzerText += line.Trim() + "\n";
                }

            }

            if (currentAnalyzerText != "")
            {
                currentAnalyzerText = "public static string Analyze(Dictionary<string, string> variables){\n" + currentAnalyzerText + "\n}";

                //Console.WriteLine(currentAnalyzerText);
                Logger.Log("Loading Function: " + openFunction, LogLevel.Log);
                var nodes = ParseEvaluator(currentEvaluatorText.Trim());

                var results = CSharpScript.Create<string>(currentAnalyzerText, ScriptOptions.Default.WithImports("System", "System.Collections.Generic")).ContinueWith<Func<Dictionary<string, string>, string>>("Analyze").CreateDelegate().Invoke().GetAwaiter().GetResult();

                functions.Add(new DomainFunction { Name = openFunction, Nodes = nodes, Analyzer = results });
            }
            else
            {
                var nodes = ParseEvaluator(currentEvaluatorText.Trim());
                functions.Add(new DomainFunction { Name = openFunction, Nodes = nodes });
            }
            if (isCommon)
            {
                common = functions.ToArray();
                return common;
            }

            return functions.ToArray();
        }

        private void LinkCalls(string domainName, string language, Dictionary<string, Func<Dictionary<string, string>, InternalCallReturn>> functions)
        {
            string metaText = IO.LoadFileText(@"\res\domains\time\" + language + @"\" + domainName + ".meta.json");
            DomainMeta meta = JsonConvert.DeserializeObject<DomainMeta>(metaText);

            foreach(var q in meta.Questions)
            {
                foreach (var a in domain.Ask)
                {
                    if (a.Name == q.Name)
                    {
                        a.Locked = q.Locked;
                        a.InternalCall = functions[q.Name];
                        break;
                    }
                }
            }
        }


        private static string[] SplitParenthesisSafe(string text, char split)
        {
            var list = new List<string>();
            var s = 0;
            var level = 0;
            for(var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if(c == '(' || c == '[')
                {
                    level++;
                }else if(c == ')' || c == ']')
                {
                    level--;
                }else if(c == split && level == 0)
                {
                    list.Add(text.Substring(s, i - s));
                    s = i + 1;
                }
            }
            list.Add(text.Substring(s, text.Length - s));
            return list.ToArray();
        }

        private static Node ParseEvaluator(string text)
        {
            int p1 = 0, p2 = 0, p3 = 0;
            foreach(char c in text)
            {
                switch (c)
                {
                    case '(':
                        p1++;
                        break;
                    case ')':
                        p1--;
                        break;

                    case '[':
                        p2++;
                        break;
                    case ']':
                        p2--;
                        break;

                    case '{':
                        p3++;
                        break;
                    case '}':
                        p3--;
                        break;
                }
            }

            if(p1 != 0)
            {
                Logger.Log("Evaluator could not be interpreted. ( = " + p1, LogLevel.Error);
            }
            if (p2 != 0)
            {
                Logger.Log("Evaluator could not be interpreted. [ = " + p2, LogLevel.Error);
            }
            if (p3 != 0)
            {
                Logger.Log("Evaluator could not be interpreted. { = " + p3, LogLevel.Error);
            }

            Node currentNode = new Node();
            int index = 0;

            var or = SplitParenthesisSafe(text, '|');
            if (or.Length > 1)
            {
                List<Node> options = new List<Node>();

                foreach (var curOr in or)
                {
                    var orNode = ParseEvaluator(curOr.Trim());
                    options.Add(orNode);
                }

                currentNode.Options = options.ToArray();
            }
            else
            {
                var and = SplitParenthesisSafe(text, '.');

                if (and.Length > 1)
                {
                    Node anchorNode = null;

                    foreach (var curAnd in and)
                    {
                        var node = ParseEvaluator(curAnd.Trim());
                        if(anchorNode == null)
                        {
                            currentNode = node;
                            anchorNode = node;
                        }
                        var counter = 0;
                        while (anchorNode.NextNode != null && anchorNode.NextNode != anchorNode && counter < 10000) {
                            anchorNode = anchorNode.NextNode;
                            counter++;
                        }

                        anchorNode.NextNode = node;
                        anchorNode = node;
                    }
                }
                else
                {

                    if (text.TrimStart().StartsWith("\""))
                    {
                        for (var i = 1; i < text.Length; i++)
                        {
                            var c = text[i];
                            if (c == '"')
                            {
                                break;
                            }

                            index++;
                        }
                        currentNode.Value = text.Substring(1, index).ToLower().Split(" ");
                    }
                    else if (text.TrimStart().StartsWith("$"))
                    {
                        for (var i = 1; i < text.Length; i++)
                        {
                            var c = text[i];
                            if (c == '$')
                            {
                                break;
                            }

                            index++;
                        }
                        var content = text.Substring(1, index);
                        var varSplit = content.Split("=");

                        if (varSplit.Length == 1)
                        {
                            currentNode.Value = new string[] { content };
                            currentNode.IsWildcard = true;
                        }
                        else
                        {
                            if (varSplit[1].EndsWith(')'))
                            {
                                currentNode.Reference = FindFunctionCommon(varSplit[1].Split("(")[0]);
                                currentNode.IsReference = true;
                            }
                            currentNode.Value = new string[] { varSplit[0], varSplit[1] };
                            currentNode.IsVar = true;
                        }
                    }
                    else if (text.TrimStart().StartsWith("#"))
                    {
                        for (var i = 1; i < text.Length; i++)
                        {
                            var c = text[i];
                            if (c == '#')
                            {
                                break;
                            }

                            index++;
                        }

                        var name = text.Substring(1, index);

                        foreach(var c in common)
                        {
                            if(c.Name == name)
                            {
                                currentNode.IsReference = true;
                                currentNode.Reference = c;
                            }
                        }

                    }
                    else if (text.TrimStart().StartsWith("("))
                    {
                        int openBrack = 0;
                        foreach (char c in text)
                        {
                            if (c == '(')
                            {
                                openBrack++;
                            }
                            else if (c == ')')
                            {
                                openBrack--;
                            }

                            if (openBrack == 0)
                            {
                                break;
                            }
                            index++;
                        }

                        currentNode = ParseEvaluator(text.Substring(1, index - 1).Trim());
                    }
                    else if (text.TrimStart().StartsWith("["))
                    {
                        int openBrack = 0;
                        foreach (char c in text)
                        {
                            if (c == '[')
                            {
                                openBrack++;
                            }
                            else if (c == ']')
                            {
                                openBrack--;
                            }

                            if (openBrack == 0)
                            {
                                break;
                            }
                            index++;
                        }

                        Node temp = ParseEvaluator(text.Substring(1, index - 1).Trim());
                        currentNode.OptionalPath = temp;
                        currentNode.Optional = true;
                    }
                }
            }

            return currentNode;
        }
    }
}
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jisarv.Data;
using Jisarv.SpeechEngine;
using Jisarv.SpeechEngine.Generator.Domains;
using Jisarv.Util;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp;

namespace Jisarv
{
    class Program
    {
        private static SpeechOutput speech;
        private static TimeDomain timeDomain;

        static void Main(string[] args)
        {
            long startTime = DateTime.Now.Ticks;

            Load();
            long loadTimestamp = DateTime.Now.Ticks;
            double loadTime = Math.Round((loadTimestamp - startTime) / 10_000_000.0, 2);

            Init();
            long initTimestamp = DateTime.Now.Ticks;
            double initTime = Math.Round((initTimestamp - loadTimestamp) / 10_000_000.0, 2);

            Compile();
            double compileTime = Math.Round((DateTime.Now.Ticks - initTimestamp) / 10_000_000.0, 2);



            Logger.Log("Total Time is: " + Math.Round((DateTime.Now.Ticks - startTime) / 10_000_000.0, 2) + "s\n\t\tLoad Time is: " + loadTime + "s\n\t\tInit Time is: " + initTime + "s\n\t\tCompile Time is: " + compileTime + "s", LogLevel.Info);

            while (true)
            {
                var text = Console.ReadLine();

                if (text == "exit" || text == "close" || text == "")
                {
                    break;
                }

                var response = timeDomain.Propagate(text);
                Logger.Log("Response: " + response);

                speech.Speak(response);
            }
        }

        private static void Load()
        {
            GeoData.LoadGeoData();
        }

        private static void Init()
        {
            speech = new SpeechOutput();
        }

        private static void Compile()
        {
            Domain.LoadCommon("en");
            timeDomain = new TimeDomain();
        }
    }
}

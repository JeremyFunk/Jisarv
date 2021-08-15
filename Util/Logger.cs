using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jisarv.Util
{
    enum LogLevel
    {
        Log,
        Info,
        Warning,
        Error
    }

    class Logger
    {
        public static void Log(string message, LogLevel lvl = LogLevel.Log)
        {
            var msg = "[" + DateTime.Now.ToString("HH:mm:ss") + "]: " + message;



            switch (lvl)
            {
                case LogLevel.Log:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(msg);
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(msg);
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(msg);
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg);
                    break;
            }
        }
    }
}

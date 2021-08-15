using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jisarv.Util
{
    class IO
    {
        private static readonly string Workspace = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;

        public static string LoadFileText(string path)
        {
            return File.ReadAllText(Workspace + path);
        }
        public static string[] LoadFileLines(string path)
        {
            return File.ReadAllLines(Workspace + path);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTerminator
{
    partial class Program
    {
        static string startLoc;
        static void Main(string[] args)
        {
            startLoc = Environment.CurrentDirectory; 
#if DEBUG
            Weston();
#else
            Weston();
#endif
        }

        static string PSScript()
        {
            return File.ReadAllText($@"{startLoc}\ScriptBlock.ps1");
        }
    }
}

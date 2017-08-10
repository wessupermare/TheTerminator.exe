using System;
using System.IO;
using System.Management.Automation;

namespace TheTerminator
{
    partial class Program
    {
        static void Weston()
        {
            int maxThreads = 50;

            //obtain server list (from flat file - same directory as set below)
            //Environment.CurrentDirectory = @"C:\PowerShell";
            //string[] serverNames = File.ReadAllLines("Servers.txt");
            //string[] services = File.ReadAllLines("Services.txt");
            //string[] processes = File.ReadAllLines("Processes.txt");

            //Document Start Time in Console:
            Console.WriteLine($"Start {DateTime.Now}");

            ScriptBlock sb = ScriptBlock.Create(PSScript());
        }
    }
}
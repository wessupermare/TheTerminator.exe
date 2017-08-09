using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace TheTerminator
{
    partial class Program
    {
        static void Weston()
        {
            int maxThreads = 0;
            List<string> processes = null, servers = null, args = Environment.GetCommandLineArgs().ToList();
            bool wmi = false;
            #region "Command line args"
            args.RemoveAt(0);

            for (ushort cntr = 1; cntr <= args.Count; ++cntr)
            {
                string arg = args[cntr++];

                switch (arg)
                {
                    case "-threads":
                        try
                        { maxThreads = int.Parse(args[cntr]); }
                        catch (FormatException)
                        { Console.WriteLine("Expected integer number of threads."); }
                        break;

                    case "-wmi":
                        wmi = true;
                        --cntr;
                        break;

                    case "-processes":
                        processes = new List<string>();
                        while (!(args[cntr] ?? "-").StartsWith("-"))
                        {
                            processes.Add(args[cntr]);
                            ++cntr;
                        }
                        break;

                    case "-servers":
                        servers = new List<string>();
                        while (!(args[cntr] ?? "-").StartsWith("-"))
                        {
                            servers.Add(args[cntr]);
                            ++cntr;
                        }
                        break;

                    default:
                        throw new ArgumentException($"Unexpected argument {arg}");
                }
            }

            if (maxThreads == 0)
            {
                maxThreadsLoop:
                try
                {
                    Console.Write("Max # of threads: ");
                    maxThreads = int.Parse(Console.ReadLine());
                }
                catch (FormatException)
                {
                    Console.WriteLine("Expected integer number of threads.");
                    goto maxThreadsLoop;
                }
            }

            if (processes == null)
            {
                Console.WriteLine("List of porcesses to kill pressing enter after each. Press enter twice to finish.");
                processes = new List<string>();
                do
                {
                    Console.Write($"\tProcess {processes.Count + 1}: ");
                    processes.Add(Console.ReadLine());
                } while (!string.IsNullOrEmpty(processes.Last()));
                servers.Remove(processes.Last());
            }

            if (servers == null)
            {
                Console.WriteLine("List server names pressing enter after each. Press enter twice to finish.");
                servers = new List<string>();
                do
                {
                    Console.Write($"\tServer {servers.Count + 1}: ");
                    servers.Add(Console.ReadLine());
                } while (!string.IsNullOrEmpty(servers.Last()));
                servers.Remove(servers.Last());
            }
            #endregion

            RunspacePool rsPool = RunspaceFactory.CreateRunspacePool(1, maxThreads);
            rsPool.Open();
            List<RSInstanceOutput> Output = new List<RSInstanceOutput>();

            if (wmi)
            { throw new NotImplementedException(); }
            else
            {
                foreach (string server in servers)
                {
                    foreach (string process in processes)
                    {
                        PowerShell PSinstance = PowerShell.Create().AddScript($@".\pskill -accepteula -nobanner -t \\{server} -u T2\admin -p T2Temp1611 {process}");
                        PSinstance.RunspacePool = rsPool;

                        Output.Add(new RSInstanceOutput
                        {
                            Server = server,
                            Pipe = PSinstance,
                            Result = PSinstance.BeginInvoke()
                        });
                    }
                }

                rsPool.Close();
            }
        }
    }

    struct RSInstanceOutput
    {
        public string Server;
        public PowerShell Pipe;
        public IAsyncResult Result;
    }
}
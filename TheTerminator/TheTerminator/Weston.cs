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
            int maxThreads = 50;
            List<string> processes = new List<string>() { "notepad" }, services = new List<string>() { "OpenVPNServiceInteractive" }, servers = new List<string>() { "192.168.3.70" }, args = Environment.GetCommandLineArgs().ToList();
            bool wmi = false;
            #region "Command line args"
            //args.RemoveAt(0);

            //for (ushort cntr = 1; cntr <= args.Count; ++cntr)
            //{
            //    string arg = args[cntr++];

            //    switch (arg)
            //    {
            //        case "-threads":
            //            try
            //            { maxThreads = int.Parse(args[cntr]); }
            //            catch (FormatException)
            //            { Console.WriteLine("Expected integer number of threads."); }
            //            break;

            //        case "-wmi":
            //            wmi = true;
            //            --cntr;
            //            break;

            //        case "-processes":
            //            processes = new List<string>();
            //            while (!(args[cntr] ?? "-").StartsWith("-"))
            //            {
            //                processes.Add(args[cntr]);
            //                ++cntr;
            //            }
            //            break;

            //        case "-services":
            //            services = new List<string>();
            //            while (!(args[cntr] ?? "-").StartsWith("-"))
            //            {
            //                services.Add(args[cntr]);
            //                ++cntr;
            //            }
            //            break;

            //        case "-servers":
            //            servers = new List<string>();
            //            while (!(args[cntr] ?? "-").StartsWith("-"))
            //            {
            //                servers.Add(args[cntr]);
            //                ++cntr;
            //            }
            //            break;

            //        default:
            //            throw new ArgumentException($"Unexpected argument {arg}");
            //    }
            //}

            //if (maxThreads == 0)
            //{
            //    maxThreadsLoop:
            //    try
            //    {
            //        Console.Write("Max # of threads: ");
            //        maxThreads = int.Parse(Console.ReadLine());
            //    }
            //    catch (FormatException)
            //    {
            //        Console.WriteLine("Expected integer number of threads.");
            //        goto maxThreadsLoop;
            //    }
            //}

            //if (processes == null)
            //{
            //    Console.WriteLine("List of porcesses to kill pressing enter after each. Press enter twice to finish.");
            //    processes = new List<string>();
            //    do
            //    {
            //        Console.Write($"\tProcess {processes.Count + 1}: ");
            //        processes.Add(Console.ReadLine());
            //    } while (!string.IsNullOrEmpty(processes.Last()));
            //    servers.Remove(processes.Last());
            //}

            //if (servers == null)
            //{
            //    Console.WriteLine("List server names pressing enter after each. Press enter twice to finish.");
            //    servers = new List<string>();
            //    do
            //    {
            //        Console.Write($"\tServer {servers.Count + 1}: ");
            //        servers.Add(Console.ReadLine());
            //    } while (!string.IsNullOrEmpty(servers.Last()));
            //    servers.Remove(servers.Last());
            //}
            #endregion

            RunspacePool rsPool = RunspaceFactory.CreateRunspacePool(1, maxThreads);
            rsPool.Open();
            List<PSObject> Output = new List<PSObject>();

            if (wmi)
            { throw new NotImplementedException(); }
            else
            {
                foreach (string server in servers)
                {
                    PowerShell PSinstance = PowerShell.Create().AddScript(PSScript()).AddArgument(server).AddArgument("stop").AddArgument(services.ToArray()).AddArgument(processes.ToArray()).AddArgument(""); // servername, start/stop, services, processes, startmode
                    PSinstance.RunspacePool = rsPool;

                    RSInstanceOutput obj = new RSInstanceOutput()
                    {
                        Server = server,
                        Pipe = PSinstance,
                        Result = PSinstance.BeginInvoke()
                    };
                    PSObject outputBlock = new PSObject(obj);

                    Output.Add(outputBlock);
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
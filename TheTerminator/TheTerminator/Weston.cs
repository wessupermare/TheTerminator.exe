using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace TheTerminator
{
    partial class Program
    {
        /// <summary>
        /// Note: The code below has been _directly translated_ from a powershell script, comments and all. Do not hold me responsible for how terrible it is.
        /// </summary>
        static void Weston()
        {
            string startStop = "stop", startup = "";
            int maxThreads = 50;

            //obtain server list (from flat file - same directory as set below)
            Environment.CurrentDirectory = @"C:\PowerShell";
            string[] serverNames = File.ReadAllLines("Servers.txt");
            string[] services = File.ReadAllLines("Services.txt");
            string[] processes = File.ReadAllLines("Processes.txt");

            //Document Start Time in Console:
            Console.WriteLine($"Start {DateTime.Now}");

            //This is the "scriptBlock" that is created and executed in parallel from the RunSpacePool. It takes some required parameters.
            //TODO: Probably double-checking the input.  Potentially could check the connectivity to each server before running the script as well.
            ScriptBlock sb = ScriptBlock.Create(PSScript());

            //create a "RunSpacePool", this allows parallel execution, minimum waiting (this creates the minimum (1) and maximum ($maxThreads) runspaces allowed.
            RunspacePool rsPool = RunspaceFactory.CreateRunspacePool(1, maxThreads);
            rsPool.Open();
            List<PSObject> Output = new List<PSObject>();
            dynamic Results; //Translators note: this isn't ever used, but (in the interest of _exact translation_) it has been left in as a dynamic.

            //loop through each server in the file and create a runspace instance to execute the script block against.
            foreach (string server in serverNames)
            {
                PowerShell PSinstance = PowerShell.Create().AddScript(sb.ToString()).AddArgument(server).AddArgument(startStop).AddArgument(services).AddArgument(processes).AddArgument(startup);
                PSinstance.RunspacePool = rsPool;

                Output.Add(new PSObject(new
                {
                    Server = server,
                    Pipe = PSinstance,
                    Result = PSinstance.BeginInvoke()
                }));
            }

            //wait for the jobs to be completed before doing anything...
            Console.Write("Waiting..");

            List<WaitHandle> waitHandleList = new List<WaitHandle>();
            waitHandleList.AddRange(ReturnTaskHandles(Output.ToArray()));
            WaitHandle[] waitHandles = waitHandleList.ToArray();
            Task waitingTask = Task.Run(() => WaitHandle.WaitAll(waitHandles));
            do
            {
                Console.Write('.');
                Thread.Sleep(20000);
                foreach (PSObject p in Output)
                {
                    if (((IAsyncResult)p.Properties.Match("Result")[0].Value).IsCompleted == false)
                    {
                        Console.WriteLine($"Waiting on {((string)p.Properties.Match("Server")[0].Value)}");
                    }
                }
            } while (waitingTask.Status != TaskStatus.RanToCompletion);
            Console.WriteLine("All jobs completed!");

            Console.WriteLine($"End {DateTime.Now}");

            rsPool.Dispose();
            rsPool.Close();
        }

        static IEnumerable<WaitHandle> ReturnTaskHandles(PSObject[] input)
        {
            foreach (PSObject item in input)
            {
                yield return ((IAsyncResult)item.Properties.Match("Result")[0].Value).AsyncWaitHandle;
            }
        }
    }
}
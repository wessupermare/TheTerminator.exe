using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTerminator
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Davin();
            Weston();
        }

        static string PSScript()
        {
            return @"param (
                    $svr, #this is the name of the server
                    $stop1, #starting or stopping services (this should always be start or stop)
                    $svcs1, #services are we starting/stopping (string array)
                    $procs1, #processes we are going to try to forcefully kill if running after service stop (ONLY RUNS ON STOP)
                    $startupType1
            #if changing the startupType ""Manual, Automatic, Disabled""
                )
                 Write-EventLog -LogName Application -Source Scripting -EventId 1 -message ""Parameters: $svr $stop1 $svcs1 $procs1 $startupType1""
    

                $remoteExecution = {
                    param (
                        $stop, #starting or stopping services (this should always be start or stop)
                        $svcs, #services are we starting/stopping (string array)
                        $procs, #processes we are going to try to forcefully kill if running after service stop (ONLY RUNS ON STOP)
                        $startupType #if changing the startupType ""Manual, Automatic, Disabled""
                    )
            # Validate if we are going to be creating a new EventLog source named ""Scripting"", or if one already exists:
                    if (!([System.Diagnostics.EventLog]::SourceExists('Scripting'))){
                        New-EventLog -LogName Application -Source Scripting
                        Write-EventLog -LogName Application -Source Scripting -EventId 9995 -message ""This script may not have been created on this machine previously. Creating a 'Scripting' Source in the Application log for future events.""
                    }
                    Write-EventLog -LogName Application -Source Scripting -EventId 9999 -message ""Beginning Services $stop""
                    if($startupType -ne """") {
                        Write-EventLog -LogName Application -Source Scripting -EventId 10002 -message ""Setting Services startup to $startupType""
                    }

                    if($svcs) {
            # loop through services and kill them / log output
            # this could be shortened to just (Stop-Service $services -PassThru -Force -ErrorAction SilentlyContinue) but that removes logging and may toss errors on missing items
                            foreach($service in $svcs)
                            {
                                Write-EventLog -LogName Application -Source Scripting -EventId 10000 -message ""Attempting to $stop $service""

            # Make sure that the service exists before doing stuff on it (avoid errors)
            # ToDo: Make adjustments to remove the TRY/CATCH block below, something cleaner if we are going to keep logging
                                if (Get-Service | Where-Object {$_.Name -eq $service}) {
            # attempt to stop the service
                                    try {
                                        if($stop -eq ""stop"") {
            #
            # Added 5/29/2015 C.Band
            #
            # This should actually set the service into a variable, wait 20 seconds for it to stop, then not find and kill PIDS

            # Set the Service Variable
                                            $s = Get-Service $service

            # verify the service is running (otherwise it'll throw an exception)
                                            if ($s.Status -eq ""Running"") {
            # Gracefully request the service to stop:
                                                $s.Stop()

                                                try {
            # wait several seconds (20 - by default) for the service to stop gracefully
                                                    $s.WaitForStatus(""Stopped"",""00:00:20"")
                                                }
                                                catch {
            # do nothing, this just means the status of the service likely timed-out... This is just more graceful and won't pop us out
                                                }
                                                finally {
            # If the service still has not stopped, find all PIDs related to the service and do a forced kill of them:
                                                    if ($s.Status -ne ""Stopped"") {
                                                        $sPID = (Get-WmiObject -Class Win32_Service | Where { $_.Name -eq $s.Name}).ProcessID
                                                        Write-EventLog -LogName Application -Source Scripting -EventId 20000 -message ""The service didn't stop in time, finding and killing PID: $sPID""
                                                        Stop-Process $sPID -Force -Confirm:$false
                                                    }
                                                }
                                            }
                                        }else {
            # get and start the service
            # TODO: there might be some decent error checking to be done here in case a service sits in the ""starting"" status - like we did for stopping above.
                                            get-service $service | where {$_.status -eq 'stopped'} | Start-Service -PassThru
                                        }
                                        Write-EventLog -LogName Application -Source Scripting -EventId 10001 -message ""Service $service $stop""
                                    }
                                    catch {
                                        Write-EventLog -LogName Application -Source Scripting -EntryType Error -EventId 10002 -message ""ERROR: Service $service failed to $stop!""
                                    }
                
            # attempt to set the startup type to something else (if included in the arguments when the scripts was run)
                                    try {
                                        if($startupType -ne """") {
            # If an argument was not supplied, skip this, otherwise set the service startup type to Manual, Disabled, or Automatic and log it.
                                            Set-Service $service -StartupType $startupType -PassThru
                                            Write-EventLog -LogName Application -Source Scripting -EventId 10010 -message ""Service $service Startup Type Changed to $startupType""
                                        }
                                    }
                                    catch {
                                        Write-EventLog -LogName Application -Source Scripting -EntryType Error -EventId 10011 -message ""ERROR: Service $service failed to change Startup!""
                                    }      
                                } 

                }";
        }
    }
}

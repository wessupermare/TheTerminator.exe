####
# Run this as an Administrator - This will use the context of the current user to execute.
# - This script executes locally but will run PowerShell on all remote devices (REQUIRED!)
# - Also ensure that PSRemoting is enabled and allowed from the device that will execte the script. (https://technet.microsoft.com/en-us/library/hh849694.aspx)
# - This script includes much more error checking and will log to the Application Log on each remote device as it attempts to execute (Source = Scripting)
# - Place this script and the Servers.txt, Services.txt, and Processes.txt to the root of C:\ in a folder named: PowerShell.
#
#
# Author: C. Band
# Company: T2 Technologies Group
# Date: 05/28/2015
#
# -Updated: 05/29/2015: Added in better functionality to check if a service actually stopped, and if not (after 20 seconds) find and kill PIDs.
#                       Removed the SleepTimer Variable - hardcoded 20 seconds
#
# -Updated: 08/10/2017: (By W. Sleeman) Parameterized the file locations, the number of threads, and whether or not the Windows Event Log is (ab)used.
#						Removed demands of excessive permissions.
####
param(
	[Parameter(Mandatory=$True)][string]$fileLocation = "C:\PowerShell",
    [validateset('stop','start')][string]$startStop="stop", #Start or Stop Services
    [validateset('','Manual','Disabled','Automatic')][string]$startup="", #Service Startup Type (blank = no change)
	[switch]$eventLog=$false,
	[int]$maxThreads=50
)

#obtain server list (from flat file - same directory as set below)
Set-Location $fileLocation
$serverNames =  Get-Content Servers.txt
$services = Get-Content Services.txt
$processes = Get-Content Processes.txt

if ($eventLog) {
	#Validate if we are going to be creating a new EventLog source named "Scripting", or if one already exists:
	if (!([System.Diagnostics.EventLog]::SourceExists('Scripting'))){
		New-EventLog -LogName Application -Source Scripting
		Write-EventLog -LogName Application -Source Scripting -EventId 9995 -message "This script may not have been created on this machine previously. Creating a 'Scripting' Source in the Application log for future events."
	}

	Write-EventLog -LogName Application -Source Scripting -EventId 9996 -message "Services Script Started. Start/Stop: $startStop Service Startup Type (blank = no change): $startup On Servers: $serverNames, services $services, and processes $processes"
}

#Document Start Time in Console:
$start = Get-Date
"Start $start"


#This is the "scriptBlock" that is created and executed in parallel from the RunSpacePool. It takes some required parameters.
#TODO: Probably double-checking the input.  Potentially could check the connectivity to each server before running the script as well.
$sb = { 
    param (
        $svr, #this is the name of the server
        $stop1, #starting or stopping services (this should always be start or stop)
        $svcs1, #services are we starting/stopping (string array)
        $procs1, #processes we are going to try to forcefully kill if running after service stop (ONLY RUNS ON STOP)
        $startupType1, #if changing the startupType "Manual, Automatic, Disabled"
		$credential, #The credentials to use on the remote host
		$eventLog #Should events be logged to the windows EventLog
    )
	if ($eventLog) {
		Write-EventLog -LogName Application -Source Scripting -EventId 1 -message "Parameters: $svr $stop1 $svcs1 $procs1 $startupType1"
    }

    $remoteExecution = {
        param (
            $stop, #starting or stopping services (this should always be start or stop)
            $svcs, #services are we starting/stopping (string array)
            $procs, #processes we are going to try to forcefully kill if running after service stop (ONLY RUNS ON STOP)
            $startupType, #if changing the startupType "Manual, Automatic, Disabled"
			$eventLog #Should events be logged to the windows EventLog
        )
		
		if ($eventLog) {
			#Validate if we are going to be creating a new EventLog source named "Scripting", or if one already exists:
			if (!([System.Diagnostics.EventLog]::SourceExists('Scripting'))){
				New-EventLog -LogName Application -Source Scripting
				Write-EventLog -LogName Application -Source Scripting -EventId 9995 -message "This script may not have been created on this machine previously. Creating a 'Scripting' Source in the Application log for future events."
			}
			Write-EventLog -LogName Application -Source Scripting -EventId 9999 -message "Beginning Services $stop"
			if($startupType -ne "") {
				Write-EventLog -LogName Application -Source Scripting -EventId 10002 -message "Setting Services startup to $startupType"
			}
		}

        if($svcs) {
                # loop through services and kill them / log output
                #this could be shortened to just (Stop-Service $services -PassThru -Force -ErrorAction SilentlyContinue) but that removes logging and may toss errors on missing items
                foreach($service in $svcs)
                {
					if ($eventLog) {
						Write-EventLog -LogName Application -Source Scripting -EventId 10000 -message "Attempting to $stop $service"
					}
					
                    #Make sure that the service exists before doing stuff on it (avoid errors)
                    #ToDo: Make adjustments to remove the TRY/CATCH block below, something cleaner if we are going to keep logging
                    if (Get-Service | Where-Object {$_.Name -eq $service}) {
                        #attempt to stop the service
                        try {
                            if($stop -eq "stop") {
                                #
                                #Added 5/29/2015 C.Band
                                #
                                #This should actually set the service into a variable, wait 20 seconds for it to stop, then not find and kill PIDS

                                #Set the Service Variable
                                $s = Get-Service $service

                                #verify the service is running (otherwise it'll throw an exception)
                                if ($s.Status -eq "Running") {
                                    #Gracefully request the service to stop:
                                    $s.Stop()

                                    try {
                                        #wait several seconds (20 - by default) for the service to stop gracefully
                                        $s.WaitForStatus("Stopped","00:00:20")
                                    }
                                    catch {
                                        #do nothing, this just means the status of the service likely timed-out... This is just more graceful and won't pop us out
                                    }
                                    finally {
                                        #If the service still has not stopped, find all PIDs related to the service and do a forced kill of them:
                                        if ($s.Status -ne "Stopped") {
                                            $sPID = (Get-WmiObject -Class Win32_Service | Where { $_.Name -eq $s.Name}).ProcessID
											if ($eventLog) {
												Write-EventLog -LogName Application -Source Scripting -EventId 20000 -message "The service didn't stop in time, finding and killing PID: $sPID"
                                            }
											Stop-Process $sPID -Force -Confirm:$false
                                        }
                                    }
                                }
                            }else {
                                #get and start the service
                                #TODO: there might be some decent error checking to be done here in case a service sits in the "starting" status - like we did for stopping above.
                                get-service $service | where {$_.status -eq 'stopped'} | Start-Service -PassThru
                            }
							if ($eventLog) {
								Write-EventLog -LogName Application -Source Scripting -EventId 10001 -message "Service $service $stop"
							}
                        }
						catch {
							if ($eventLog) {
								Write-EventLog -LogName Application -Source Scripting -EntryType Error -EventId 10002 -message "ERROR: Service $service failed to $stop!"
							}
                        }
                
                        #attempt to set the startup type to something else (if included in the arguments when the scripts was run)
                        try {
                            if($startupType -ne "") {
                                #If an argument was not supplied, skip this, otherwise set the service startup type to Manual, Disabled, or Automatic and log it.
                                Set-Service $service -StartupType $startupType -PassThru
								if ($eventLog) {
									Write-EventLog -LogName Application -Source Scripting -EventId 10010 -message "Service $service Startup Type Changed to $startupType"
								}
                            }
                        }
                        catch {
							if ($eventLog) {
								Write-EventLog -LogName Application -Source Scripting -EntryType Error -EventId 10011 -message "ERROR: Service $service failed to change Startup!"
							}
                        }      
                    } 

                }

            }
       

        #cleanup any running processes IF still running (shouldn't be many (if any))
        #TODO: decide if we even need to have this, since we should be killing services/processes forcefully above
        if($stop -eq "stop") {
			if ($eventLog) {
                Write-EventLog -LogName Application -Source Scripting -EventId 10000 -message "Beginning Process Kill"
			}

			if($procs) {

				#Loop through all of the processes we want to stop now that the services are stopped
				#Could be shortened to one line, but then will not log
				#
				foreach($process in $procs) {
					if ($eventLog) {
						Write-EventLog -LogName Application -Source Scripting -EventId 10001 -message "Killing: $process.EXE"
					}

				   if (Get-Process | Where-Object {$_.Name -eq $process}) {
						try {
							#This will try another avenue than used above if some PIDs are still in play.  Try to kill then drop them
							Kill -ProcessName $process -PassThru -Force
							if ($eventLog) {
								Write-EventLog -LogName Application -Source Scripting -EventId 10004 -message "Process $process Killed!"
							}
						}
						catch {
							if ($eventLog) {
								Write-EventLog -LogName Application -Source Scripting -EntryType Error -EventId 10005 -message "ERROR: Kill process $process failed!"
							}
						}   
					} 
				}
			}
		}
    }
    

    $session = New-PSSession -ComputerName $svr -Credential $credential

    #this invoke will depend on powershell existing or not:
    Invoke-Command -Session $session -ScriptBlock $remoteExecution -ArgumentList @($stop1,$svcs1,$procs1,$startupType1)
}

#create a "RunSpacePool", this allows parallel execution, minimum waiting (this creates the minimum (1) and maximum ($maxThreads) runspaces allowed.
$rsPool = [runspacefactory]::CreateRunspacePool(1,$maxThreads)
$rsPool.Open()
$Output = @()
$Results = @()

#Obtain credentials for remote hosts
$creds = Get-Credential

#loop through each server in the file and create a runspace instance to execute the script block against.
foreach($server in $serverNames) {
    $PSinstance = [powershell]::Create().AddScript($sb).AddArgument($server).AddArgument($startStop).AddArgument($services).AddArgument($processes).AddArgument($startup).AddArgument($creds).AddArgument($eventLog)
    $PSinstance.RunspacePool = $rsPool

    $Output += New-Object PSObject -Property @{
        Server = $server
        Pipe = $PSinstance
        Result = $PSinstance.BeginInvoke()
    }
}

#wait for the jobs to be completed before doing anything...
Write-Host "Waiting.." -NoNewline
Do {
   Write-Host "." -NoNewline
   Start-Sleep -Seconds 20
   foreach ($p in $Output | where {$Output.Result.IsCompleted -contains $false}) {
       $prop = $p.Server
       Write-Host "Waiting on $prop"
   }
   
} While ( $Output.Result.IsCompleted -contains $false )
Write-Host "All jobs completed!"

$end = Get-Date
"End $end"

if ($eventLog) {
	#write out the final event log to show we completed:
	Write-EventLog -LogName Application -Source Scripting -EventId 10009 -message "Services Script Completed"
}

$rsPool.Dispose()
$rsPool.Close()
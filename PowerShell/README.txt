To use the ServicesControl.ps1 script:

YOU MUST BE AN ADMINISTRATOR AND RUN POWERSHELL AS AN ADMINISTRATOR FOR THIS TO WORK!

POWERSHELL MUST BE INSTALLED ON ALL SERVERS FOR THIS TO WORK!

1. Connect to a server in the same domain that you wish to run the script against
2. Copy the ENTIRE PowerShell folder to the root of C:\ on the server it will from from (only one server please)
3. Edit the Processes.txt file to include all Processes.  These are the ".EXE"'s that need to be killed forcefully in the event the service stop doesn't kill them.
4. Edit the Services.txt file to include Services that need to be stopped gracefully (if possible).
5. Edit the Servers.txt file to include all servers the script should run against.

When running the script, open a PowerShell window as an Admin on a Server 2008 or newer Server.  There are several variables/options available with this script:

.\ServicesControl.ps1 - running this without parameters will default to stopping all services and killing processes from the text files for each server in the Servers.txt file

.\ServicesControl.ps1 [start/stop] [manual,disabled,automatic] - this allows to optionally define start or stop (for services), and also to set the startup type on the services.
NOTE: if you want to set the startup type, you MUST also define start or stop first.

EXAMPLES: 
.\ServicesControl.ps1 start automatic (this will start all the services defined and set them to automatically start up on boot)
.\ServicesControl.ps1 stop manual (this will stop all services and set the startup type to Manual)
.\ServicesControl.ps1 start (this will start the services defined and make no change to the statup type already defined)


StartAutoServices.ps1 (no params) ****** This is just an extra script to run against the Servers.txt file.  It will check each server for Automatic services and attempt to start ones that are not started.
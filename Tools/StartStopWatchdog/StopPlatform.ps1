# Stop Watchdog service, kill Platform.exe and HomeOSDashboard processes 

# Variables
$servicename = "HomeOS Hub Watchdog"
$processname = "HomeOS.Hub.Platform"
$appexprocess ="HomeOS.Hub.Dashboard"
$path = "C:\HomeOS2\Hub\Output"

# Verify is HomeOS.Hub.Platform service running 
function IsServiceRunning($servicename)
{
   Write-Host "Verifying status of the $servicename service ..." 
   Write-Host "                                                "

   $arrService = Get-Service -Name $servicename   
   if ($arrService.Status -eq "Running")
   {
        Write-Host "$servicename service is running"
        Write-Host "                               "
        $isservicerunning = $True
        return $True
   }
   else
   {
        Write-Host "$servicename service is not running"
        Write-Host "                                   "
        $isservicerunning = $False
        return $False
   }
}

function StartService($servicename)
{
    Write-Host "Trying to start Watchdog service ..."
    Write-Host "                                    "
    Start-Service $ServiceName

    $arrService = Get-Service -Name $ServiceName
    if ($arrService.Status -eq "Running")
    {   
        Write-Host "$servicename service was successfully started"
	Write-Host "                                             "
    }
    else
    {
        Write-Host "Could not start the $servicename service :("
        Exit
    }
}


function StopService($servicename)
{
    Write-Host "Trying to stop Watchdog service ..."
    Write-Host "                                   "
    Stop-Service $ServiceName

    $arrService = Get-Service -Name $ServiceName
    if ($arrService.Status -ne "Running")
    {   
        Write-Host "Watchdog service was successfully stopped"
	Write-Host "                                         "
    }
    else
    {
        Write-Host "Could not stop the Watchdog service :("
        Exit
    }
}


function KillProcess($processname)
{
    Write-Host "Looking for the running $processname process"  
    $isRunning = (Get-Process | Where-Object {$_.Name -eq $ProcessName}).Count -gt 0

    if($isRunning)
    {
        Write-Host "Found the $processname process, trying to stop it..."
 
        Stop-Process -name $processname -Force -passthru | wait-process | out-null
     
        $isRunning = (Get-Process | Where-Object {$_.Name -eq $ProcessName}).Count -gt 0
        if(!$isRunning)
        { 
           Write-Host "HomeOS.Hub.Platform process was stopped successfully"
           Write-Host "                                                    "
        }
        else
        {
           Write-Host "Could not stop HomeOS.Hub.Platform process"
        } 
    }
    else
    {
       Write-Host "Could not find the HomeOS.Hub.Platform process"
    }    
}


# Program steps
#Check if these files exist
$file1 = "HomeOS.Hub.Watchdog.exe"
$file1 = "HomeOS.Hub.Platform.exe"
$File1Exist = (Test-Path -Path ($root + "\" + $file1))
$File2Exist = (Test-Path -Path ($root + "\" + $file2))

#Stop service iit is running 

if(IsServiceRunning($servicename))
{
   StopService($servicename)
}

#Kill Platform process if it is running..
KillProcess($processname)

#Kill Dashboard process if it is running
KillProcess($appexprocess)
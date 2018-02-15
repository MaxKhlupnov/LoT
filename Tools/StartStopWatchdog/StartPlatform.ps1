# Start Watchdog service and Platform.exe process 

# Variables
$servicename = "HomeOS Hub Watchdog"
$processname = "HomeOS.Hub.Platform"
$root = "C:\HomeOS2\Hub\Output"

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



# Program steps

#Check if these files exist
$file1 = "HomeOS.Hub.Watchdog.exe"
$file1 = "HomeOS.Hub.Platform.exe"
$File1Exist = (Test-Path -Path ($root + "\" + $file1))
$File2Exist = (Test-Path -Path ($root + "\" + $file2))

#Error msg - could not find file(s)
if(!$File1Exist)
{
	Write-Host "Could not the file $file1 to start WatchDog service"
}

if(!$File2Exist)
{
	Write-Host "Could not the file $file2 to start process"
	Exit
}

#Starting Watchdog service...
StartService($servicename)

#Verify service running
IsServiceRunning($servicename)

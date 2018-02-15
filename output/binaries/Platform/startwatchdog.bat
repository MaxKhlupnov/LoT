:: check for administrator rights
net session >nul 2>&1
if NOT %errorLevel% == 0 ( 
   echo You must run with adminstrator privileges
   exit /B 1
)


:: enable network discovery and sharing - needed by hue bridge scout
netsh advfirewall firewall set rule group="Network Discovery" new enable=Yes
netsh advfirewall firewall set rule group="File and Printer Sharing" new enable=Yes

:: remove current firewall settings for HomeOS.Hub.Platform.exe
netsh advfirewall firewall delete rule name="Hub Platform" dir=in program=%outputPath%HomeOS.Hub.Platform.exe profile=private,public localip=any remoteip=any protocol=TCP localport=any remoteport=any
netsh advfirewall firewall delete rule name="Hub Platform" dir=in program=%outputPath%HomeOS.Hub.Platform.exe profile=private,public localip=any remoteip=any protocol=UDP localport=any remoteport=any

:: create firewall settings for HomeOS.Hub.Platform.exe
netsh advfirewall firewall add rule name="Hub Platform" dir=in program=%outputPath%HomeOS.Hub.Platform.exe action=allow enable=yes profile=private,public localip=any remoteip=any protocol=TCP localport=any remoteport=any
netsh advfirewall firewall add rule name="Hub Platform" dir=in program=%outputPath%HomeOS.Hub.Platform.exe action=allow enable=yes profile=private,public localip=any remoteip=any protocol=UDP localport=any remoteport=any 

:: stop WatchDog service if it is running
:: net stop "HomeOS Hub Watchdog"

:: install watchdog in case it isn't already - IF THIS FAILS CHECK THE PATH TO instalutill on your machine.
C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe Watchdog\HomeOS.Hub.Watchdog.exe  

:: Start the watchdog
net start "HomeOS Hub Watchdog"                                                   
 







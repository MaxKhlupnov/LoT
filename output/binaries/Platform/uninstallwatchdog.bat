:: check for administrator rights
net session >nul 2>&1
if NOT %errorLevel% == 0 ( 
   echo You must run with adminstrator privileges
   exit /B 1
)

:: stop  Watchdog 
net stop "HomeOS Hub Watchdog"

:: kill homeos if it is already running
taskkill /F /IM HomeOS.Hub.Platform.exe

:: uninstall Watchdog (this is necessary before re-installing in different directory on the same hub)
 C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe /u Watchdog\HomeOS.Hub.Watchdog.exe

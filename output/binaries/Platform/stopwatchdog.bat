:: check for administrator rights
net session >nul 2>&1
if NOT %errorLevel% == 0 ( 
   echo You must run with adminstrator privileges
   exit /B 1
)

:: stop WatchDog service if it is running
net stop "HomeOS Hub Watchdog"

:: kill homeos if it is already running
taskkill /F /IM HomeOS.Hub.Platform.exe

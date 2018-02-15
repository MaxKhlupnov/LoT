:: check for administrator rights
net session >nul 2>&1
if NOT %errorLevel% == 0 ( 
   echo You must run with adminstrator privileges
   exit /B 1
)

:: what config directory are we working with?
set configDir=Config

IF NOT "%~1" == "" (
set configDir=%1
)

:: stop WatchDog service if it is running
net stop "HomeOS Hub Watchdog"

:: kill homeos if it is already running
taskkill /F /IM HomeOS.Hub.Platform.exe

:: nuke the current config
del /Q Configs\%configDir%\\*

:: stop hosted network
:: netsh wlan stop hostednetwork

:: copy over the fresh config
copy ..\\Platform\\Configs\\%configDir%\\* Configs\\%configDir%

:: make it writeable
:: attrib -r \\Configs\%configDir%\\*

:: !Please Keep this as the last block in the file!
:: Cleans up any DataStore generated files and directories
setlocal
for /F %%i IN ('dir /s /b stream.dat') DO call :delete_stream_root_dir_recursive %%i
goto :eof

:delete_stream_root_dir_recursive <streamFilePath>
rd /q /s "%~dp1..\.."

:eof
endlocal
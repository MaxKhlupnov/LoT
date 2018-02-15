Prerequisites: 
Watchdog service should be installed on your system
by running C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil  <Path>\HomeOS.Hub.Watchdog.exe

First:
Copy StartPlatform, StopPlatform files at your local folder, e.g C:\Users\homelab\Documents\HubSetup

To create desktop shortcuts do next:
1. right click on ps1 file, select "send to Desktop(create shortcut)"
2. on newly created shortcut right click/Properties then copy, paste accordingly

Shortcut name: StartPlatform 
Target: "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -command C:\Users\homelab\Documents\HubSetup\StartPlatform.ps1"
Start in: C:\homeos2\Hub\output\Watchdog (change to your Watchdog folder location accordingly)

Shortcut name: StopPlatform 
Target: "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -command C:\Users\homelab\Documents\HubSetup\StopPlatform.ps1"
Start in: C:\homeos2\Hub\output\Watchdog (change to your Watchdog folder location accordingly)


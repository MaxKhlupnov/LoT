using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;

namespace HomeOS.Hub.Watchdog
{
    [RunInstaller(true)]
    public partial class WatchdogInstaller : System.Configuration.Install.Installer
    {
        public WatchdogInstaller()
        {
            InitializeComponent();
        }
    }
}

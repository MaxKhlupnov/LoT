using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Views
{
    public interface VLogger
    {
        void Log(string format, params string[] args);
    }
}

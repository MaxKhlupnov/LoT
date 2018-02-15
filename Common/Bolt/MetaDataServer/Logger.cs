using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HomeOS.Hub.Common.MDServer
{
    public class Logger
    {
        List<string> buffer;
        public Logger()
        {
            buffer = new List<string>();
        }

        public void Log(string data)
        {
            Console.WriteLine(DateTime.UtcNow.Ticks + ":" + data);
            // buffer.Add(DateTime.UtcNow.Ticks + ":" + data);
        }

        public void Dump(string file)
        {
            File.AppendAllLines(file, buffer);
        }

    }
}

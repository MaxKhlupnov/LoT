using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    public class Logger
    {
        List<string> buffer;
        List<long> times;
        public Logger()
        {
            buffer = new List<string>(2000000);
            times = new List<long>(2000000);
        }

        public void Log(string data)
        {
            // Console.WriteLine(DateTime.UtcNow.Ticks + ":" + data);
            // times.Add(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds);
            // return span.TotalSeconds;
            times.Add(DateTime.UtcNow.Ticks);
            buffer.Add(data);
        }

        public void Dump(string file)
        {
            try
            {
                List<string> tmp = new List<string>(2000000);
                int i = 0;
                for (i = 0; i < times.Count; ++i)
                {
                    tmp.Add(times[i] + ":" + buffer[i]);
                }
                File.AppendAllLines(file, tmp);
            }
            catch (Exception e)
            {
                Console.WriteLine("exception: "+ e);
            }
        }
    }
}

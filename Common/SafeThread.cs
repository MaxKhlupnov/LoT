using System;
using System.Collections.Generic;
using System.Threading;

namespace HomeOS.Hub.Common
{
    public class SafeThread
    {
        Thread thread;
        HomeOS.Hub.Platform.Views.VLogger logger;
        ///unclear how to make this safe

        public SafeThread(ThreadStart start, string name, HomeOS.Hub.Platform.Views.VLogger logger)
        {
            this.logger = logger;
            thread = new Thread(delegate()
            {
                try
                {
                    start();
                }
                catch (Exception exception)
                {
                    //string message = "HomeOS SafeThread named: " + name + ", raised exception: " + exception.GetType();
                    string message = "HomeOS SafeThread named: " + name + ", raised exception: " + exception.ToString();
                    if (logger != null) logger.Log(message);

                    //lets print these messages to stderr as well
                    Console.Error.WriteLine(message);
                }
            }
                    );
            thread.Name = name;
            
        }

        public String Name()
        {
            return this.thread.Name;
        }

        public void Start()
        {
            try
            {
                thread.Start();
            }
            catch (Exception exception)
            {
                if (logger != null) logger.Log("HomeOS SafeThread exception in start(): " + exception.GetType());
            }
        }

        public void Abort()
        {
            try
            {
                thread.Abort();
            }
            catch (Exception exception)
            {
                if (logger != null) logger.Log("HomeOS SafeThread exception in abort(): " + exception.GetType());
            }
        }

        public void SetApartmentState(ApartmentState state)
        {
            try
            {
                thread.SetApartmentState(state);
            }
            catch (Exception exception)
            {
                if (logger != null) logger.Log("HomeOS SafeThread exception in setapartmentstate() : " + exception.GetType());
            }
        }

        public void Join(TimeSpan timeout)
        {
            try
            {
                thread.Join(timeout);
            }
            catch (Exception exception)
            {
                if (logger != null) logger.Log("HomeOS SafeThread:" + thread.Name + " exception in join(timeout): " + exception.GetType());
            }
        }

        public void Join()
        {
            try
            {
                thread.Join();
            }
            catch (Exception exception)
            {
                if (logger != null) logger.Log("HomeOS SafeThread exception in join(): " + exception.GetType());
            }
        }


        public bool IsAlive()
        {
            try
            {
                return thread.IsAlive;
            }
            catch (Exception exception)
            {
                if (logger != null) logger.Log("HomeOS SafeThread exception in isalive(): " + exception.GetType());
            }
            return false;
        }

    }
}

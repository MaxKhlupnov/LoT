﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.Apps.DNW
{
    class MainClass_legacy
    {
        /*
        static void Main(string[] args)
        {
            int len_hours = 24 * 60 * 30;
            int[] windows = { 600, 24 * 60, 7 * 24 * 60 };
            foreach (int window in windows)
            {
                DNW dnwt = new DNW(1, window, "dnw-" + window + "-1.txt");
                for (int i = 1; i <= len_hours; i++)
                {
                    dnwt.ReadObject();
                    dnwt.NumberOfMatches(null);
                }
                dnwt.Finish();


                for (int numberOfStreams = 10; numberOfStreams <= 50; numberOfStreams = numberOfStreams + 10)
                {
                    DNW dnw = new DNW(numberOfStreams, window, "dnw-" + window + "-" + numberOfStreams + ".txt");
                    for (int i = 1; i <= len_hours; i++)
                    {
                        dnw.ReadObject();
                        dnw.NumberOfMatches(null);
                    }
                    dnw.Finish();
                }
            }

        }*/
    }
}

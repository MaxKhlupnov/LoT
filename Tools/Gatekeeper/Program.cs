using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HomeOS.Hub.Common;
using HomeOS.Shared.Gatekeeper;
using HomeOS.Hub.Platform.Gatekeeper;

namespace HomeServiceTest
{
    class Program
    {
        /// <summary>
        /// Flag for service exit on debug builds. 
        /// </summary>
        private static bool serviceExited = false;

        /// <summary>
        /// Marks the service being debugged as having exited.
        /// </summary>
        public static void Exit()
        {
            serviceExited = true;
        }
        static Logger logger;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            ArgumentsDictionary argsDict = ProcessArguments(args);

            //this logger is not being used at the moment
            logger = new Logger((string)argsDict["Log"]);

            Settings.InitSettingsFromFile((string) argsDict["ConfigFile"]);

            HomeService homeService = new HomeService(logger);

            homeService.Start(null);

            while (!serviceExited)
            {
                System.Threading.Thread.Sleep(10000);
            }

            Environment.Exit(homeService.ExitCode);
        }

        /// <summary>
        /// Processes the command line arguments
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private static ArgumentsDictionary ProcessArguments(string[] arguments)
        {
            ArgumentSpec[] argSpecs = new ArgumentSpec[]
            {
                new ArgumentSpec(
                    "Help",
                    '?',
                    false,
                    null,
                    "Display this help message."),
                new ArgumentSpec(
                    "Log",
                    'l',
                    "-",
                    "file",
                    "Log file name ('-' for stdout)"),
                new ArgumentSpec(
                    "ConfigFile",
                    'c',
                    "Settings.xml",
                    null,
                    "Configuration file"),
            };

            ArgumentsDictionary args = new ArgumentsDictionary(arguments, argSpecs);
            if (args.AppSettingsParseError)
            {
                Console.Error.WriteLine("Error in .config file options: ignoring");
            }

            if (args.CommandLineParseError)
            {
                Console.Error.WriteLine("Error in command line arguments at {0}\n", args.ParseErrorArgument);
                Console.Error.WriteLine(args.GetUsage("homeservice"));
                System.Environment.Exit(1);
            }

            if ((bool)args["Help"])
            {
                Console.Error.WriteLine("Runs HomeService\n");
                Console.Error.WriteLine(args.GetUsage("homeservice"));
                System.Environment.Exit(0);
            }

            string configFile = ((string)args["ConfigFile"]);

            if (!File.Exists(configFile))
            {
                Console.Error.WriteLine("ConfigFile {0} not found", configFile);
                System.Environment.Exit(1);
            }

            return args;
        }

    }
}

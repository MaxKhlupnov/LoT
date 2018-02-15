using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

namespace EmailTester
{
    class EmailTester
    {
        static void Main(string[] args)
        {
            var argsDict = ProcessArguments(args);

            Emailer emailer = new Emailer((string)argsDict["SmtpServer"], (string) argsDict["SmtpUser"], (string) argsDict["SmtpPassword"]);

            Notification notification = new Notification();
            //notification.toAddress = (String)argsDict["To"];
            notification.toAddress = "ajbrush@microsoft.com";
            notification.subject = "homeos testing";
            notification.body = "This should just be fine, cheers";

            //this logger maps to stdout
            VLogger logger = new Logger();

            bool result = emailer.Send(notification, logger);

            logger.Log("result of email = " + result);
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
                   "SmtpServer",
                   's',
                   "smtp.live.com",
                   "smtp server name",
                   "The name of the smtp server"),
               new ArgumentSpec(
                   "SmtpUser",
                   'u',
                   "homeos@live.com",
                   "smtp user name",
                   "The name of the smtp user"),
               new ArgumentSpec(
                   "SmtpPassword",
                   'p',
                   "home123$",
                   "smtp server name",
                   "password for the smtp server"),
               new ArgumentSpec(
                   "To",
                   't',
                   "ajbrush@microsoft.com",
                   "whom to send email",
                   "The target of email"),
            };

            ArgumentsDictionary args = new ArgumentsDictionary(arguments, argSpecs);
            if (args.AppSettingsParseError)
            {
                Console.Error.WriteLine("Error in .config file options: ignoring");
            }

            if (args.CommandLineParseError)
            {
                Console.Error.WriteLine("Error in command line arguments at {0}\n", args.ParseErrorArgument);
                Console.Error.WriteLine(args.GetUsage("PlatformPackager"));
                System.Environment.Exit(1);
            }

            if ((bool)args["Help"])
            {
                Console.Error.WriteLine("Packages platform binaries\n");
                Console.Error.WriteLine(args.GetUsage("PlatformPackager"));
                System.Environment.Exit(0);
            }

            if ((string.IsNullOrEmpty((string) args["To"])))
            {
                Console.Error.WriteLine("You must supply a valid to address");
                Console.Error.WriteLine(args.GetUsage("EmailTester"));
                System.Environment.Exit(1);
            }

              return args;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Common
{
    public abstract class EmailerBase
    {
        protected string smtpServer;
        protected string smtpUsername;
        protected string smtpPassword;
        protected VLogger logger;
        protected EmailerBase(string smtpServer, string smtpUsername, string smtpPassword, VLogger logger)
        {
            this.smtpServer = smtpServer;
            this.smtpUsername = smtpUsername;
            this.smtpPassword = smtpPassword;
            this.logger = logger;
        }

        public abstract Tuple<bool, string> Send(Notification notification);
    }

    public class Emailer : EmailerBase
    {
        public Emailer(string smtpServer, string smtpUsername, string smtpPassword, VLogger logger) : base(smtpServer, smtpUsername, smtpPassword, logger)
        {
        }

        public override Tuple<bool, string> Send(Notification notification)
        {
            string error = "";
            if (string.IsNullOrWhiteSpace(base.smtpServer) ||
                string.IsNullOrWhiteSpace(base.smtpUsername) ||
                string.IsNullOrWhiteSpace(base.smtpPassword) ||
                string.IsNullOrWhiteSpace(notification.toAddress))
            {
                error = "Cannot send email. Email Setup not done correctly";
                base.logger.Log(error);
                return new Tuple<bool, string>(false, error);
            }

            MailMessage message = new MailMessage();

            message.From = new MailAddress(base.smtpUsername);

            message.To.Add(notification.toAddress);

            message.Subject = notification.subject;
            message.Body = notification.body;

            if (notification.attachmentList != null)
            {
                foreach (Attachment attachment in notification.attachmentList)
                {
                    message.Attachments.Add(attachment);
                }
            }

            try
            {
                SmtpClient smtpClient = new SmtpClient(base.smtpServer);
                smtpClient.Credentials = new System.Net.NetworkCredential(base.smtpUsername, base.smtpPassword);
                smtpClient.EnableSsl = true;

                smtpClient.Send(message);
            }
            //this usually happens when the port is blocked
            catch (System.Net.Mail.SmtpException exception)
            {
                error = string.Format("SmtpException while sending direct message: {0}", exception.Message);
                // no need to log this here. it will get logged at the caller if its important. base.logger.Log(error);
                return new Tuple<bool, string>(false, error);
            }
            //some other unknown exception
            catch (Exception exception)
            {
                error = string.Format("Exception while sending direct message: {0}", exception.ToString());
                base.logger.Log(error);
                return new Tuple<bool, string>(false, error);
            }

            return new Tuple<bool, string>(true, "");
        }

    }
}

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Tools.UpdateHelper
{
    public class SecureFtpRepoUpdate
    {
        private class FtpState
        {
            private ManualResetEvent wait;
            private FtpWebRequest request;
            private string argument;
            private Exception operationException = null;
            string status;
            Stream responseStream;

            public FtpState()
            {
                wait = new ManualResetEvent(false);
            }
            public ManualResetEvent OperationComplete
            {
                get { return wait; }
            }

            public FtpWebRequest Request
            {
                get { return request; }
                set { request = value; }
            }

            public string Argument
            {
                get { return argument; }
                set { argument = value; }
            }
            public Exception OperationException
            {
                get { return operationException; }
                set { operationException = value; }
            }

            public string StatusDescription
            {
                get { return status; }
                set { status = value; }
            }

            public Stream ResponseStream
            {
                get { return responseStream; }
                set { responseStream = value; }
            }
        }

        /// <summary>
        /// Delete file on the server.
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static void DeleteFile(Uri serverUri, string userName, string password, bool enableSSL)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException(String.Format("{0} scheme is not ftp", serverUri.AbsoluteUri));
            }
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.EnableSsl = enableSSL;
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(userName, password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Utils.configLog("I", String.Format("Delete status: {0}", response.StatusDescription));
            response.Close();
        }

        private static string ReadStreamContentAsString(Stream stream)
        {
            string content = "";
            const int BUF_SIZE = 1024;
            Byte[] buffer = new Byte[BUF_SIZE];

            while (true)
            {

                int bytes = stream.Read(buffer, 0, buffer.Length);
                content += Encoding.ASCII.GetString(buffer, 0, bytes);

                if (bytes < buffer.Length)
                {
                    break;
                }
            }

            return content;
        }

        /// <summary>
        /// ListDirectory on remote server
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static string[] ListDirectory(Uri serverUri, string userName, string password, bool details, bool enableSSL)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException(String.Format("{0} scheme is not ftp", serverUri.AbsoluteUri));
            }
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.EnableSsl = enableSSL;
            if (details)
            {
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            }
            else
            {
                request.Method = WebRequestMethods.Ftp.ListDirectory;
            }

            request.Credentials = new NetworkCredential(userName, password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Utils.configLog("I", String.Format("ListDirectory status: {0}", response.StatusDescription));

            string[] seperator = { "\r\n" };
            string respText = ReadStreamContentAsString(response.GetResponseStream());
            string[] fileArray = respText.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

            response.Close();

            return fileArray;

        }

        /// <summary>
        /// Returns the size of file on remote server
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static long GetFileSize(Uri serverUri, string userName, string password, bool enableSSL)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException(String.Format("{0} scheme is not ftp", serverUri.AbsoluteUri));
            }
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.EnableSsl = enableSSL;
            request.UsePassive = true;
            request.Method = WebRequestMethods.Ftp.GetFileSize;
            request.Credentials = new NetworkCredential(userName, password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Utils.configLog("I", String.Format("GetFileSize status: {0}", response.StatusDescription));

            string[] seperator = { "\r\n" };
            string[] result = response.StatusDescription.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

            long fileSize = long.Parse(result[0].Substring(4));
            response.Close();
            return fileSize;
        }

        /// <summary>
        /// Returns the size of file on remote server
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static string GetFileDateTimeStamp(Uri serverUri, string userName, string password, bool enableSSL)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException(String.Format("{0} scheme is not ftp", serverUri.AbsoluteUri));
            }
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.EnableSsl = enableSSL;
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            request.Credentials = new NetworkCredential(userName, password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Utils.configLog("I", String.Format("GetFileDateTimeStamp status: {0}", response.StatusDescription));

            string[] seperator = { "\r\n" };
            string[] result = response.StatusDescription.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

            string dateTimeStamp = result[0].Substring(4);
            response.Close();
            return dateTimeStamp;
        }

        /// <summary>
        /// Create a directory on the remote ftp server.
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static void MakeDirectory(Uri serverUri, string userName, string password, bool enableSSL)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException(String.Format("{0} scheme is not ftp", serverUri.AbsoluteUri));
            }
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.EnableSsl = enableSSL;
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(userName, password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Utils.configLog("I", String.Format("MakeDirectory status: {0}", response.StatusDescription));
            response.Close();
        }

        /// <summary>
        /// Removes a directory on the remote ftp server.
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static void RemoveDirectory(Uri serverUri, string userName, string password, bool enableSSL)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException(String.Format("{0} scheme is not ftp", serverUri.AbsoluteUri));
            }
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.EnableSsl = enableSSL;
            request.Method = WebRequestMethods.Ftp.RemoveDirectory;
            request.Credentials = new NetworkCredential(userName, password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Utils.configLog("I", String.Format("RemoveDirectory status: {0}", response.StatusDescription));
            response.Close();
        }

        /// <summary>
        /// Uploads the specified local file to the ftp server location.
        /// </summary>
        /// <param name="serverUri">The url that is the name of the file being uploaded to the server. </param>
        /// <param name="filename">The name of the file on the local machine. </param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static void UploadFile(Uri serverUri, string filepath, string userName, string password, bool enableSSL)
        {
            HandleFileTransfer(serverUri, filepath, true /*up*/, userName, password, enableSSL);
        }

        /// <summary>
        /// Downloads the specified remote file on the ftp server locally.
        /// </summary>
        /// <param name="serverUri">The url that is the name of the file being downloaded from the server. </param>
        /// <param name="filename">The name of the file on the local machine. </param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="enableSSL"></param>
        public static void DownloadFile(Uri serverUri, string filepath, string userName, string password, bool enableSSL)
        {
            HandleFileTransfer(serverUri, filepath, false /*up*/, userName, password, enableSSL);
        }

        private static void HandleFileTransfer(Uri serverUri, string filepath, bool up, string userName, string password, bool enableSSL)
        {
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new ArgumentException(String.Format("{0} scheme is not ftp", serverUri.AbsoluteUri));
            }

            ManualResetEvent waitObject;

            FtpState state = new FtpState();
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
            request.EnableSsl = enableSSL;

            if (up)
            {
                request.Method = WebRequestMethods.Ftp.UploadFile;
            }
            else
            {
                request.Method = WebRequestMethods.Ftp.DownloadFile;
            }

            request.Credentials = new NetworkCredential(userName, password);

            // Store the request in the object that we pass into the 
            // asynchronous operations.
            state.Request = request;
            state.Argument = filepath;

            // Get the event to wait on.
            waitObject = state.OperationComplete;

            if (up)
            {
                // Asynchronously get the stream for the file contents.
                request.BeginGetRequestStream(
                    new AsyncCallback(EndGetStreamCallback),
                    state
                );
            }
            else
            {
                request.BeginGetResponse(
                    new AsyncCallback(EndGetResponseCallback),
                    state
                );
            }

            // Block the current thread until all operations are complete.
            waitObject.WaitOne();

            // The operations either completed or threw an exception. 
            if (state.OperationException != null)
            {
                throw state.OperationException;
            }
            else
            {
                Utils.configLog("I", String.Format("The operation completed - {0}", state.StatusDescription));
            }

        }

        private static void EndGetStreamCallback(IAsyncResult ar)
        {
            FtpState state = (FtpState)ar.AsyncState;

            FileStream stream = null;
            Stream requestStream = null;
            // End the asynchronous call to get the request stream. 
            try
            {
                requestStream = state.Request.EndGetRequestStream(ar);
                // Copy the file contents to the request stream. 
                const int bufferLength = 2048;
                byte[] buffer = new byte[bufferLength];
                int count = 0;
                int readBytes = 0;

                stream = File.OpenRead(state.Argument);
                do
                {
                    readBytes = stream.Read(buffer, 0, bufferLength);
                    requestStream.Write(buffer, 0, readBytes);
                    count += readBytes;
                }
                while (readBytes != 0);
                Utils.configLog("I", String.Format("Writing {0} bytes to the stream.", count));

                // IMPORTANT: Close the request stream before sending the request.
                requestStream.Close();
                requestStream = null;
                // Asynchronously get the response to the upload request.
                state.Request.BeginGetResponse(
                    new AsyncCallback(EndGetResponseCallback),
                    state
                );
            }
            // Return exceptions to the main application thread. 
            catch (Exception e)
            {
                Utils.configLog("E", String.Format("Could not get the request stream"));
                state.OperationException = e;
                state.OperationComplete.Set();
                return;
            }
            finally
            {
                if (null != requestStream)
                {
                    requestStream.Close();
                }
                if (null != stream)
                {
                    stream.Close();
                }
            }
        }

        // The EndGetResponseCallback method   
        // completes a call to BeginGetResponse. 
        private static void EndGetResponseCallback(IAsyncResult ar)
        {
            FtpState state = (FtpState)ar.AsyncState;
            FtpWebResponse response = null;
            FileStream stream = null;
            try
            {
                response = (FtpWebResponse)state.Request.EndGetResponse(ar);
                state.StatusDescription = response.StatusDescription;
                state.ResponseStream = response.GetResponseStream();


                if (state.Request.Method == WebRequestMethods.Ftp.DownloadFile)
                {
                    const int bufferLength = 2048;
                    byte[] buffer = new byte[bufferLength];
                    int count = 0;
                    int writeBytes = 0;

                    stream = File.OpenWrite(state.Argument);
                    do
                    {
                        writeBytes = state.ResponseStream.Read(buffer, 0, bufferLength);
                        stream.Write(buffer, 0, writeBytes);
                        count += writeBytes;
                    }
                    while (writeBytes != 0);
                    Utils.configLog("I", String.Format("Writing {0} bytes to the file.", count));

                }

                // Signal the main application thread that  
                // the operation is complete.
                state.OperationComplete.Set();
            }
            // Return exceptions to the main application thread. 
            catch (Exception e)
            {
                Utils.configLog("E", String.Format("Error getting response."));
                state.OperationException = e;
                state.OperationComplete.Set();
            }
            finally
            {
                if (null != response)
                {
                    response.Close();
                }
                if (null != stream)
                {
                    stream.Close();
                }
            }
        }

        public static bool GetZipFromUrl(Uri zipUrl, string localZipFile, string user, string password, bool enableSSL)
        {
            bool success = false;
            try
            {
                DownloadFile(zipUrl, localZipFile, user, password, enableSSL);

                if (File.Exists(localZipFile))
                {
                    success = true;
                }
            }
            catch(Exception)
            {
            }
            return success;
        }
    }
}


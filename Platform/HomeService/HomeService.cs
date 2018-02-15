// -
// <copyright file="HomeService.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -

namespace HomeOS.Hub.Platform.Gatekeeper
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.ServiceProcess;
    using HomeOS.Hub.Common;
    using HomeOS.Shared;
    using HomeOS.Shared.Gatekeeper;
    using HomeOS.Hub.Platform.Views;

    /// <summary>
    /// Represents the gatekeeper home service.
    /// </summary>
    public partial class HomeService 
    {
        private const int RegConnectionLivenessCheckIntervalSecs = 60;

        /// <summary>
        /// The main connection to the cloud service.
        /// </summary>
        private ServiceConnection registrationConnection;

        VLogger logger;

        /// <summary>
        /// Initializes a new instance of the HomeService class.
        /// </summary>
        public HomeService()
        {

            logger = new Logger();
        }

        public HomeService(VLogger logger) 
        {
            this.logger = logger;
        }

        public int ExitCode { get; set; }

        /// <summary>
        /// Starts the HomeService.
        /// </summary>
        /// <param name="args">Arguments for service Start command.</param>
        public void Start(string[] args)
        {
            this.OnStart(args);
        }

        /// <summary>
        /// Stops the HomeService.
        /// </summary>
        public void Stop()
        {
            this.OnStop();
        }

        /// <summary>
        /// Handler for the Start command.
        /// </summary>
        /// <param name="args">Arguments for service Start command.</param>
        protected void OnStart(string[] args)
        {
            if (string.IsNullOrEmpty(Settings.HomeId) ||
                string.IsNullOrEmpty(Settings.ServiceHost) ||
                Settings.ServicePort <= 0)
            {
                logger.Log("ERROR: not all parameters ({0},{1},{2}) home are there for homeservice to start. Not starting", Settings.ServiceHost,
                                                                  Settings.ServicePort.ToString(),
                                                                  Settings.HomeId);
            }

            logger.Log("Starting HomeService to {0}:{1} with homeid {2}", Settings.ServiceHost, 
                                                                  Settings.ServicePort.ToString(), 
                                                                  Settings.HomeId);

            Register();

            //create a timer that checks on the health of the registration connection periodically
            var scanTimer = new System.Timers.Timer(RegConnectionLivenessCheckIntervalSecs*1000);
            scanTimer.Enabled = true;
            scanTimer.Elapsed += new System.Timers.ElapsedEventHandler(CheckRegConnectionLiveness);
        }

        private void CheckRegConnectionLiveness(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.registrationConnection == null ||
                this.registrationConnection.Socket == null ||
                !this.registrationConnection.Socket.Connected)
            {
                logger.Log("HomeService: Registration connection is dead. Attempting to register again.");

                Register();
            }
        }

        private void Register() 
        {
            // -
            // Register ourselves with the cloud service.
            // Then wait for it to send us client forwarding requests.
            // -
            Socket socket = StaticUtilities.CreateConnectedSocket(
                Settings.ServiceHost,
                Settings.ServicePort);
            if (socket == null)
            {
                this.ExitCode = 1066;  // 1066 = "The service has returned a service-specific error code."  Or 10054? 10064? 10065?
                OnStop();
                return;
            }

            this.registrationConnection = new ServiceConnection(
                Settings.ServiceHost,
                socket,
                0,
                this.Forwarding, logger);

        }

        /// <summary>
        /// Handler for the Stop command.
        /// </summary>
        protected void OnStop()
        {
            if (this.registrationConnection != null)
            {
                this.registrationConnection.Close();
                this.registrationConnection = null;
            }
        }

        /// <summary>
        /// Handler for forwarded connections.
        /// </summary>
        /// <param name="connection">
        /// The connection being forwarded.
        /// </param>
        /// <returns>
        /// True if forwarding was established, false otherwise.
        /// </returns>
        private bool Forwarding(ServiceConnection connection)
        {
            Socket localService = StaticUtilities.CreateConnectedSocket(
                "localhost",
                HomeOS.Hub.Common.Constants.InfoServicePort);

            NetworkStream netstream = new NetworkStream(localService, true /*ownSocket*/);

            if (localService != null)
            {
                Forwarder forwarder = new Forwarder(
                    netstream,
                    connection.GetStream(),
                    this.StopForwarding, null);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handler for end-of-forwarding event.
        /// </summary>
        /// <param name="forwarder">The forwarder that is closing.</param>
        private void StopForwarding(HomeOS.Shared.Gatekeeper.Forwarder forwarder)
        {
        }
    }
}

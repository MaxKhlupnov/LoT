using System;
using System.Collections.Generic;

namespace HomeOS.Hub.Common
{

    public sealed class ModuleInfo : HomeOS.Hub.Platform.Views.VModuleInfo
    {
        /// <summary>
        /// The friendlyname assigned by users to the module (must be unique across the system)
        /// </summary>
        string friendlyName;

        /// <summary>
        /// The name of the app as displayed to users
        /// </summary>
        string appName;

        /// <summary>
        /// The directory where the binaries of this module sit
        /// </summary>
        string binaryDirectory;

        /// <summary>
        /// The name of the binary to invoke when the module is started
        /// </summary>
        string binaryName;

        /// <summary>
        /// The arguments with which to start the module
        /// </summary>
        string[] args;

        /// <summary>
        /// the working directory of the module
        /// </summary>
        string workingDirectory;

        /// <summary>
        /// whether this module should be automatically started when the platform starts
        /// </summary>
        public bool AutoStart {get; private set;}

        /// <summary>
        /// whether this module is a background module (which shouldn't be shown on the UI)
        /// </summary>
        public bool Background { get; set; }

        /// <summary>
        /// the manifest of this module which represents the roles that it needs
        /// </summary>
        Manifest manifest;

        /// <summary>
        ///  the version of this module that is actually running
        /// </summary>
        string runningVersion;

        /// <summary>
        ///  the version of this module that is desired as per the configuration
        /// </summary>
        string desiredVersion;

        /// <summary>
        ///  the base URL of this module
        /// </summary>
        string baseURL; 


        public ModuleInfo(string friendlyName, string appName, string binaryName, string workingDirectory, bool autoStart, params string[] args)
        {
            this.friendlyName = friendlyName;

            this.appName = appName;
            this.binaryName = binaryName;
            this.workingDirectory = workingDirectory;
            this.AutoStart = autoStart;

            if (args == null)
                this.args = new string[0];
            else
                this.args = args;

            Background = false;
            this.desiredVersion = Constants.UnknownHomeOSUpdateVersionValue;
        }

        public override string[] Args()
        {
            return args; 
        }

        public override string FriendlyName()
        {
            return friendlyName;
        }

        //public string GetIconPath()
        //{
        //    return Constants.AddInRoot + "\\AddIns\\" + this.binaryName + "\\icon.png";
        //}

        
        public override string AppName()
        {
            return appName;
        }

        public override string BinaryDir()
        {
            return binaryDirectory;
        }

        public override string BinaryName()
        {
            return binaryName;
        }

        public override string WorkingDir()
        {
            return workingDirectory;
        }

        public void SetWorkingDir(string directory)
        {
            workingDirectory = directory;
        }

        public Manifest GetManifest()
        {
            return this.manifest;
        }


        public void SetManifest(Manifest manifest)
        {
            this.manifest = manifest;
        }

        public string GetDesiredVersion()
        {
            return this.desiredVersion;
        }

        public string GetRunningVersion()
        {
            return this.runningVersion; 
        }

        public void SetDesiredVersion(string v)
        {
            this.desiredVersion = v;
        }

        public void SetRunningVersion(string v)
        {
            this.runningVersion = v; 
        }

        public override string BaseURL()
        {
            return this.baseURL;
        }

        public void SetBaseURL(string baseURL)
        {
            this.baseURL = baseURL;
        }

        public void SetBinaryDir(string binaryDir)
        {
            this.binaryDirectory = binaryDir;
        }
    }
}
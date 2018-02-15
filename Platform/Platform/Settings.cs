using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Platform
{

    public class Settings
    {
        public sealed class SettingsRef<T>
        {
            private Func<T> getter;
            private Action<T> setter;
            public SettingsRef(Func<T> getter, Action<T> setter)
            {
                this.getter = getter;
                this.setter = setter;
            }
            public T Value
            {
                get { return getter(); }
                set { setter(value); }
            }
        }

        /// <summary>
        /// organization id
        /// </summary>
        public static string OrgId { get; private set; }

        /// <summary>
        /// study id
        /// </summary>
        public static string StudyId { get; private set; }

        /// <summary>
        /// stores our homeid when configured
        /// </summary>
        public static string HomeId { get; private set; }

        /// <summary>
        /// Our wifi credentials
        /// </summary>
        public static string WifiSsid { get; private set; }
        public static string WifiKey { get; private set; }

        /// <summary>
        /// directory where configuration files sit
        /// </summary>
        public static string ConfigDir { get; private set; }

        /// <summary>
        /// directory within which modules store their data
        /// </summary>
        public static string ModuleWorkingDirBase { get; private set; }

        #region these settings impact how we run
        
        /// <summary>
        /// A development convenient. Use a non-standard running mode (e.g., yourname) if you want specific things to 
        /// happen (and nothing else, as no module is automatically started in this mode)
        /// </summary>
        public static string RunningMode { get; private set; }

        /// <summary>
        /// whether we should stay offline
        /// </summary>
        public static bool StayOffline { get; private set; }

        /// <summary>
        /// Whether we should enforce security policies
        /// </summary>
        public static bool EnforcePolicies { get; set; }

        #endregion

        #region variables related to log file management
        /// <summary>
        /// The file where logs are written
        /// </summary>
        public static string LogFile  { get; private set; }

        /// <summary>
        /// The directory where logs are archived. If an absolute path is not specified, this is interpreted to a subdirectory of where logs sit
        /// </summary>
        public static string LogArchivalDir  { get; private set; }

        /// <summary>
        /// the threshold at which we should rotate logs (in number of lines)
        /// </summary>
        public static ulong LogRotationThreshold { get; private set; }

        /// <summary>
        /// whether we should automatically sync the logs
        /// </summary>
        public static bool AutoSyncLogs { get; private set; }

        // we just use the data store account credentials
        //public static string LogArchivalAccountName { get; private set; }
        //public static string LogArchivalAccountKey { get; private set; }

        #endregion

        #region settings related to cloud resources
        /// <summary>
        /// The base URL for where the HomeStore is located
        /// </summary>
        public static string HomeStoreBase { get; private set; }

       
        /// <summary>
        /// Where to look for modules when updating them
        /// </summary>
        public static string RepositoryURIs { get; private set; }

        /// <summary>
        /// Where the gatekeeper sits
        /// </summary>
        public static string GatekeeperURI { get; private set; }

        /// <summary>
        /// Where to send heartbeats
        /// </summary>
        public static string HeartbeatServiceHost { get; private set; }

        /// <summary>
        /// Azure datastore account name
        /// </summary>
        public static string DataStoreAccountName { get; private set; }
                
        /// <summary>
        /// Azure datastore account key
        /// </summary>
        public static string DataStoreAccountKey { get; private set; }

        /// <summary>
        /// Where to send email for cloud relay
        /// </summary>
        public static string EmailServiceHost { get; private set; }


        #endregion

        #region various timeouts in dealing with modules
        /// <summary>
        /// How long to wait for port registration calls
        /// </summary>
        public static int PortRegisterDelay { get; private set; }

        ///<summary>
        /// Time allowed for each module to execute its Stop(). Platform is waiting for this to end, so it can kill and wipe the module.
        ///</summary>
        public static int MaxStopExecutionTime { get; private set; } 

        ///<summary>
        /// Time allowed for each module to execute its Port Deregistered. Because the module and the corresponding port object need to be wiped out.
        ///</summary>
        public static int MaxPortDeregisteredExecutionTime { get; private set; }

        ///<summary>
        /// When the appdomain is to be unloaded a module maybe executing unmanaged code or executing a finally block, 
        /// in which case a CannotUnloadAppDomainException will be thrown. So we re-try unloading untill a certain time:
        ///</summary>
        public static int MaxFinallyBlockExecutionTime { get; private set; }

        #endregion

        #region how cloud services run
        /// <summary>
        /// How frequently to look for configuration
        /// </summary>
        public static int ConfigLookupFrequency { get; private set; }

        /// <summary>
        /// How frequently we refresh the information in home store
        /// </summary>
        public static int HomeStoreRefreshIntervalsMins { get; private set; }

        /// <summary>
        /// cloud services related 
        /// </summary>
        public static string HeartbeatServiceMode { get; private set; }

        /// <summary>
        /// Heartbeat interval in minutes
        /// </summary>
        public static UInt32 HeartbeatIntervalMins { get; private set; }


        #endregion

        #region settings related to sending email

        public static string NotificationEmail { get; private set; }
         public static string SmtpServer {get;private set;}
         public static string SmtpUser {get; private set;}
         public static string SmtpPassword { get; private set; }

        #endregion

         /// <summary>
        /// The dictionary in which we store (non-private) settings
        /// </summary>
        public static Dictionary<string, SettingsRef<object>> SettingsTable = null;

        /// <summary>
        /// The dictionary in which we store private settings
        /// </summary>
        public static Dictionary<string, SettingsRef<object>> PrivateSettingsTable = null;

        public static void Initialize()
        {
            Settings.SettingsTable = new Dictionary<string, SettingsRef<object>>();
            Settings.PrivateSettingsTable = new Dictionary<string, SettingsRef<object>>();

            try
            {
                SettingsTable["OrgId"] = new SettingsRef<object>(() => Settings.OrgId, v => { Settings.OrgId = v.ToString(); });
                SettingsTable["StudyId"] = new SettingsRef<object>(() => Settings.StudyId, v => { Settings.StudyId = v.ToString(); });
                SettingsTable["HomeId"] = new SettingsRef<object>(() => Settings.HomeId, v => { Settings.HomeId = v.ToString(); });

                SettingsTable["ConfigDir"] = new SettingsRef<object>(() => Settings.ConfigDir, v => { Settings.ConfigDir = v.ToString(); });
                SettingsTable["ModuleWorkingDirBase"] = new SettingsRef<object>(() => Settings.ModuleWorkingDirBase, v => { Settings.ModuleWorkingDirBase = v.ToString(); });

                // .... settings related to how we run
                SettingsTable["RunningMode"] = new SettingsRef<object>(() => Settings.RunningMode, v => { Settings.RunningMode = v.ToString(); });
                SettingsTable["StayOffline"] = new SettingsRef<object>(() => Settings.StayOffline, v => { Settings.StayOffline = Convert.ToBoolean(v); });
                SettingsTable["EnforcePolicies"] = new SettingsRef<object>(() => Settings.EnforcePolicies, v => { Settings.EnforcePolicies = Convert.ToBoolean(v); });

                //.... settings related to log management
                SettingsTable["LogFile"] = new SettingsRef<object>(() => Settings.LogFile, v => { Settings.LogFile = v.ToString(); });
                SettingsTable["LogArchivalDir"] = new SettingsRef<object>(() => Settings.LogArchivalDir, v => { Settings.LogArchivalDir = v.ToString(); });
                SettingsTable["LogRotationThreshold"] = new SettingsRef<object>(() => Settings.LogRotationThreshold, v => { Settings.LogRotationThreshold = Convert.ToUInt64(v); });
                SettingsTable["AutoSyncLogs"] = new SettingsRef<object>(() => Settings.AutoSyncLogs, v => { Settings.AutoSyncLogs = Convert.ToBoolean(v); });
                //SettingsTable["LogArchivalAccountName"] = new SettingsRef<object>(() => Settings.LogArchivalAccountName, v => { Settings.LogArchivalAccountName = v.ToString(); });
                //SettingsTable["LogArchivalAccountKey"] = new SettingsRef<object>(() => Settings.LogArchivalAccountKey, v => { Settings.LogArchivalAccountKey = v.ToString(); });

                // .... timeouts related to module communication
                SettingsTable["PortRegisterDelay"] = new SettingsRef<object>(() => Settings.PortRegisterDelay, v => { Settings.PortRegisterDelay = Convert.ToInt32(v); });
                SettingsTable["MaxStopExecutionTime"] = new SettingsRef<object>(() => Settings.MaxStopExecutionTime, v => { Settings.MaxStopExecutionTime = Convert.ToInt32(v); });
                SettingsTable["MaxFinallyBlockExecutionTime"] = new SettingsRef<object>(() => Settings.MaxFinallyBlockExecutionTime, v => { Settings.MaxFinallyBlockExecutionTime = Convert.ToInt32(v); });
                SettingsTable["MaxPortDeregisteredExecutionTime"] = new SettingsRef<object>(() => Settings.MaxPortDeregisteredExecutionTime, v => { Settings.MaxPortDeregisteredExecutionTime = Convert.ToInt32(v); });

                // .... settings related to cloud resources
                SettingsTable["HomeStoreBase"] = new SettingsRef<object>(() => Settings.HomeStoreBase, v => { Settings.HomeStoreBase = v.ToString(); });
                SettingsTable["RepositoryURIs"] = new SettingsRef<object>(() => Settings.RepositoryURIs, v => { Settings.RepositoryURIs = v.ToString(); });
                SettingsTable["GatekeeperURI"] = new SettingsRef<object>(() => Settings.GatekeeperURI, v => { Settings.GatekeeperURI = v.ToString(); });
                SettingsTable["HeartbeatServiceHost"] = new SettingsRef<object>(() => Settings.HeartbeatServiceHost, v => { Settings.HeartbeatServiceHost = v.ToString(); }); 
                SettingsTable["DataStoreAccountName"] = new SettingsRef<object>(() => Settings.DataStoreAccountName, v => { Settings.DataStoreAccountName = v.ToString(); });
                SettingsTable["DataStoreAccountKey"] = new SettingsRef<object>(() => Settings.DataStoreAccountKey, v => { Settings.DataStoreAccountKey = v.ToString(); });



                // .... settings related to how cloud services run
                SettingsTable["ConfigLookupFrequency"] = new SettingsRef<object>(() => Settings.ConfigLookupFrequency, v => { Settings.ConfigLookupFrequency = Convert.ToInt32(v); });
                SettingsTable["HomeStoreRefreshIntervalMins"] = new SettingsRef<object>(() => Settings.HomeStoreRefreshIntervalsMins, v => { Settings.HomeStoreRefreshIntervalsMins = Convert.ToInt32(v); });
                SettingsTable["HeartbeatServiceMode"] = new SettingsRef<object>(() => Settings.HeartbeatServiceMode, v => { Settings.HeartbeatServiceMode = v.ToString(); });
                SettingsTable["HeartbeatIntervalMins"] = new SettingsRef<object>(() => Settings.HeartbeatIntervalMins, v => { Settings.HeartbeatIntervalMins = Convert.ToUInt32(v); });


                SettingsTable["EmailServiceHost"] = new SettingsRef<object>(() => Settings.EmailServiceHost, v => { Settings.EmailServiceHost = v.ToString(); });

                PrivateSettingsTable["SmtpServer"] = new SettingsRef<object>(() => Settings.SmtpServer, v => { Settings.SmtpServer = v.ToString(); });
                PrivateSettingsTable["SmtpUser"] = new SettingsRef<object>(() => Settings.SmtpUser, v => { Settings.SmtpUser = v.ToString(); });
                PrivateSettingsTable["SmtpPassword"] = new SettingsRef<object>(() => Settings.SmtpPassword, v => { Settings.SmtpPassword = v.ToString(); });
                PrivateSettingsTable["NotificationEmail"] = new SettingsRef<object>(() => Settings.NotificationEmail, v => { Settings.NotificationEmail = v.ToString(); });

                PrivateSettingsTable["WifiSsid"] = new SettingsRef<object>(() => Settings.WifiSsid, v => { Settings.WifiSsid = v.ToString(); });
                PrivateSettingsTable["WifiKey"] = new SettingsRef<object>(() => Settings.WifiKey, v => { Settings.WifiKey = v.ToString(); });

                AssignDefaultValues();
            }
            catch (FormatException e)
            {
                throw new Exception("Parameter update to invalid format/value. " + e);
            }
        }


        public static void SetParameter(string name, object value)
        {
            lock (SettingsTable)
            {
                try
                {
                    Settings.SettingsTable[name].Value = value;
                }
                catch (KeyNotFoundException e)
                {
                    throw new KeyNotFoundException("Parameter:" + name + " being updated is invalid." + " " + e);
                }
            }
        }

        public static object GetParameter(string name)
        {
            lock (SettingsTable)
            {
                try
                {
                    return Settings.SettingsTable[name].Value;
                }
                catch (KeyNotFoundException e)
                {
                    throw new KeyNotFoundException("Parameter:" + name + " being updated is invalid." + " " + e);
                }
            }
        }

        public static void SetPrivateParameter(string name, object value)
        {
            lock (PrivateSettingsTable)
            {
                try
                {
                    Settings.PrivateSettingsTable[name].Value = value;
                }
                catch (KeyNotFoundException e)
                {
                    throw new KeyNotFoundException("Parameter:" + name + " being updated is invalid." + " " + e);
                }
            }
        }

        public static object GetPrivateParameter(string name)
        {
            lock (PrivateSettingsTable)
            {
                try
                {
                    return Settings.PrivateSettingsTable[name].Value;
                }
                catch (KeyNotFoundException e)
                {
                    throw new KeyNotFoundException("Parameter:" + name + " being updated is invalid." + " " + e);
                }
            }
        }

        /// <summary>
        /// Assigns default values to various settings
        /// </summary>
        private static void AssignDefaultValues()
        {
            //...
            // DO NOT assign null as default value. Use an empty string or equivalent if you don't want to initialize a variable
            //...

            OrgId = "Default";
            StudyId = "Default";

            // homeid has no default
            HomeId = String.Empty;

            ConfigDir = Constants.PlatformBinaryDir + "\\..\\..\\Configs\\Config";
            ModuleWorkingDirBase = Constants.PlatformBinaryDir + "\\..\\..\\Data";

            //... settings related to how we run
            RunningMode = "standard";
            StayOffline = false;
            EnforcePolicies = true;

            // .... settings related to log management
            LogFile = Constants.PlatformBinaryDir + "\\..\\..\\Data\\Platform\\homeos.log";
            //LogFile  = ":stdout";
            LogArchivalDir = "archived-logs";
            LogRotationThreshold = 1000;
            AutoSyncLogs = true;
            //LogArchivalAccountName = String.Empty;
            //LogArchivalAccountKey = String.Empty;

            // .... location of cloud resources
            HomeStoreBase = "file:///" + Constants.PlatformBinaryDir + "/../../HomeStore";
            
            RepositoryURIs = HomeStoreBase + "/repository";
            GatekeeperURI = "www.lab-of-things.net";
            HeartbeatServiceHost = "www.lab-of-things.net";
            DataStoreAccountName = "";
            DataStoreAccountKey = "";

            PortRegisterDelay = 10000;
            MaxStopExecutionTime = 5000;
            MaxPortDeregisteredExecutionTime = 5000;
            MaxFinallyBlockExecutionTime = 30000;

            ConfigLookupFrequency = 1800000;
            HomeStoreRefreshIntervalsMins = 60;

            HeartbeatServiceMode = "Off"; // options: Off, Production
            HeartbeatIntervalMins = 1;

            EmailServiceHost = "www.lab-of-things.net";

            // .... the following settings are private and sit in PrivateSettings.xml

            NotificationEmail = String.Empty;
            SmtpServer = "";
            SmtpUser = "";
            SmtpPassword = "";

            WifiSsid = String.Empty;
            WifiKey = String.Empty;
        }
    }

}

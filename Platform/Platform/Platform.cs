using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.AddIn.Hosting;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Platform.Views;
using System.Linq;

using HomeOS.Hub.Platform.Authentication;
using System.Xml;
using System.Xml.Linq;


//using HomeOS.Hub.Platform.VirtualRouter.Wlan;

namespace HomeOS.Hub.Platform
{
    public class Platform : MarshalByRefObject, VPlatform, ScoutViewOfPlatform, IDisposable, AuthSvcViewOfPlatform, SafeServicePolicyDecider
    {
        const string DEFAULT_COMMAND_LINE_ARG_VAL = "_d_e_f_a_u_l_t_";

        /// <summary>
        /// Version of the platform currently running
        /// </summary>
        string platformVersion = Constants.UnknownHomeOSUpdateVersionValue;

        /// <summary>
        /// Modules that are currently running
        /// </summary>
        Dictionary<VModule, ModuleInfo> runningModules;

         /// <summary>
        /// States of the modules that are currently running
        /// </summary>
        Dictionary<VModule, VModuleState> runningModulesStates;
      
        /// <summary>
        /// A delegate invoked by the ConfigLookup service to load new downloaded configuration
        /// </summary>
        /// <param name="configDir"></param>
        delegate void LoadConfig(String configDir);
        ConfigUpdater configLookup=null;

        /// <summary>
        /// All scouts that are currently running
        /// </summary>
        Dictionary<string, Tuple<ScoutInfo, IScout>> runningScouts;

        /// <summary>
        /// Ports that are currently registered
        /// </summary>
        Dictionary<VPort, VModule> registeredPorts;

        /// <summary>
        /// Tokens for all modules that were found
        /// </summary>
        Collection<AddInToken> allAddinTokens;

        /// <summary>
        /// A pointer to the configuration
        /// </summary>
        Configuration config;

        /// <summary>
        /// A pointer to the policy engine
        /// </summary>
        PolicyEngine policyEngine;

        /// <summary>
        /// The logging object where log messages go
        /// </summary>
        Logger logger;

        /// <summary>
        /// A random number generator
        /// </summary>
        Random random;

        /// <summary>
        /// command line arguments passed to platform
        /// </summary>
        string[] arguments; 

        /// <summary>
        /// The handle for the service that exports the Gui (dashboard)
        /// </summary>
        GuiService guiService ;

        /// <summary>
        /// The handle for the connection to the gatekeeper
        /// </summary>
        Gatekeeper.HomeService homeService ;

        /// <summary>
        /// Handle to the generic info service
        /// This is mostly legacy at this point -- it has the redirection from index.html
        /// </summary>
        InfoService infoService;

        /// <summary>
        /// Handle to the service that helps discover platform
        /// </summary>
        DiscoveryHelper discoveryHelperService;

        /// <summary>
        /// Handle to the service that sends heartbeats
        /// </summary>
        HeartbeatService heartbeatService;

        /// <summary>
        /// authentication service
        /// </summary>
        HomeOS.Hub.Platform.Authentication.AuthenticationService authenticationService;

        /// <summary>
        /// authentication service
        /// </summary>
        System.ServiceModel.ServiceHost authenticationServiceHost; 

        /// <summary>
        /// Signals the fact that the platform has been stopped for shutdown
        /// </summary>
        AutoResetEvent eventPlatformStopped;

        /// <summary>
        /// Whether it is safe for the services to go online and do their business
        /// </summary>
        bool safeToGoOnline ;

        /// <summary>
        /// object that contains homestore information
        /// </summary>
        HomeStoreInfo homeStoreInfo;

        /// <summary>
        /// This is where it all begins
        /// </summary>
        /// <param name="arguments">The command line arguments</param>
        static void Main(string[] arguments)
        {            
            Platform homeOS = new Platform(arguments);
            homeOS.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(guiService!=null)
                    guiService.Dispose();
                
                if(infoService!=null)
                    infoService.Dispose();
                
                if (heartbeatService != null)
                    heartbeatService.Dispose();

                if (discoveryHelperService != null)
                    discoveryHelperService.Dispose();

                if (authenticationServiceHost != null)
                    authenticationServiceHost.Close();

                if (authenticationService != null)
                    authenticationService = null; 
               
            }
        }

        /// <summary>
        /// Constructor for the platform
        /// </summary>
        /// <param name="arguments">The command line arguments</param>
        public Platform(string[] arguments)
        {
            this.arguments = arguments;
            this.Initialize(arguments);

        }

        private void Initialize(string[] arguments)
        {
            safeToGoOnline = false;
            guiService = null;
            homeService = null; 

            //initialize various data structures
            random = new Random();
            runningModules = new Dictionary<VModule, ModuleInfo>();
            runningScouts = new Dictionary<string, Tuple<ScoutInfo, IScout>>();
            runningModulesStates = new Dictionary<VModule, VModuleState>();
            registeredPorts = new Dictionary<VPort, VModule>();

            //this initializes the settings to what was default (in the code)
            Settings.Initialize();

            ArgumentsDictionary argsDict = ProcessArguments(arguments);

            //were we supplied a non-default configuration directory?
            if (!DEFAULT_COMMAND_LINE_ARG_VAL.Equals((string)argsDict["ConfigDir"]))
                Settings.SetParameter("ConfigDir", (string) argsDict["ConfigDir"]);
           
            //this overwrites the settings to what was in the configuration file
            config = new Configuration(Settings.ConfigDir);
            config.ParseSettings();

            //now, overwrite the settings with those on the command line
            foreach (var parameter in argsDict.Keys)
            {
                if (parameter.Equals("Help"))
                    continue;

                if (!DEFAULT_COMMAND_LINE_ARG_VAL.Equals((string)argsDict[parameter]))
                    config.UpdateConfSetting(parameter, argsDict[parameter]);
            }


            //initialize the logger
            logger = InitLogger(Settings.LogFile);
            logger.Log("Platform initialized");

            //set the logger for config and read remaining config fines
            config.SetLogger(logger);
            config.ReadConfiguration();

            //initialize the policy enginer
            policyEngine = new PolicyEngine(logger);
            policyEngine.Init(config);

            //rebuild the addin tokens
            this.rebuildAddInTokens();

            homeStoreInfo = new HomeStoreInfo(logger);
            _consoleHandler = new PlatformConsoleCtrlHandlerDelegate(ConsoleEventHandler);
            SetConsoleCtrlHandler(_consoleHandler, true);
        }

        #region Platform console event handling
        enum PlatformConsoleCtrlHandlerCode : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        delegate bool PlatformConsoleCtrlHandlerDelegate(PlatformConsoleCtrlHandlerCode eventCode);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(PlatformConsoleCtrlHandlerDelegate handlerProc, bool add);
        static PlatformConsoleCtrlHandlerDelegate _consoleHandler;

        private bool ConsoleEventHandler(PlatformConsoleCtrlHandlerCode eventCode)
        {
            // Handle close event here...
            switch (eventCode)
            {
                case PlatformConsoleCtrlHandlerCode.CTRL_C_EVENT:
                case PlatformConsoleCtrlHandlerCode.CTRL_CLOSE_EVENT:
                case PlatformConsoleCtrlHandlerCode.CTRL_BREAK_EVENT:
                case PlatformConsoleCtrlHandlerCode.CTRL_LOGOFF_EVENT:
                case PlatformConsoleCtrlHandlerCode.CTRL_SHUTDOWN_EVENT:
                    this.Shutdown();
                    Environment.Exit(0);
                    break;
            }

            return (false);
        }
        #endregion

        /// <summary>
        /// Returns a log object
        /// </summary>
        private Logger InitLogger(string logFileName)
        {
            if (Settings.RunningMode.Equals("unittesting", StringComparison.CurrentCultureIgnoreCase))
                return new Logger(":unittester");

            if (logFileName.Equals(":stdout"))
                return new Logger(logFileName);

            //get the absolute path for archival directory if isn't already absolute
            var logArchivalDirectory = (System.IO.Path.IsPathRooted(Settings.LogArchivalDir)) ? Settings.LogArchivalDir :
                                       new FileInfo(logFileName).Directory + "\\" + Settings.LogArchivalDir;

            return new Logger(logFileName, Settings.LogRotationThreshold, logArchivalDirectory);
        }

        public void StartScout(ScoutInfo sInfo)
        {
            lock (runningScouts)
            {
            if (runningScouts.ContainsKey(sInfo.Name))
            {
                logger.Log("Error: Scout {0} is already running; cannot start again", sInfo.Name);
                return;
            }

            string baseDir = Constants.ScoutRoot + "\\" + sInfo.DllName;
            string dllPath = baseDir + "\\" + sInfo.DllName + ".dll";
            string baseUrl = GetBaseUrl() + "/" + Constants.ScoutsSuffixWeb + "/" + sInfo.DllName;

            try
            {
                string dllFullPath = Path.GetFullPath(dllPath);

                Version vDesired = new Version(sInfo.DesiredVersion);

                if (!File.Exists(dllFullPath))
                {
                    logger.Log("{0} is not present locally. Will try to get version {1} from Repository", sInfo.Name, sInfo.DesiredVersion);

                    GetScoutFromRep(sInfo); // now attempt to start what we got (if we did)
                }
                else
                {
                    
                    Version vLocal = new Version(Utils.GetHomeOSUpdateVersion(dllFullPath + ".config", logger));

                    //if local version and desired versions differ ... 
                    if (vLocal.CompareTo(vDesired) != 0)
                    {
                        //if the desired version is unspecified, pick between the most recent of the latest-on-rep and local
                        if (vDesired.CompareTo(new Version(Constants.UnknownHomeOSUpdateVersionValue)) == 0)
                        {
                            Version vLatestOnRep = new Version(GetVersionFromRep(Settings.RepositoryURIs, sInfo.DllName));

                            if (vLatestOnRep.CompareTo(vLocal) > 0)
                            {
                                logger.Log("Local verison ({0}) is lower than the latest rep version ({1}) for {2}", vLocal.ToString(), vDesired.ToString(), sInfo.Name);

                                GetScoutFromRep(sInfo);
                            }
                            else
                            {
                                logger.Log("Local verison ({0}) is already latest for {1}", vLocal.ToString(), sInfo.Name);
                            }
                        }
                        //we a specific version is desired
                        else
                        {
                            logger.Log("Will try to get specific version {0} for {1} from Repository", sInfo.DesiredVersion, sInfo.Name);

                            GetScoutFromRep(sInfo); // now attempt to start what we got (if we did)
                        }
                    }
                    else
                    {
                        logger.Log("Local verison ({0}) is same as desired version for {1}. Starting that", vDesired.ToString(), sInfo.Name);
                    }
                }

                if (!File.Exists(dllFullPath))
                {
                    logger.Log("Error: Could not fine Scout {0} anywhere; not starting", sInfo.Name);
                    return;
                }

                var vAboutToRun = new Version(Utils.GetHomeOSUpdateVersion(dllFullPath + ".config", logger));

                if (vDesired.CompareTo(new Version(Constants.UnknownHomeOSUpdateVersionValue)) != 0 && vDesired.CompareTo(vAboutToRun) != 0)
                {
                    logger.Log("WARNING: couldn't get the desired version {0} for {1}. Starting {2}", vDesired.ToString(), sInfo.Name, vAboutToRun.ToString());
                }

                logger.Log("starting scout {0} (v{1}) using dll {2} at url {3}", sInfo.Name, vAboutToRun.ToString(), dllPath, baseUrl);

                System.Reflection.Assembly myLibrary = System.Reflection.Assembly.LoadFile(dllFullPath);
                Type myClass = (from type in myLibrary.GetExportedTypes()
                                where typeof(IScout).IsAssignableFrom(type)
                                select type)
                                .Single();

                var scout = (IScout)Activator.CreateInstance(myClass);

                scout.Init(baseUrl, baseDir, this, logger);

                sInfo.SetRunningVersion(vAboutToRun.ToString());

                runningScouts.Add(sInfo.Name, new Tuple<ScoutInfo, IScout>(sInfo, scout));
            }
            catch (Exception e)
            {
                logger.Log("Got exception while starting {0}: {1}", sInfo.Name, e.ToString());
            }
        }
        }

        private string GetBaseUrl()
        {
            string homeIdPart = (string.IsNullOrWhiteSpace(Settings.HomeId)) ? String.Empty 
                                     :  "/" + Settings.HomeId;
   

            return Constants.InfoServiceAddress + homeIdPart;
        }

        private void rebuildAddInTokens()
        {
            lock (this)
            {
                allAddinTokens = GetModuleList(Constants.AddInRoot);
            }
        }
       
        public override string ToString() {
            return "platform";
        }

        public bool SafeToGoOnline()
        {
            return safeToGoOnline;
        }

        /// <summary>
        /// Start the platform
        /// </summary>
        public void Start()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogUnhandledException);
            AppDomain.MonitoringIsEnabled = true;

            // cache the version
            this.platformVersion = Utils.GetHomeOSUpdateVersion(this.GetType().Assembly.CodeBase + ".config", logger);


            //start the basic services
            infoService = new InfoService(this, logger);
            guiService = new GuiService(this, config, homeStoreInfo, logger);
            discoveryHelperService = new DiscoveryHelper(this, logger);

            if (!guiService.IsConfigNeeded())
            {
                ConfiguredStart();
            }

            if (!Settings.RunningMode.Equals("unittesting", StringComparison.CurrentCultureIgnoreCase))
            {
                if (Environment.UserInteractive)
                {
                    ReadCommandsFromConsole();
                }
                else
                {
                    // wait indefinitely, once we support Stop() on the platform this event
                    // will be signalled, which will be needed for Updating the the Platform itself
                    this.eventPlatformStopped = new AutoResetEvent(false);
                    this.eventPlatformStopped.WaitOne();
                }
        
            }
        }

        public void ConfiguredStart()
        {

            guiService.ConfiguredStart();

            //initialize the scout helper; needed for scouts that rely on upnp discovery
            ScoutHelper.Init(logger);

            //start the configured scouts. starting scouts before modules.
            foreach (var sInfo in config.GetAllScouts())
            {
                StartScout(sInfo);
            }

            //start the heartbeat service (directly writes to cloud)
            if (Settings.HeartbeatServiceMode != "Off")
            {
                InitHeartbeatService();
            }

            //config updater
            if(this.configLookup==null)
            {
                ConfigUpdater configLookup = null;
                LoadConfig loadNewConfig = this.LoadConfigFromDir;

                configLookup = new ConfigUpdater(null, logger, Settings.ConfigLookupFrequency, loadNewConfig, this);
                this.configLookup = configLookup;
                if (this.configLookup != null)
                {
                    this.configLookup.setConfig(this.config);
                }

            }

            // start the authentication service
            {
                authenticationService = new HomeOS.Hub.Platform.Authentication.AuthenticationService(logger, this);
                authenticationServiceHost = HomeOS.Hub.Platform.Authentication.AuthenticationService.CreateServiceHost(logger, this, authenticationService);
                authenticationServiceHost.Open();
            }

            InitHomeService();

            if (Settings.RunningMode.Equals("standard"))
            {
                InitAutoStartModules();
            }
            else
            #region developers' running modes
            {
                if (Settings.RunningMode.Equals("unittesting"))
                {

                    // don't start any modules, we just need a module-less platform to initialize the module (app/device)
                    // being unit tested
                }
                else if (Settings.RunningMode.Equals("ratul"))
                {
                    //StartModule(new ModuleInfo("axiscamdriver", "DriverAxisCamera", "DriverAxisCamera", null, false, "192.168.0.198", "root", "homeos"));
                    //StartModule(new ModuleInfo("foscamdriver1", "DriverFoscam", "HomeOS.Hub.Drivers.Foscam", null, false, "192.168.1.125", "admin", ""));
                    
                    StartModule(new ModuleInfo("webcamdriver", "DriverWebCam", "HomeOS.Hub.Drivers.WebCam", null, false, @"Microsoft® LifeCam VX-7000"));
                    StartModule(new ModuleInfo("AppCam", "AppCamera", "HomeOS.Hub.Apps.SmartCam", null, false));

                    //string para1 = "C:\\Users\\t-chuchu\\Desktop\\homeos\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera1.txt";
                    //string para2 = "C:\\Users\\t-chuchu\\Desktop\\homeos\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera2.txt";
                    //StartModule(new ModuleInfo("trackingapp", "AppTracking", "AppTracking", null, false, para1, para2));                   

                    //StartModule(new ModuleInfo("HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.WindowCamera for HomeOSGadgeteerDevice_WindowCamera_MicrosoftResearch_65355695098562951548", "DriverGadgetCamera", "HomeOS.Hub.Drivers.Gadgeteer.MicrosoftResearch.WindowCamera", null, false, "192.168.0.197"));

                    //StartModule(new ModuleInfo("zwavezensys", "DriverZwaveZensys", "HomeOS.Hub.Drivers.ZwaveZensys_4_55", null, false));
                    //StartModule(new ModuleInfo("switchapp", "AppSwitch", "HomeOS.Hub.Apps.Switch", null, false));

                    //StartModule(new ModuleInfo("alerts", "AppAlerts", "HomeOS.Hub.Apps.Alerts", null, false));

                    //StartModule(new ModuleInfo("foscamdriver1", "DriverFoscam", "HomeOS.Hub.Drivers.Foscam", null, false, "192.168.1.125", "admin", ""));

                    //StartModule(new ModuleInfo("AppDummy1", "AppDummy1", "HomeOS.Hub.Apps.Dummy", null, false, null));
                    //StartModule(new ModuleInfo("DriverDummy1", "DriverDummy1", "HomeOS.Hub.Drivers.Dummy", null, false, null));
                }
                else if (Settings.RunningMode.Equals("rayman"))
                {
                    ModuleInfo d = new ModuleInfo("HomeOS.Hub.Drivers.Mic", "HomeOS.Hub.Drivers.Mic", "HomeOS.Hub.Drivers.Mic", null, false,"foo", "8000", "1" );
                    StartModule(d);

                    /*
                    HomeOS.Hub.Platform.Authentication.AuthenticationService auth = new HomeOS.Hub.Platform.Authentication.AuthenticationService(logger, this);
                    System.ServiceModel.ServiceHost s = HomeOS.Hub.Platform.Authentication.AuthenticationService.CreateServiceHost(logger, this, auth);
                    s.Open();


                    ModuleInfo app = new ModuleInfo("AppDummy1", "AppDummy1", "HomeOS.Hub.Apps.Dummy", null, false, null);
                    StartModule(app);
                    ModuleInfo app1 = new ModuleInfo("DriverDummy1", "DriverDummy1", "HomeOS.Hub.Drivers.Dummy", null, false, null);
                    StartModule(app1);
                    
                    HomeOS.Hub.Common.TokenHandler.SafeTokenHandler t = new Common.TokenHandler.SafeTokenHandler("randomsalt");
                    string s1 = t.GenerateToken("helloworlergwergwergwergwergwegrwegewgewrgwergwregwgwgd"); 
                    logger.Log("Encryting helloworld: "+s1);
                    t = null;
                    t = new Common.TokenHandler.SafeTokenHandler("randomsalt");
                    logger.Log("decrypting token: " + t.ProcessToken(s1).Name);*/


                    //    DateTime t = policyEngine.AllowAccess("*","AppDummy1", "jeff");
                    //    logger.Log(">>>>>>>>>>> " + t.ToString()+ "  , " + DateTime.Now);


                    // Dont touch my running mode
                    /*
                     AddInToken t =  null ; 
                         foreach (AddInToken token in allAddinTokens)
                         {
                             if (token.Name.Equals("HomeOS.Hub.Drivers.Dummy") )
                             {
                                 t = token ; 
                             }
                         }
                         VModule a = t.Activate<VModule>(AddInSecurityLevel.FullTrust);
                      ModuleInfo info = new ModuleInfo("friendlyName", "moduleName", "moduleName", null, false, null);
                         AddInController aic = AddInController.GetAddInController(a);
                         a.Initialize(this, logger,info, 0);
                         SafeThread moduleThread = new SafeThread(delegate() { a.Start(); },"", logger);
                         moduleThread.Start();
                         System.Threading.Thread.Sleep(1 * 11 * 1000);
                         aic.Shutdown();
                    

                     ModuleInfo[] app = new ModuleInfo[100];
                     int i;
                     for (i = 0; i < 30; i++)
                     {
                         app[i] = new ModuleInfo("AppDummy"+i.ToString(), "AppDummy"+i.ToString(), "HomeOS.Hub.Apps.Dummy", null, false, null);
                         StartModule(app[i]);
                     }
            

                 

                     ModuleInfo[] driver = new ModuleInfo[3000]; 
                     int j;
                     for (j = 0; j <30; j++)
                     {
                         driver[j] = new ModuleInfo("DriverDummy"+j.ToString(), "DriverDummy"+j.ToString(), "HomeOS.Hub.Drivers.Dummy", null, false, null);
                         StartModule(driver[j]);
                     }
            
                     System.Threading.Thread.Sleep(1 * 20 * 1000);
                    
                     
                     for (j = 29; j >=0; j--)
                     {
                         StopModule(runningModules.First(x => x.Value.Equals(driver[j])).Key.Secret());

                     }
                     for (i = 29;i >= 1; i--)
                     {
                         StopModule(runningModules.First(x => x.Value.Equals(app[j])).Key.Secret());

                     }
          */


                }
                else if (Settings.RunningMode.Equals("chunte"))
                {
                    //StartModule(new ModuleInfo("webcamdriver", "DriverWebCam", "DriverWebCam", null, false, "logitech"));
                    //StartModule(new ModuleInfo("foscamdriver", "DriverFoscam", "DriverFoscam", null, false, "192.168.0.196", "admin", "whoareyou?"));
                    //StartModule(new ModuleInfo("foscamdriver", "DriverFoscam", "DriverFoscam", null, false, "172.31.42.177", "admin", ""));
                    string video1 = "c:\\img\\cam2_20120821165455_test1.avi";
                    string video2 = "c:\\img\\DSCN7066_test1.avi";
                    StartModule(new ModuleInfo("loadvideo1", "DriverVideoLoading", "DriverVideoLoading", null, false, video1));
                    StartModule(new ModuleInfo("loadvideo2", "DriverVideoLoading", "DriverVideoLoading", null, false, video2));
                    //StartModule(new ModuleInfo("cameraapp", "AppCamera", "AppCamera", null, false));

                    string para1 = "C:\\Users\\t-chuchu\\Desktop\\homeos\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera1.txt";
                    string para2 = "C:\\Users\\t-chuchu\\Desktop\\homeos\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera2.txt";
                    StartModule(new ModuleInfo("trackingapp", "AppTracking", "AppTracking", null, false, para1, para2));
                }
                else if (Settings.RunningMode.Contains("khurshed"))
                {
                    if (Settings.RunningMode.Equals("khurshed_test_smartcam_foscam"))
                    {
                        StartModule(new ModuleInfo("foscamdriver2", "DriverFoscam", "HomeOS.Hub.Drivers.Foscam", null, false, "157.54.148.65", "admin", ""));
                        StartModule(new ModuleInfo("SmartCamApp", "AppSmartCam", "HomeOS.Hub.Apps.SmartCam", null, false));
                    }
                    else if (Settings.RunningMode.Equals("khurshed_test_smartcam_foscam_notifications"))
                    {
                        StartModule(new ModuleInfo("foscamdriver2", "DriverFoscam", "DriverFoscam", null, false, "157.54.148.65", "admin", ""));
                        StartModule(new ModuleInfo("SmartCamApp", "AppSmartCam", "AppSmartCam", null, false));
                    }
                    else if (Settings.RunningMode.Equals("khurshed_test_smartcam_webcam"))
                    {
                        StartModule(new ModuleInfo("webcamdriver", "DriverWebCam", "DriverWebCam", null, false, "Logitech QuickCam Pro 9000"));
                        StartModule(new ModuleInfo("SmartCamApp", "AppSmartCam", "AppSmartCam", null, false));
                    }
                    else if (Settings.RunningMode.Equals("khurshed_test_smartcam_foscam_webcam"))
                    {
                        StartModule(new ModuleInfo("webcamdriver", "DriverWebCam", "DriverWebCam", null, false, "Logitech QuickCam Pro 9000"));
                        StartModule(new ModuleInfo("foscamdriver2", "DriverFoscam", "DriverFoscam", null, false, "157.54.148.65", "admin", ""));
                        StartModule(new ModuleInfo("SmartCamApp", "AppSmartCam", "AppSmartCam", null, false));
                    }
                    else if (Settings.RunningMode.Equals("khurshed_test_tracking_foscam"))
                    {
                        string para1 = "C:\\homeos2\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera1.txt";
                        string para2 = "C:\\homeos2\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera2.txt";
                        StartModule(new ModuleInfo("foscamdriver2", "DriverFoscam", "DriverFoscam", null, false, "157.54.148.65", "admin", ""));
                        StartModule(new ModuleInfo("trackingapp", "AppTracking", "AppTracking", null, false, para1, para2));
                    }
                    else if (Settings.RunningMode.Equals("khurshed_test_tracking_videoloading"))
                    {
                        string video1 = "c:\\img\\cam2_20120821165455_test1.avi";
                        string video2 = "c:\\img\\DSCN7066_test1.avi";
                        StartModule(new ModuleInfo("loadvideo1", "DriverVideoLoading", "DriverVideoLoading", null, false, video1));
                        StartModule(new ModuleInfo("loadvideo2", "DriverVideoLoading", "DriverVideoLoading", null, false, video2));
                        string para1 = "C:\\homeos2\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera1.txt";
                        string para2 = "C:\\homeos2\\homeos\\Apps\\AppTracking\\VideoTracking\\para_camera2.txt";
                        StartModule(new ModuleInfo("trackingapp", "AppTracking", "AppTracking", null, false, para1, para2));
                    }
                }
                else if (Settings.RunningMode.Equals("jamie"))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(new XmlTextReader(@"C:\homeos\LightSettings.xml"));

                    XmlNode xmlData = xmlDoc.SelectSingleNode("data");
                    string strLightsIP = xmlData.SelectSingleNode("lightsIP").InnerText;
                    string strLightsUser = xmlData.SelectSingleNode("lightsUser").InnerText;
                    string strfoscamIP = xmlData.SelectSingleNode("foscamIP").InnerText;
                    string strfoscamUser = xmlData.SelectSingleNode("foscamUser").InnerText;
                    string strLightCount = xmlData.SelectSingleNode("light_count").InnerText;

                    StartModule(new ModuleInfo("foscamdriver1", "DriverFoscam", "HomeOS.Hub.Drivers.Foscam", null, false, strfoscamIP, strfoscamUser, ""));
                    StartModule(new ModuleInfo("huedriver1", "HueBridge", "HomeOS.Hub.Drivers.HueBridge", null, false, strLightsIP, strLightsUser, strLightCount));
                    StartModule(new ModuleInfo("LightsHome1", "LightsHome", "HomeOS.Hub.Apps.LightsHome", null, false));
                }
                else if (Settings.RunningMode.Equals("sarah"))
                {
                    //Sarah to fill in the right device id
                    StartModule(new ModuleInfo("couchdriver", "DriverCouch", "HomeOS.Hub.Drivers.GadgetCouch", null, false, ".."));
                    StartModule(new ModuleInfo("couchapp", "EmotoCouch", "HomeOS.Hub.Apps.EmotoCouch", null, false));
                }
                else if (Settings.RunningMode.Equals("erin"))
                {
                    //app startup needed since it's not in the install repository
                    StartModule(new ModuleInfo("AppDoorjamb", "AppDoorjamb", "HomeOS.Hub.Apps.Doorjamb", null, false));
                }
                else
                {
                    throw new Exception("Unknown running mode: " + Settings.RunningMode);
                }

                //we ran using a non-standard running mode
                //make sure that the modules we ran are entered in the config, so that we can keep it consistent
                //otherwise, a service (port) will get added without its exporting module
                lock (this)
                {
                    foreach (ModuleInfo moduleInfo in runningModules.Values)
                    {
                        if (moduleInfo.GetManifest() == null)
                            moduleInfo.SetManifest(new Manifest());

                        config.AddModuleIfMissing(moduleInfo);
                    }
                }

            }
            #endregion


            if (String.IsNullOrEmpty(Settings.WifiSsid))
                logger.Log("Warning: WiFi credentials are not configured");

            if (!Settings.StayOffline)
            {
                //start checking for the uniqueness of home id on a separate thread
                SafeThread uniqueHomeIdCheck = new SafeThread(delegate()
                {
                    heartbeatService.CanIClaimHomeId(Utils.HardwareId, Settings.HomeId, UniqueHomeIdCheckCompleted);
                }, "UniqueHomeIdCheck", logger);

                uniqueHomeIdCheck.Start();
            }
        }

        /// <summary>
        /// Starts the heartbeat service
        /// </summary>
        private void InitHeartbeatService()
        {
            if (string.IsNullOrWhiteSpace(Settings.HomeId))
            {
                logger.Log("Cannot start home service without a configured home id. What is going on?");
                return;
            }

            heartbeatService = new HeartbeatService(this, logger);
            heartbeatService.Start();
        }

        /// <summary>
        /// Starts the home service that connects us to the cloud using the gatekeeper
        /// </summary>
        private void InitHomeService()
        {
            if (string.IsNullOrWhiteSpace(Settings.HomeId))
            {
                logger.Log("Cannot start home service without a configured home id. What is going on?");
                return;
            }

            HomeOS.Shared.Gatekeeper.Settings.HomeId = Settings.HomeId;
            HomeOS.Shared.Gatekeeper.Settings.ServiceHost = Settings.GatekeeperURI;

            homeService = new Gatekeeper.HomeService(logger);
            homeService.Start(null);
        }

        private void UniqueHomeIdCheckCompleted(bool isUnique)
        {
            if (isUnique)
            {
                logger.Log("Hurray, we are unique. Going online now");

                //start log syncing
                if (Settings.AutoSyncLogs && logger.IsRotatingLog)
                {
                    string accountName = GetConfSetting("DataStoreAccountName");
                    string accountKey = GetConfSetting("DataStoreAccountKey");
                    string containerName = "log-" + Settings.HomeId.ToLower();

                    if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(accountKey))
                    {
                        logger.Log("DataStore credentials are not configured. Will NOT sync logs");
                    }
                    else
                    {
                        //todo: make sure that the container name is valid

                        logger.InitSyncing(accountName, accountKey, containerName);
                    }
                }

                //tell all the modules that we've gone online
                lock (runningModules)
                {
                    foreach (var module in runningModules.Keys)
                    {
                        module.OnlineStatusChanged(isUnique);
                    }
                }

            }
            else
            {
                string message = String.Format("ERROR: HomeId {0} is not unique. Will not go online\n", Settings.HomeId);
                logger.Log(message);
                Console.Error.WriteLine(message);
            }
        }

        
        /// <summary>
        /// Get the list of all modules that are currently installed
        /// </summary>
        /// <returns> A list of tokens, one per module</returns>
        private static Collection<AddInToken> GetModuleList(string addInRoot) {

            // rebuild the cache files of the pipeline segments and add-ins.
            string[] warnings = AddInStore.Rebuild(addInRoot);

            foreach (string warning in warnings)
                Console.WriteLine(warning);

            // Search for add-ins of type VModule
            Collection<AddInToken> tokens = AddInStore.FindAddIns(typeof(VModule), addInRoot);

            foreach (AddInToken token in tokens)
                Console.WriteLine("Found module {0}", token.Name);

            return tokens;
        }

        /// <summary>
        /// Issues a capability
        /// </summary>
        /// <param name="module">The module that is asking for the capability</param>
        /// <param name="targetPort">The port for which the capability is being requested</param>
        /// <param name="userName">The name of the user on behalf of which the capability is being requested</param>
        /// <param name="password">The password of the user</param>
        /// <returns>The issued capability</returns>
        public VCapability GetCapability(VModule requestingModule, VPort targetPort, string username, string password)
        {
            // ....
            //check if the user is valid, the module exists, and the port is registered
            // ....
            if (!username.Equals(Constants.UserSystem.Name) && 
                config.ValidUserUntil(username, password) < DateTime.Now)
                return null;

            if (!runningModules.ContainsKey(requestingModule))
                return null;

            //if (!config.allPorts.ContainsKey(targetPort.GetInfo()))
            //    return null;

            if (!registeredPorts.ContainsKey(targetPort))
                return null;

            string portName = targetPort.GetInfo().GetFriendlyName();

            DateTime allowedUntil = (Settings.EnforcePolicies) ? 
                                        policyEngine.AllowAccess(portName, requestingModule.GetInfo().FriendlyName(), username) : 
                                        DateTime.MaxValue;

            if (allowedUntil < DateTime.Now)
            {
                return null;
            }

            Capability capability = new Capability("platform", allowedUntil);

            if (RemoteInstallCapability(capability, targetPort))
            {
                return capability;
            }
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// Install the capability on the port
        /// </summary>
        /// <param name="capability">The capability to install</param>
        /// <param name="targetPort">The port on which to install</param>
        /// <returns>If the install succeeded</returns>
        private bool RemoteInstallCapability(Capability capability, VPort targetPort)
        {
            VModule targetModule = null;

            lock (this)
            {
                if (registeredPorts.ContainsKey(targetPort))
                {
                    targetModule = registeredPorts[targetPort];
                }
            }

            if (targetModule == null)
            {
                logger.Log("Platform could not find the target module for port {0}", targetPort.ToString());
                return false;
            }

            ResultCode result = (ResultCode) targetModule.InstallCapability(capability, targetPort);

            if (result == ResultCode.Success) {
                return true;
            }
            else
            {
                logger.Log("RemoteCapabilityInstall failed: {0}", result.ToString());
                return false;
            }
        }

        public bool IsValidUser(string username, string password)
        {
            return (config.ValidUserUntil(username, password) > DateTime.Now);
        }

        public string GetLiveIdUserName(string LiveIdUniqueUserToken)
        {
            List<UserInfo> users = config.GetAllUsers();
            foreach (UserInfo user in users)
            {
                if (LiveIdUniqueUserToken.Equals(user.LiveIdUniqueUserToken, StringComparison.CurrentCultureIgnoreCase))
                    return user.Name;
            }
            return "";
        }

        private bool IsValidUserByLiveId(string LiveIdUniqueUserToken)
        {
            return (config.ValidLiveIdUntil(LiveIdUniqueUserToken) > DateTime.Now);
        }

        private bool IsValidAccessByUserName(string requestingModule, string username)
        {
            return (ValidAccess(requestingModule, username) > DateTime.Now);
        }
        
        private DateTime ValidAccess(string requestingModule, string username)
        {            
            return policyEngine.AllowAccess("*",requestingModule, username);
        }

        public int IsValidAccess(string accessedModule, string domainOfAccess, string privilegeLevel, string userIdentifier)
        {
            if (!Settings.EnforcePolicies)
                return (int) ResultCode.Allow;

            List<string> allowedPrivilegeLevels = GetPrivilegeLevels(accessedModule, domainOfAccess);
            
            if (!allowedPrivilegeLevels.Contains(privilegeLevel)) // if privilege level is insufficient for this access
                return (int)ResultCode.InSufficientPrivilege; 

            bool auth = false ; 

            if (privilegeLevel.Equals(Constants.LiveId))// need to check again because need to match UniqueLiveIdUserTokens for LiveID authentication
            {
                if(!IsValidUserByLiveId(userIdentifier))
                    return (int)ResultCode.InvalidUser;

                 foreach (UserInfo user in config.GetAllUsers())
                {
                    if (user.LiveIdUniqueUserToken.Equals(userIdentifier))
                    {
                        auth =  IsValidAccessByUserName(accessedModule, user.Name);
                        break;
                    }   
                }

            }
            else
            {
                auth =  IsValidAccessByUserName(accessedModule, userIdentifier);
            }

            if (!auth) // access not allowed
                return (int)ResultCode.ForbiddenAccess;

            return (int)ResultCode.Allow; 

        }

        public List<string> GetPrivilegeLevels(string moduleFriendlyName, string domainOfAccess)
        {
            if (domainOfAccess.Equals(Shared.Gatekeeper.Settings.ServiceHost+":"+Shared.Gatekeeper.Settings.ClientPort))// access is via gatekeeper
            {
                return GetPrivilegeLevelsforGateKeeperAccess(moduleFriendlyName);
            }
            else // access is not-via gatekeeper e.g. localhost or local IP
            {
                return GetPrivilegeLevelsforLocalAccess(moduleFriendlyName);
            }

        }

        private List<string> GetPrivilegeLevelsforLocalAccess(string moduleFriendlyName)
        {
            List<string> retVal = new List<string>();

            foreach (string privilegeLevel in Constants.PrivilegeLevels.Keys) 
            {
                if (IsValidAccessByUserName(moduleFriendlyName, privilegeLevel))
                {
                    retVal.Add(privilegeLevel);
                }

                else if (privilegeLevel.Equals(Constants.LiveId)) // special case for checking of LiveId access
                {// we need to check if atleast one liveid-based user has access to the module. if so, we put liveid privilege in the returned list
                    foreach (UserInfo user in config.GetAllUsers())
                    {
                        if (!string.IsNullOrEmpty(user.LiveIdUniqueUserToken) && IsValidAccessByUserName(moduleFriendlyName, user.Name))
                        {// There exists a valid LiveId-based user, who has access to the module.
                            retVal.Add(privilegeLevel);
                            break;
                        }
                    }
                }
            }
            return retVal;   
        }

        private List<string> GetPrivilegeLevelsforGateKeeperAccess(string moduleFriendlyName)
        {
            List<string> retVal = new List<string>();

            foreach (string privilegeLevel in Constants.PrivilegeLevels.Keys)
            {
                /* Disabling systemlow and system high accesses for remote access (after discussing with ratul on 6/24)
                // allow system high for remote access, for now, (by AJ's request)
                if (IsValidAccessByUserName(moduleFriendlyName, privilegeLevel) && privilegeLevel.Equals(Constants.SystemHigh)) 
                {
                    retVal.Add(privilegeLevel);
                }
                
                else*/ if (privilegeLevel.Equals(Constants.LiveId)) // special case for checking of LiveId access
                {// we need to check if atleast one liveid-based user has access to the module. if so, we put liveid privilege in the returned list
                    foreach (UserInfo user in config.GetAllUsers())
                    {
                        if (!string.IsNullOrEmpty(user.LiveIdUniqueUserToken) && IsValidAccessByUserName(moduleFriendlyName, user.Name))
                        {// There exists a valid LiveId-based user, who has access to the module.
                            retVal.Add(privilegeLevel);
                            break;
                        }
                    }
                }
            }
            return retVal;
        }


        public string GetConfSetting(string paramName)
        {
            return config.GetConfSetting(paramName);
        }

        public string GetPrivateConfSetting(string paramName)
        {
            return config.GetPrivateConfSetting(paramName);
        }

        public string GetDeviceIpAddress(string deviceId)
        {
            return config.GetDeviceIpAddress(deviceId);
        }

        /// <summary>
        /// Returns all ports that are currently registered
        /// </summary>
        /// <returns>the list of ports</returns>
        public IList<VPort> GetAllPorts()
        {
            IList<VPort> portList = new List<VPort>();

            lock (this)
            {
                foreach (VPort port in registeredPorts.Keys)
                {
                    portList.Add(port);
                }
            }

            return portList;
        }

        /// <summary>
        /// Starts all modules that are marked as auto start in the configuration
        /// </summary>
        private void InitAutoStartModules()
        {

            //*** Checking if modules to be auto-started, as per config, are present in the rep 
            // if not download and rebuild tokens
           /* Dictionary<string, string> tokenNameVersion = new Dictionary<string, string>();
            bool rebuild=false; 
            foreach (AddInToken token in allAddinTokens)
            {
                tokenNameVersion.Add(token.Name , token.Version);
            }

            foreach (ModuleInfo moduleInfo in config.allModules.Values)
            {
                KeyValuePair<string,string> moduleNameVersion = new KeyValuePair<string,string>(moduleInfo.BinaryName(),moduleInfo.GetVersion() );
                if (moduleInfo != null && moduleInfo.AutoStart && !tokenNameVersion.Contains(moduleNameVersion) ) 
                { // if module is to be auto-started and is not present in tokens
                    if (GetAddInFromRep(moduleInfo, false))
                        rebuild = true;
                }
            }
            if(rebuild)
                rebuildAddInTokens(); */
            

            foreach (ModuleInfo moduleInfo in config.allModules.Values)
            {
                if (moduleInfo != null && moduleInfo.AutoStart)
                {
                    StartModule(moduleInfo, true);
                }
            }
        }

        /// <summary>
        /// A handler for all uncaught exceptions. We log, but we'll still die
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            logger.Log("Got unhandled exception from {0}: {1}\nException: {2}", sender.ToString(), args.ToString(), e.ToString());
        }

        /// <summary>
        /// Starts a module by searching for a matching token
        /// </summary>
        /// <param name="moduleInfo">The ModuleInfo for the module to start</param>
        public VModule StartModule(ModuleInfo moduleInfo, bool exactlyMatchVersions = false)
        {
            VModule startedModule = null;

            foreach (AddInToken token in allAddinTokens)
            {
                if (token.Name.Equals(moduleInfo.BinaryName()) &&
                    (!exactlyMatchVersions || CompareModuleVersions(moduleInfo.GetDesiredVersion(), Utils.GetHomeOSUpdateVersion(Utils.GetAddInConfigFilepath(moduleInfo.BinaryName()), logger))))
                {
                    if (startedModule != null)
                    {
                        logger.Log("WARNING: Found multiple matching tokens for " + moduleInfo.ToString());
                        continue;
                    }

                    try
                    {
                        startedModule = StartModule(moduleInfo, token);
                    }
                    catch (Exception exception)
                    {
                        logger.Log("Could not start module {0}: {1}", moduleInfo.ToString(), exception.ToString());
                        return null;
                    }
                }
            }

            //we ran something, lets return it
            if (startedModule != null)
                return startedModule;

            //we didn't run anything.
            //if we were doing exact match on versions, this could be because we didn't find an exact match
            if (exactlyMatchVersions)
            {
                logger.Log("No exact-match-version token found for Module: Binary name: " + moduleInfo.BinaryName() + ", App Name: " + moduleInfo.AppName() + ", Version: " + moduleInfo.GetDesiredVersion());

              
                Version versionRep = new Version(GetVersionFromRep(Settings.RepositoryURIs, moduleInfo.BinaryName()));
                Version versionLocal = new Version(Utils.GetHomeOSUpdateVersion((Utils.GetAddInConfigFilepath(moduleInfo.BinaryName())), logger));

                logger.Log("The latest version for {0} on the repository: {1}", moduleInfo.BinaryName(), versionRep.ToString());
                logger.Log("The version for {0} in the local AddIn dir: {1}", moduleInfo.BinaryName(), versionLocal.ToString()); 

                if (versionRep.CompareTo(versionLocal) > 0)
                {
                    logger.Log("The latest version on the repository ({0}) > local version ({1}) in AddIn for {2} - the latest from the rep will be downloaded!", versionRep.ToString(), versionLocal.ToString(), moduleInfo.BinaryName());

                    //try to get an exact match from the homestore
                    GetAddInFromRep(moduleInfo);

                }
                //maybe, we got the right version, maybe we didn't; in any case, lets now run what we can find, without being strict about version numbers
                return StartModule(moduleInfo, false);
            }
            else
            {
                logger.Log("No matching token at all found for Module: Binary name: " + moduleInfo.BinaryName() + ", App Name: " + moduleInfo.AppName() + ", Version: " + moduleInfo.GetDesiredVersion());
                return null;
            }
        }

        public string GetVersionFromRep(string uriRoot, string binaryName)
        {
            string ret = "0.0.0.0";
            string zipUrl = uriRoot;

            string[] path = binaryName.Split('.');

            foreach (string pathElement in path)
                zipUrl += "/" + pathElement;
            
            zipUrl += "/Latest/" + binaryName+ ".dll.config";

            //string tempPath = Environment.CurrentDirectory + "\\temp_checkversion\\";

            //tempPath += binaryName + "\\";

            ////create a temp directory to store the zip file and its contents from the Latest dir on the rep  
            //if (!System.IO.Directory.Exists(tempPath))
            //    System.IO.Directory.CreateDirectory(tempPath);

            ////download the latest config file to the temp dir.
            //DownloadFile(zipUrl, tempPath, binaryName + ".dll.config");

            ////look for the HomeOSUpdateVersion from the config file 
            //ret = Utils.GetHomeOSUpdateVersion(tempPath + binaryName + ".dll.config", logger);

            ret = Utils.GetHomeOSUpdateVersion(zipUrl, logger);

            ////delete the temp dir
            //System.IO.DirectoryInfo dir = new DirectoryInfo(tempPath);   
            //if (dir.Exists)  
            //dir.Delete(true);

            return ret;
        }




        /// <summary>
        /// Starts a module given its token
        ///    We don't do this anymore: Has the side effect of updating the moduleInfo object's version to what was exactly ran
        /// </summary>
        /// <param name="moduleInfo"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private VModule StartModule(ModuleInfo moduleInfo, AddInToken token)
        {
            VModule startedModule = null;

            string moduleVersion = Utils.GetHomeOSUpdateVersion(Utils.GetAddInConfigFilepath(moduleInfo.BinaryName()), logger);
            if (!CompareModuleVersions(moduleInfo.GetDesiredVersion(), moduleVersion))
            {
                logger.Log("WARNING: Starting an inexact match for {0}", moduleInfo.FriendlyName());                
            }

            moduleInfo.SetRunningVersion(moduleVersion);

            switch (Constants.ModuleIsolationLevel)
            {
                case ModuleIsolationModes.AppDomain:
                    //     AppDomain addInAppDomain = AppDomain.CreateDomain(moduleInfo.BinaryName());
                    //    startedModule = token.Activate<VModule>(addInAppDomain);

                    // Adding upfront, to check if the module is already running.
                    // So that token is not re-activated if not needed - rayman
                    if (runningModules.Values.Contains(moduleInfo))
                        throw new Exception(string.Format("Attempted to run duplicate module. New = {0}. Old = {1}", moduleInfo, moduleInfo));

                    startedModule = token.Activate<VModule>(AddInSecurityLevel.FullTrust);

                    //AddInController ainController = AddInController.GetAddInController(startedModule);
                    //AppDomain domain = ainController.AppDomain;

                    break;
                case ModuleIsolationModes.Process:
                    AddInProcess addInProc = new AddInProcess();
                    startedModule = token.Activate<VModule>(addInProc, AddInSecurityLevel.FullTrust);
                    break;
                case ModuleIsolationModes.NoAddInAppDomain:
                    //this requires putting Views.dll in the directory with AppBenchmarker.dll
                    //the simplest way to do that is to just add AppBenchmarker as a reference
                    //AppDomainSetup ads = new AppDomainSetup();
                    //ads.ApplicationBase = Environment.CurrentDirectory;//AddInRoot + "\\AddIns\\" + "AppBenchmarker";
                    var ad = AppDomain.CreateDomain(moduleInfo.FriendlyName());//, null, ads);
                    startedModule = (VModule)ad.CreateInstanceAndUnwrap("AppBenchmarker", "AppBenchmarker.Benchmarker");
                    break;
                case ModuleIsolationModes.None:
                    //this requires adding AppBenchmarker and ModuleBase projects as References
                    //startedModule = (VModule)new AppBenchmarker.Benchmarker();
                    //if (moduleInfo.BinaryName().Equals("AppCamera"))
                    //    startedModule = (VModule)new AppCamera.CameraController();
                    //else if (moduleInfo.BinaryName().Equals("DriverFoscam"))
                    //    startedModule = (VModule)new DriverFoscam.DriverFoscam();
                    //else
                    //    logger.Log("Unknown module: {0}", moduleInfo.BinaryName());
                    break;
            }

            if (moduleInfo.WorkingDir() == null)
                moduleInfo.SetWorkingDir(Settings.ModuleWorkingDirBase + "\\" + moduleInfo.FriendlyName());

            if (String.IsNullOrEmpty(moduleInfo.BaseURL()))
                moduleInfo.SetBaseURL(GetBaseUrl() + "/" + moduleInfo.FriendlyName());

            if (string.IsNullOrEmpty(moduleInfo.BinaryDir()))
                moduleInfo.SetBinaryDir(Constants.AddInRoot + "\\AddIns\\" + moduleInfo.BinaryName());

            int secret = GetNewSecret();
            startedModule.Initialize(this, logger, moduleInfo, secret);

            lock (this)
            {
                //check if we already have this module running
                foreach (ModuleInfo runningModuleInfo in runningModules.Values)
                {
                    if (runningModuleInfo.Equals(moduleInfo))
                    {
                        //moduleThread.Abort();
                        throw new Exception(string.Format("Attempted to run duplicate module. New = {0}. Old = {1}", moduleInfo, moduleInfo));
                    }
                }

                runningModules[startedModule] = moduleInfo;
            }

            SafeThread moduleThread = new SafeThread(delegate() { startedModule.Start(); }, moduleInfo.FriendlyName(), logger);
            moduleThread.Start();

            return startedModule;
        }


        private int GetNewSecret()
        {
            int secret = random.Next(0, int.MaxValue);

            lock (this)
            {
                foreach (VModule module in runningModules.Keys)
                {
                    if (secret == module.Secret())
                        return GetNewSecret();
                }
            }

            return secret;
        }

        /// <summary>
        /// Stops a module
        /// </summary>
        /// <param name="moduleSecret"></param>
        private bool StopModule(int moduleSecret)
        {
            VModule moduleToStop = null;

            lock (this)
            {
                foreach (VModule module in runningModules.Keys)
                {
                    if (module.Secret() == moduleSecret)
                    {
                        moduleToStop = module;
                        break;
                    }
                }
            }
            logger.Log("Stopping Module: " + moduleToStop.GetInfo().BinaryName());

            if (moduleToStop == null)
            {
                logger.Log(this + " failed to stopmodule. secret {0} not found", moduleSecret.ToString());
                return false;
            }
            else
            {
                return StopModule(moduleToStop);
            }
        }

        public bool StopModule(VModule moduleToStop)
        {
            //***
            try
            {

                SafeThread t = new SafeThread(delegate() { moduleToStop.Stop(); }, moduleToStop.GetInfo().AppName() + "stop thread", logger); // invoke stop on the module
                t.Start();
                t.Join(TimeSpan.FromMilliseconds(Settings.MaxStopExecutionTime));
                if (t.IsAlive())
                    t.Abort();

                ModuleFinished(moduleToStop); // dereg its ports broadcast ports' dereg; wipe module off data structures
                AddInCleanup(moduleToStop); // addin cleanup

                return true;
            }
            catch (Exception e)
            {
                logger.Log("Exception in stopping of module: " + e);

                return false;
            }

            //***
        }

        public bool StopModule(string moduleFriendlyName)
        {
            VModule moduleToStop = null;
            lock (this)
            {
                foreach (VModule module in runningModules.Keys)
                {
                    if (moduleFriendlyName.Equals(module.GetInfo().FriendlyName()))
                        moduleToStop  = module;
                }
            }

            if (moduleToStop != null)
            {
                return StopModule(moduleToStop);
            }
            else
            {
                logger.Log("Did not find a module with friendlyName {0} to stop", moduleFriendlyName);
                return false;
            }
        }

        // Ratul
        public void UninstallModule(ModuleInfo moduleInfo) {
            // TODO:
        }

        /// <summary>
        /// Issues a portinfo object
        /// </summary>
        /// <param name="moduleFacingName">The local name used by the owning module for this port</param>
        /// <param name="module">The owning module</param>
        /// <returns></returns>
        public VPortInfo GetPortInfo(string moduleFacingName, VModule module)
        {
            PortInfo targetPortInfo = new PortInfo(moduleFacingName, module.GetInfo());

            //if matching portInfo exists, return that object
            //NB: we cannot return targetPortInfo itself because that is a different object (which does not have location and other things set)
            VPortInfo matchingPortInfo = config.GetMatchingPortInfo(targetPortInfo);

            if (matchingPortInfo != null)
                return matchingPortInfo;

            //this is not a port that we've seen before
            //make up a friendly name for this port as well as a location
            targetPortInfo.SetFriendlyName(moduleFacingName + " - " + module.GetInfo().FriendlyName());
            targetPortInfo.SetLocation(config.RootLocation);

            config.AddUnconfiguredPort(targetPortInfo);
            return targetPortInfo;
        }

        /// <summary>
        /// Set the roles that are being exported by a port
        /// </summary>
        /// <param name="portInfo">the portinfo object of the port</param>
        /// <param name="roles">the list of roles to bind</param>
        /// <param name="module">the module that own the port</param>
        public void SetRoles(VPortInfo vPortInfo, IList<VRole> roles, VModule module)
        {
            PortInfo storedPortInfo = config.GetMatchingPortInfo(vPortInfo);

            if (storedPortInfo == null)
                throw new Exception(vPortInfo + " not found!");
        
            IList<VRole> oldList = storedPortInfo.GetRoles();

            bool roleListChanged = (roles.Count != oldList.Count ||
                                    roles.Count != roles.Intersect(oldList).Count());
            
            storedPortInfo.SetRoles(roles);

            //update the configuration
            if (roleListChanged)
                config.UpdateRoleList(storedPortInfo);
        }

        /// <summary>
        /// Function call to register a port. Called by modules to activate new ports or to register changes in status.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public int RegisterPort(VPort port, VModule module)
        {
            ResultCode result;

            lock (this)
            {
                if (registeredPorts.ContainsKey(port))
                {
                    logger.Log(this + " got registration for an existing port: {0}", port.ToString());
                    result = ResultCode.Failure;
                }
                else
                {
                    if (runningModules.ContainsKey(module))
                    {
                        registeredPorts[port] = module;
                        logger.Log(this + " added {0} from {1}", port.ToString(), module.ToString());
                        result = ResultCode.Success;
                    }
                    else
                    {
                        logger.Log(this + " got port {0} registeration request from non-existent module {1}", port.ToString(), module.ToString());
                        result = ResultCode.ModuleNotFound;
                    }
                }
            }

            if (result == ResultCode.Success)
            {
                SafeThread newThread = new SafeThread(delegate() {
         //           System.Threading.Thread.Sleep(Settings.portRegisterDelay);
                    BroadcastPortRegistration(port, module);
                }, "RegisterPort " + port , logger);
                newThread.Start();
            }

            return (int) result;
        }

        /// <summary>
        /// Send a port registration message to all active modules
        /// </summary>
        /// <param name="port"></param>
        /// <param name="owner"></param>
        public void BroadcastPortRegistration(VPort port, VModule owner)
        {
            lock (this)
            {
                foreach (VModule module in runningModules.Keys)
                {
                    if (!module.Equals(owner))
                    {
                        VModule tmp = module; //make a copy to avoid delegate weirdnesses

                        //spawn this in a separate thread to avoid locking
                        SafeThread mPortRegThread = new SafeThread(delegate() {
                            bool done = false;
                            while (!done)
                            {
                                //*** Check if the module is ready to receive a new port (e.g., it has finished its start() ) 
                                if (this.CanEnterPortRegistered(module))
                                {
                                //***
                                try
                                {
                                    tmp.PortRegistered(port);
                                    done = true;
                                }
                                    catch (NullReferenceException e)
                                {
                                        logger.Log("Exception " + e);
                                        //Ratul TODO: not quite sure why this gets thrown, but for now this works
                                    System.Threading.Thread.Sleep(1000);
                                }
                            }
                            }
                        } ,tmp + ".PortRegistered(" + port + ")" , logger );
                        
                        mPortRegThread.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Deregister a port to declare it unavailable for use by other modules
        /// </summary>
        /// <param name="port">The port to deregisted</param>
        /// <param name="owner">The module to which the port belongs</param>
        /// <returns></returns>
        public int DeregisterPort(VPort port, VModule module)
        {
            ResultCode result;

            lock (this)
            {
                if (registeredPorts.ContainsKey(port))
                {
                    if (module.Equals(registeredPorts[port]))
                    {
                        logger.Log("deregistering port: {0}", port.ToString());
                        registeredPorts.Remove(port);

                        result = ResultCode.Success;
                    }
                    else
                    {
                        logger.Log("got port deregisteration for {0} from a non-owner {1}", port.ToString(), module.ToString());

                        result = ResultCode.Failure;
                    }
                }
                else
                {
                    logger.Log("got deregisteration for unregistered port: {0}", port.ToString());

                    result = ResultCode.PortNotFound;
                }
            }

            if (result == ResultCode.Success)
            {
                
                BroadcastPortDeregistration(port, module); 
                // broadcast needs to be asyncronous because the module's addin (and appdomain, ports, etc) will be wiped
                /*
                System.Threading.Thread newThread = new System.Threading.Thread(delegate() { 
                 //   System.Threading.Thread.Sleep(Settings.portRegisterDelay); 
                    BroadcastPortDeregistration(port, module); });
                newThread.Name = "Deregister Port " + port;
                newThread.Start(); */
            }

            return (int)result;
        }
       
        /// <summary>
        /// Send a port deregisterd message to all active modules
        /// </summary>
        /// <param name="port"></param>
        /// <param name="owner"></param>
        public void BroadcastPortDeregistration(VPort port, VModule owner)
        {
            Dictionary<String, SafeThread> listOfThreads = new Dictionary<string, SafeThread>() ;
            lock (this)
            {
                foreach (VModule module in runningModules.Keys)
                {
                    if (!module.Equals(owner))
                    {
                     

                        //***< Thread that monitors and timesout the port deregistered thread
                       SafeThread mPortDeregThreadControl = new SafeThread(delegate()
                            {
                                //***< Thread that invokes port deregistered on the module 
                                SafeThread mPortDeregThread = new SafeThread(delegate()
                                {
                                    InvokePortDeregistered(module, port);
                                }, module + ".PortDeregistered(" + port + ")", logger);

                                //***>
                                mPortDeregThread.Start();
                                mPortDeregThread.Join(TimeSpan.FromMilliseconds(Settings.MaxPortDeregisteredExecutionTime));
                                try
                                {
                                    if (mPortDeregThread.IsAlive())
                                       mPortDeregThread.Abort();
                                }
                                catch (Exception)
                                {
                                    //The PortDeregistered() calls when aborted (if so) will raise an exception
                                }
                            } , module + ".PortDeregistered(" + port + ") - Control" , logger);
                        
                        //***

                        listOfThreads[mPortDeregThreadControl.Name()] = mPortDeregThreadControl; //store the list because we want to wait on these later
                        mPortDeregThreadControl.Start();
                            }
                    }
                }
           
                foreach (SafeThread t in listOfThreads.Values)
                {
                    t.Join();
            }
 
        }

        private bool ModuleHasRegisteredPorts(VModule module)
        {
            lock (this)
            {
                foreach (VModule moduleWithActivePorts in registeredPorts.Values)
                {
                    if (module.Equals(moduleWithActivePorts))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Signals to the platform that a particular module is terminating
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public int ModuleFinished(VModule module)
        {
            ResultCode result;

            bool activePorts = ModuleHasRegisteredPorts(module);

            lock (this)
            {
                if (runningModules.ContainsKey(module) && runningModulesStates.ContainsKey(module)) // take module off platform's data structures
                {
                    //***
                    runningModules.Remove(module);
                    runningModulesStates.Remove(module);
                    //***
                    result = ResultCode.Success;
                }
                else
                {
                    logger.Log(this + "got module finished for non-existent module: {0}", module.ToString());
                    result = ResultCode.Failure;
                }
            }

            if (activePorts) // if the module has active ports deregister them
            {
                logger.Log(this + " got module finished for a module with active ports: {0}", module.ToString());
                List<VPort> portsToDeregister = new List<VPort>();
                lock (this)
                {
                    //*** Separating the parts of enumerating over registeredPorts and removing things (deregistering ports) from registeredPorts
                    foreach (VPort port in registeredPorts.Keys) // enumeration cannot continue while dictionary is being modified (e.g., dereg ports)
                    {
                        if (registeredPorts[port].Equals(module))
                        {
                            portsToDeregister.Add(port);
                        }
                    }
                }

                foreach (VPort port in portsToDeregister)
            {
                        DeregisterPort(port, module);
                }

                // we should not delete this port/service from config. we want to remember it
                //foreach (VPort port in portsToDeregister)
                //{
                //    RemovePortFromConfig(port, module);// removing ports from platform config
                //}
                    //***    
            }
            return (int)result;
        }

        public bool ClaimExclusiveAccess(VPort port)
        {
            return true;
        }

        public List<ModuleInfo> GetRunningModuleInfos()
        {
            List<ModuleInfo> retList;

            lock (this)
            {
                retList = System.Linq.Enumerable.ToList<ModuleInfo>(runningModules.Values);
            }

            return retList;
        }

        /// <summary>
        /// Reads commands from console (some functionality might be out-of-date)
        /// </summary>
        private void ReadCommandsFromConsole()
        {
            Console.Out.WriteLine("Waiting for commands");
            string[] separators = new string[2] { " ", "\n" };

            while (true)
            {
                Console.Write("> ");

                try
                {
                    string input = Console.ReadLine();
                    string[] words = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    if (words.Length == 0) continue;

                    string command = words[0].ToLower();

                    switch (command)
                    {
                        case "quit":
                        case "exit":
                            Console.WriteLine("Auf Wiedersehen!\nLive Long and Prosper. ");
                            //this.Shutdown();
                            //System.Environment.Exit(0);
                            ForceShutdown();
                            break;
                        case "restart":
                            this.Restart();
                            break;
                        case "help":
                        case "?":
                            PrintInteractiveUsage();
                            break;
                        case "starthomeservice":
                            if (null != this.homeService)
                                this.homeService.Start(null);
                            break;
                        case "stophomeservice" :
                            if (null != this.homeService)
                                this.homeService.Stop();
                            break;
                            //***
                        case "loadconfig":
                            string configdir = words[1];
                            lock (Settings.ConfigDir)
                            {
                                Settings.SetParameter("ConfigDir", configdir);
                            }
                            this.LoadConfigFromDir(Settings.ConfigDir);
                            break; 
                            /*
                        case "loadaddins":
                            rebuildAddInTokens();
                            break;
                        case "unloadaddin":
                            string tokenName = words[1];
                            AddInToken removeToken =null; 
                            foreach (AddInToken t in allAddinTokens)
                                {
                                    if (t.Name.Equals(tokenName))
                                    {
                                        removeToken = t;
                                        break;
                                    }
                                }
                                allAddinTokens.Remove(removeToken);
                                removeToken = null;
                               // GC.Collect();

                            break;
                        case "getaddin":
                            string url = words[1];
                            string dirname = words[2];
                            this.CreateAddInDirectory(Globals.AddInRoot+"\\AddIns\\",dirname);
                            this.GetAddInZip(url, Globals.AddInRoot + "\\AddIns\\" + dirname, "newaddin.zip");
                            this.UnpackZip(Globals.AddInRoot + "\\AddIns\\" + dirname + "\\newaddin.zip", Globals.AddInRoot + "\\AddIns\\" + dirname);

                            break;
                            */
                        case "clear":
                            Console.Clear();
                            break;
                        case "stopallmodules":
                            this.StopAllModules();
                            break;
                            //***
                        case "startmodule":
                            {
                                string friendlyName = words[1];
                                string moduleName = words[2];

                                string[] moduleArgs = new string[words.Length - 3];
                                for (int index = 0; index < moduleArgs.Length; index++)
                                    moduleArgs[index] = words[index + 3];

                                ModuleInfo info = new ModuleInfo(friendlyName, moduleName, moduleName, null, false, moduleArgs);
                                StartModule(info);
                            }
                            break;
                        case "show":
                            {
                                string showWhat = words[1].ToLower();
                                switch (showWhat)
                                {
                                        //*** Adding new command that iterates over run-time states of running modules
                                    case "modulesstates":
                                        lock (this)
                                        {
                                            foreach (VModule module in runningModulesStates.Keys)
                                                Console.WriteLine("{0} state: {1} time: {2} runningverion: {3} desiredversion: {4}", module, (ModuleState.SimpleState)runningModulesStates[module].GetSimpleState(), runningModulesStates[module].GetTimestamp(), runningModules[module].GetRunningVersion(), runningModules[module].GetDesiredVersion());
                                        }
                                        break;
                                    case "addins": 
                                        lock (this)
                                        {
                                            foreach (AddInToken token in allAddinTokens)
                                            {
                                                Console.Write("AddInToken : {0}",token.Name.ToString()); 
                                             //   if(token.Version!=null)
                                                    // Console.Write(", Version: {0}", token.Version);
                                                    Console.Write(", Version: {0}", Utils.GetHomeOSUpdateVersion(Utils.GetAddInConfigFilepath(token.Name), logger));
                                              //  if(token.Publisher!=null)
                                                    Console.Write(", Publisher: {0}", token.Publisher);
                                               // if(token.AssemblyName!=null)
                                                    Console.Write(", Assembly: {0}", token.AssemblyName.Name);

                                                Console.WriteLine("");
                                            }
                                            break;
                                        }
                                    case "getconfsetting":
                                        lock (this)
                                        {
                                            string parameter = words[2];
                                            Console.WriteLine(GetConfSetting(parameter));
                                        }
                                        break;
                                    case "configdir":
                                        lock (this)
                                        {
                                            Console.WriteLine(Settings.ConfigDir);
                                        }
                                        break;
                                        //***
                                    case "ports":
                                        lock (this)
                                        {
                                            foreach (VPort port in registeredPorts.Keys)
                                                Console.WriteLine("{0} {1}", port.GetInfo(), registeredPorts[port]);
                                        }
                                        break;
                                    case "modules":
                                        lock (this)
                                        {
                                            foreach (VModule module in runningModules.Keys)
                                                Console.WriteLine(module.Secret() + " " + runningModules[module] + " " +runningModules[module].GetRunningVersion());
                                        }
                                        break;
                                    case "resourceusage":
                                        {
                                            if (null != heartbeatService)
                                            {
                                                HomeOS.Shared.HeartbeatInfo hbi = heartbeatService.GetPlatformHeartBeatInfo();
                                                if (null != hbi)
                                                    Console.WriteLine(hbi.ToString());
                                            }
                                        }
                                        break;
                                    default:
                                        Console.WriteLine("unknown show command " + words[1]);
                                        break;
                                }
                            }
                            break;
                        case "sendemail":
                            {
                                string dest = words[1];
                                string subject = "homeos testing";
                                string body = "This should be fine, cheers";
                                string mimeType = "image/jpeg";
                                List<Attachment> attachmentList = new List<Attachment>();                                
                                Attachment attachment = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10,10,9,9,30)), "test.jpg", mimeType);
                                attachmentList.Add(attachment);

                                Tuple<bool,string> result = Utils.SendEmail(dest, subject, body, attachmentList, this, logger);
                                logger.Log("result of email: Succeeded = " + result.Item1 + " " + "Message=" + result.Item2);
                            }
                            break;
                        case "sendhubemail":
                            {
                                string dest = words[1];
                                string subject = "homeos testing";
                                string body = "This should be fine, cheers";
                                string mimeType = "image/jpeg";
                                List<Attachment> attachmentList = new List<Attachment>();
                                Attachment attachment1 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 3, 3, 30)), "test1.jpg", mimeType);
                                Attachment attachment2 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 4, 4, 30)), "test2.jpg", mimeType);
                                Attachment attachment3 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 5, 5, 30)), "test3.jpg", mimeType);
                                Attachment attachment4 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 6, 6, 30)), "test4.jpg", mimeType);
                                Attachment attachment5 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 7, 7, 30)), "test5.jpg", mimeType);
                                attachmentList.Add(attachment1);
                                attachmentList.Add(attachment2);
                                attachmentList.Add(attachment3);
                                attachmentList.Add(attachment4);
                                attachmentList.Add(attachment5);

                                Tuple<bool, string> result = Utils.SendHubEmail(dest, subject, body, attachmentList, this, logger);
                                logger.Log("result of email: Succeeded = " + result.Item1 + " " + "Message=" + result.Item2);
                            }
                            break;
                        case "sendcloudemail":
                            {
                                string dest = words[1];
                                string subject = "homeos testing";
                                string body = "This should be fine, cheers";
                                string mimeType = "image/jpeg";
                                List<Attachment> attachmentList = new List<Attachment>();
                                Attachment attachment1 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 3, 3, 30)), "test1.jpg", mimeType);
                                Attachment attachment2 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 4, 4, 30)), "test2.jpg", mimeType);
                                Attachment attachment3 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 5, 5, 30)), "test3.jpg", mimeType);
                                Attachment attachment4 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 6, 6, 30)), "test4.jpg", mimeType);
                                Attachment attachment5 = new Attachment(new MemoryStream(Common.Utils.CreateTestJpegImage(10, 10, 7, 7, 30)), "test5.jpg", mimeType);
                                attachmentList.Add(attachment1);
                                attachmentList.Add(attachment2);
                                attachmentList.Add(attachment3);
                                attachmentList.Add(attachment4);
                                attachmentList.Add(attachment5);

                                Tuple<bool, string> result = Utils.SendCloudEmail(dest, subject, body, attachmentList, this, logger);
                                logger.Log("result of email: Succeeded = " + result.Item1 + " " + "Message=" + result.Item2);
                            }
                            break;
                        case "stopmodule":
                            {
                                int moduleSecret = int.Parse(words[1]);
                                StopModule(moduleSecret);
                            }
                            break;
                        //case "configurefoscam":
                        //    {
                        //        string username = words[1];
                        //        string password = (words.Length == 3) ? words[2] : "";

                        //        guiService.FindFoscamOnWire(username, password);
                        //    }
                        //    break;
                        case "test":
                            {
                                //do anything here that you like

                                //Gatekeeper.Settings.Configure(Settings.ConfigDir);

                                //int numTimes = int.Parse(words[1]);

                                //for (int i = 0; i < numTimes; i++)
                                //{
                                //    config.UpdateConfSetting(i.ToString(), i.ToString());
                                //}

                                while (true)
                                {
                                    //string url = words[1];

                                    string url = "http://localhost:51430/ratul/GuiWeb/webapp/GetAllUnconfiguredDevices";
                                    // string json = ""; // Your JSON message
                                    WebRequest request = WebRequest.Create(url);
                                    //request.Method = "POST";
                                    //var postData = Encoding.UTF8.GetBytes(json);
                                    //request.ContentLength = postData.Length;
                                    //request.ContentType = "text/json";
                                    //using (var reqStream = request.GetRequestStream())
                                    //{
                                    //    reqStream.Write(postData, 0, postData.Length);
                                    //}
                                    try
                                    {
                                        var response = request.GetResponse();

                                        logger.Log("response = " + response.ToString());

                                    }
                                    catch (WebException e)
                                    {
                                        logger.Log(e.Message);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Log("EXCEPTION: " + ex.ToString());
                                    }

                                    Thread.Sleep(1000);
                                }

                            }
                            break;
                        case "configurewebcam":
                            {
                                string devName = "Integrated Camera";

                                var result1 = guiService.GetUnconfiguredDevicesForScout("HomeOS.Hub.Scouts.WebCam");
                                var result2 = guiService.StartDriver(devName);

                                var result3 = guiService.IsDeviceReady(devName);

                                while (!result3[0].Equals(""))
                                {
                                    logger.Log("Device not ready yet. Will try again in 2 seconds");
                                    Thread.Sleep(2000);
                                    result3 = guiService.IsDeviceReady(devName);
                                }

                                var result4 = guiService.InstallAppWeb("SmartCam");
                                var result5 = guiService.ConfigureDeviceWeb(devName, "mycam", true, "Basement", new string[1] {"SmartCam"});
                            }
                            break;
                        case "starthostednetwork":
                            {
                                try
                                {
                                    var result = StartHostedNetwork();
                                    logger.Log("Result = " + result.ToString());
                                }
                                catch (Exception e)
                                {
                                    logger.Log("Exception: " + e.ToString());
                                }
                            }
                            break;
                        case "stophostednetwork":
                            {
                                try
                                {
                                    var result = StopHostedNetwork();
                                    logger.Log("Result = " + result.ToString());
                                }
                                catch (Exception e)
                                {
                                    logger.Log("Exception: " + e.ToString());
                                }
                            }
                            break;
                        case "guigeneric":
                            {
                                string methodName = words[1];
                                Type type = guiService.GetType();
                                System.Reflection.MethodInfo method = type.GetMethod(methodName);

                                if (method == null)
                                {
                                    logger.Log("Method {0} not found (check for proper capitalization)", methodName);
                                    break;
                                }

                                object[] parameters = new object[words.Length - 2];

                                for (int index = 0; index < parameters.Length; index++)
                                    parameters[index] = words[2 + index];
                                                                
                                var result = (List<string>) method.Invoke(guiService, parameters);
                                PrintGuiCallResult(result);
                            }
                            break;
                        case "setscouts":
                            {
                                string[] scouts = new string[words.Length - 1];

                                for (int index = 1; index < words.Length; index++)
                                {
                                    scouts[index - 1] = words[index];
                                }

                                var result = guiService.SetScouts(scouts);
                                PrintGuiCallResult(result);
                            }
                            break;
                        case "removezwavenode":
                            {
                                // get and check the zwave driver
                                VModule driverZwave = GetDriverZwave();

                                if (driverZwave == null)
                                {
                                    logger.Log("zwave driver is not running");
                                    break;
                                }
                                
                                string result = (string) driverZwave.OpaqueCall("RemoveDevice"); 

                                logger.Log("Result of remove zwave = " + result);
                            }
                            break;
                        default:
                            //didn't see any recognized command
                            Console.WriteLine("unknown command " + command);
                            PrintInteractiveUsage();
                            break;
                    }

                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Problem parsing/executing command: " + e);
                    PrintInteractiveUsage();
                }
            }
        }

        private void PrintGuiCallResult(List<string> result)
        {
            for (int index = 0; index < result.Count; index++)
                logger.Log("{0}: {1}", index.ToString(), result[index]);
        }

        private bool StartHostedNetwork()
        {
            List<string> stdout = new List<string>();
            List<string> stderr = new List<string>();

            string filename = "netsh";
            string args1 = "wlan set hostednetwork mode=allow ssid=setup key=hellowworld";

            Utils.RunCommandTillEnd(filename, args1, stdout, stderr, logger);

            string args2 = "wlan start hostednetwork";

            Utils.RunCommandTillEnd(filename, args2, stdout, stderr, logger);

            if (stdout[stdout.Count - 1].Contains("The hosted network started"))
                return true;
            else
            {
                logger.Log("it appears that the hostednetwork did not start. the last stdout string was: {0}. does the machine have a wireless card? does it support virtual wifi?", stdout[stdout.Count - 1]);
                return false;
            }
        }

        private bool StopHostedNetwork()
        {
            //WlanManager wlanManager = new WlanManager();
            //wlanManager.StopHostedNetwork();

            List<string> stdout = new List<string>();
            List<string> stderr = new List<string>();

            string filename = "netsh";
            string args = "wlan stop hostednetwork";

            Utils.RunCommandTillEnd(filename, args, stdout, stderr, logger);

            if (stdout[stdout.Count - 1].Contains("The hosted network stopped"))
                return true;
            else
            {
                logger.Log("it appears that the hostednetwork did not start. the last stdout string was: {0}. does the machine have a wireless card? does it support virtual wifi?", stdout[stdout.Count - 1]);
                return false;
            }                      
        }

        public List<HomeOS.Shared.ModuleMonitorInfo> GetModuleMonitorInfoList()
        {
            List<HomeOS.Shared.ModuleMonitorInfo> amiList = new List<HomeOS.Shared.ModuleMonitorInfo>();
            lock (this)
            {
                foreach (var module in runningModules.Keys)
                {
                    var resources = module.GetResourceUsage();
                    HomeOS.Shared.ModuleMonitorInfo ami = new HomeOS.Shared.ModuleMonitorInfo();
                    ami.ModuleFriendlyName = runningModules[module].FriendlyName();
                    ami.ModuleVersion = runningModules[module].GetRunningVersion();
                    ami.MonitoringTotalProcessorTime = resources[0];
                    ami.MonitoringTotalAllocatedMemorySize = resources[1];
                    ami.MonitoringSurvivedMemorySize = resources[2];
                    ami.MonitoringSurvivedProcessMemorySize = resources[3];
                    amiList.Add(ami);
                }
            }
            HomeOS.Shared.ModuleMonitorInfo amiTotal = new HomeOS.Shared.ModuleMonitorInfo();
            amiTotal.ModuleFriendlyName = "platform";
            amiTotal.ModuleVersion = this.platformVersion;
            amiTotal.MonitoringTotalProcessorTime = (long)AppDomain.CurrentDomain.MonitoringTotalProcessorTime.TotalMilliseconds;
            amiTotal.MonitoringTotalAllocatedMemorySize = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            amiTotal.MonitoringSurvivedMemorySize = AppDomain.CurrentDomain.MonitoringSurvivedMemorySize;
            amiTotal.MonitoringSurvivedProcessMemorySize = AppDomain.MonitoringSurvivedProcessMemorySize;

            amiList.Add(amiTotal);

            return amiList;
        }

        public string GetPlatformVersion()
        {
            return this.platformVersion;
        }

        public List<HomeOS.Shared.ScoutInfo> GetScoutInfoList()
        {
            List<HomeOS.Shared.ScoutInfo> siList = new List<HomeOS.Shared.ScoutInfo>();
            lock (this)
            {
                foreach (var Scout in runningScouts.Keys)
                {
                    HomeOS.Shared.ScoutInfo si = new HomeOS.Shared.ScoutInfo();
                    si.ScoutFriendlyName = runningScouts[Scout].Item1.Name;
                    si.ScoutVersion = runningScouts[Scout].Item1.RunningVersion;
                    siList.Add(si);
                }
            }
            return siList;
        }

        public List<ScoutInfo> GetRunningScoutInfos()
        {
            List<ScoutInfo> siList = new List<ScoutInfo>();
            lock (this)
            {
                foreach (var Scout in runningScouts.Values)
                {
                    siList.Add(Scout.Item1);
                }
            }
            return siList;
        }

        /// <summary>
        /// prints a usage message on the console
        /// </summary>
        void PrintInteractiveUsage()
        {
            //this is incomplete
            Console.WriteLine("------------- Commands -------------");
            Console.WriteLine("");
            Console.WriteLine(">show alladdins");
            Console.WriteLine("To show all AddIns that are available, loaded, and ready to start as modules. AddInToken names are same as binary names.");
            Console.WriteLine("");
            Console.WriteLine(">startmodule {friendlyName} {binaryName}");
            Console.WriteLine("Command to start a module from binary called binaryName with a friendly name assigned as friendlyName");
            Console.WriteLine("");
            Console.WriteLine(">show modules");
            Console.WriteLine("Command to dispplay currently running modules and their secrets.");
            Console.WriteLine("");
            Console.WriteLine(">show modulesstates");
            Console.WriteLine("Command to dispplay currently what state running modules are in.");
            Console.WriteLine("");
            Console.WriteLine(">show resourceusage");
            Console.WriteLine("Command to display CPU and memory consumption of running modules.");
            Console.WriteLine("");
            Console.WriteLine(">show ports");
            Console.WriteLine("Command to display ports exported by currently running modules.");
            Console.WriteLine("");
            Console.WriteLine(">show wifinets");
            Console.WriteLine("Command to display currenlty available wifi networks.");
            Console.WriteLine("");
            Console.WriteLine(">stopmodule {secret}");
            Console.WriteLine("Command to stop module with given secret.");
            Console.WriteLine("");
            Console.WriteLine(">exit");
            Console.WriteLine("Command to quit all modules and HomeOS platform.");
            Console.WriteLine("");
            Console.WriteLine("There are some other commands available, with no help (yet). Take a guess, or see code :-) ");
            Console.WriteLine("------------------------------------");
        }

        /// <summary>
        /// Processes the command line arguments
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private ArgumentsDictionary ProcessArguments(string[] arguments)
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
                    "LogFile",
                    'l',
                    DEFAULT_COMMAND_LINE_ARG_VAL,
                    "file name",
                    "Log file name ('-' for stdout)"),
                new ArgumentSpec(
                    "ConfigDir",
                    'c',
                    DEFAULT_COMMAND_LINE_ARG_VAL,
                    "directory",
                    "Configuration directory"),
                new ArgumentSpec(
                    "RunningMode",
                    'r',
                     DEFAULT_COMMAND_LINE_ARG_VAL,
                    "string",
                    "Running Mode"),
                new ArgumentSpec(
                    "EnforcePolicies",
                    'p',
                    DEFAULT_COMMAND_LINE_ARG_VAL,
                    "true/false",
                    "Turn policy enforcement on/off"),
                new ArgumentSpec(
                    "StayOffline",
                    'o',
                    DEFAULT_COMMAND_LINE_ARG_VAL,
                    "true/false",
                    "Whether we should stay offline")
            };

            ArgumentsDictionary args = new ArgumentsDictionary(arguments, argSpecs);
            if (args.AppSettingsParseError)
            {
                Console.Error.WriteLine("Error in .config file options: ignoring");
            }

            if (args.CommandLineParseError)
            {
                Console.Error.WriteLine("Error in command line arguments at {0}\n", args.ParseErrorArgument);
                Console.Error.WriteLine(args.GetUsage("homeos"));
                System.Environment.Exit(1);
            }

            if ((bool)args["Help"])
            {
                Console.Error.WriteLine("Runs HomeOS\n");
                Console.Error.WriteLine(args.GetUsage("homeos"));
                System.Environment.Exit(0);
            }

            //if the supplied config path is relative, make it absolute
            if (!DEFAULT_COMMAND_LINE_ARG_VAL.Equals((string)args["ConfigDir"]))
            {
                string configDir = (string)args["ConfigDir"];

                if (!Directory.Exists(configDir))
                {
                    Console.Error.WriteLine("Configuration directory does not exist: " + configDir);
                    System.Environment.Exit(1);
                }

                args["ConfigDir"] = Path.GetFullPath(configDir);
            }


           // if ((string)args["ConfigDir"] != "\\Config")
         //   {
             //  Settings.SetParameter("ConfigDir", Environment.CurrentDirectory + "\\" + (string)args["ConfigDir"]);
            //}

            //Settings.RunningMode = (string) args["RunningMode"];

            if (Settings.RunningMode.Equals("unittesting"))
            {
                // need to set the global switch here since doing it in Configured Start is too late as Logger
                // settings for unittesting do not get affected correctly
                //Settings.UnitTestingEnabled = true;

                // Current directory is not the output directory when platform is launched from the unit tests,
                // this is needed because we don't want to set the output directory in the unit test project to the
                // global output dir.
                // ....
                // evgeni -- per my email, this should be needed any more -- ratul
                // if true please delete all this code
                // ....
                //Constants.AddInRoot = Environment.CurrentDirectory + "\\..\\..\\..\\output\\binaries\\Pipeline"; 
                //Constants.ScoutRoot = Environment.CurrentDirectory + "\\..\\..\\..\\output\\binaries\\Scouts";
                //Settings.ConfigDir = Environment.CurrentDirectory + "\\..\\..\\..\\output\\Configs\\Config";
                
                //Constants.LocationsFileName = Environment.CurrentDirectory + "\\..\\..\\..\\output\\Configs\\Config\\" + Constants.LocationsFileName;
            }

            //Settings.EnforcePolicies = !(bool)args["NoPolicyEnforcement"];
            
            return args;
        }

        #region functions called by the device scouts

        public void ProcessNewDiscoveryResults(List<Device> deviceList) 
        {
            config.ProcessNewDiscoveryResults(deviceList);
        }

        public void SetDeviceDriverParams(Device targetDevice, List<string> paramList)
        {
            config.SetDeviceDriverParams(targetDevice, paramList);
        }

        public List<string> GetDeviceDriverParams(Device targetDevice)
        {
            return config.GetDeviceDriverParams(targetDevice);
        }

        #endregion

        public void AddService(PortInfo portInfo, string friendlyName, bool highSecurity, string locationStr, string[] apps) {
            logger.Log("AddService is called on " + friendlyName + " for " + portInfo.ToString() + " loc:" + locationStr + " sec: " + highSecurity + " #apps " + apps.Length.ToString());

            portInfo.SetFriendlyName(friendlyName);
            portInfo.SetSecurity(highSecurity);

            Location location = config.GetLocation(locationStr);
            if (location == null)
                throw new Exception("Unknown location " + locationStr);

            portInfo.SetLocation(location);
            location.AddChildPort(portInfo);

            config.AddConfiguredPort(portInfo);

            foreach (string app in apps)
            {
                if (config.GetModule(app) != null)
                    AllowAppAcccessToDevice(app, friendlyName);
                else
                    logger.Log("ERROR: Could not give access to device {0} to app {1} because the app does not exist", friendlyName, app);

                //AccessRule rule = new AccessRule();
                //rule.RuleName = portInfo.GetFriendlyName();
                //rule.ModuleName = app;
                //rule.UserGroup = "everyone";
                //rule.AccessMode = Common.AccessMode.Allow;
                //rule.Priority = 0;
                
                //rule.DeviceList = new List<string>();
                //rule.DeviceList.Add(friendlyName);
                
                //rule.TimeList = new List<TimeOfWeek>();
                //rule.TimeList.Add(new TimeOfWeek(-1, 0, 2400));

                //policyEngine.AddAccessRule(rule);

                //config.AddAccessRule(rule);
            }

            //send port registration message to all modules now that this service has been registered
            //  first, get the port object and the owner module
            VPort portToRegister = null;
            VModule ownerModule = null;
            lock (this) {
                foreach (VPort port in registeredPorts.Keys)
                {
                    if (port.GetInfo().Equals(portInfo))
                    {
                        portToRegister = port;
                        ownerModule = registeredPorts[port];
                        break;
                    }
                }
            }

            if (portToRegister != null)
            {
                SafeThread newThread = new SafeThread(delegate()
                    {
                        BroadcastPortRegistration(portToRegister, ownerModule);
                    }, "AddService:RegisterPort " + portToRegister, logger);
                
                newThread.Start();
            }
        }

        public void AllowAppAcccessToDevice(string appFriendlyName, string deviceFriendlyName)
        {
            AccessRule rule = new AccessRule();
            rule.RuleName = appFriendlyName + "-" + deviceFriendlyName;
            rule.ModuleName = appFriendlyName;
            rule.UserGroup = "everyone";
            rule.AccessMode = Common.AccessMode.Allow;
            rule.Priority = 0;

            rule.DeviceList = new List<string>();
            rule.DeviceList.Add(deviceFriendlyName);

            rule.TimeList = new List<TimeOfWeek>();
            rule.TimeList.Add(new TimeOfWeek(-1, 0, 2400));

            policyEngine.AddAccessRule(rule);

            config.AddAccessRule(rule);
        }

        public bool DisallowAppAccessToDevice(string appFriendlyName, string deviceFriendlyName)
        {

            bool result = policyEngine.RemoveAccessRule(appFriendlyName, deviceFriendlyName);

            if (!result)
                logger.Log("Warning: no rule for {0}-{1} was found in the policy engine", appFriendlyName, deviceFriendlyName);

            return config.RemoveAccessRule(appFriendlyName, deviceFriendlyName);
        }

        public void RemoveAccessRulesForDevice(string deviceFriendlyName)
        {
            policyEngine.RemoveAccessRulesForDevice(deviceFriendlyName);
            config.RemoveAccessRulesForDevice(deviceFriendlyName);
        }

        public void RemoveAccessRulesForModule(string moduleFriendlyName)
        {
            policyEngine.RemoveAccessRulesForModule(moduleFriendlyName);
            config.RemoveAccessRulesForModule(moduleFriendlyName);
        }

        public void AddAccessRule(AccessRule rule)
        {
            policyEngine.AddAccessRule(rule);
            config.AddAccessRule(rule);
        }

        //TODO: this function should be retired
        public Tuple<bool, string> StartDriverForDevice(Device device, string username, string password)
        {
            return StartDriverForDevice(device, new List<string>() { device.UniqueName, username, password });
        }

        public Tuple<bool, string> StartDriverForDevice(Device device, List<string> driverParams)
        {
            if (device.DriverBinaryName.Equals(""))
            {
                return new Tuple<bool, string>(false, "Driver not specified");
            }

            string driverFriendlyName = device.DriverBinaryName + " for " + device.FriendlyName;
            string driverAppName = "driver for " + device.FriendlyName;

            ModuleInfo moduleInfo = new ModuleInfo(driverFriendlyName, driverAppName, device.DriverBinaryName, null, true, driverParams.ToArray());

            // this will be set in StartModule -- why set here
            //moduleInfo.SetWorkingDir(Environment.CurrentDirectory + "\\" + moduleInfo.FriendlyName());

            moduleInfo.SetManifest(new Manifest());
            moduleInfo.Background = true;

            StartModule(moduleInfo);

            //add the module to the configuration and update details for the device
            //these changes are written to disk
            config.AddModule(moduleInfo);
            config.UpdateDeviceDetails(device.UniqueName, true, driverFriendlyName);

            return new Tuple<bool, string>(true, "Added driver module");
        }

        public List<Device> GetDevicesForScout(string scoutName)
        {
            if (!runningScouts.ContainsKey(scoutName))
            {
                logger.Log("Error: Someone asked for devices for a non-existent scout {0}.\n Stack trace: {1}", scoutName, Environment.StackTrace);

                return null;
            }

            return runningScouts[scoutName].Item2.GetDevices();
        }

        public List<string> GetAllRunningScoutNames()
        {
            var retList = new List<string>();

            foreach (var scoutName in runningScouts.Keys)
            {
                retList.Add(scoutName);
            }

            return retList;
        }

        public bool StopScout(string scoutName)
        {
            lock (runningScouts)
            {
                if (!runningScouts.ContainsKey(scoutName))
                    return false;

                logger.Log("Stopping scout: " + scoutName);
                runningScouts[scoutName].Item2.Dispose();

                runningScouts.Remove(scoutName);

                return true;
            }            
        }

        public VModule GetDriverZwave()
        {
            lock (this)
            {
                foreach (VModule module in runningModules.Keys)
                {
                    //it is a contains, not equality because we could have a version number at the end of 
                    if (module.GetInfo().BinaryName().Contains("HomeOS.Hub.Drivers.ZwaveZensys"))
                    {
                        return module;
                    }
                }
            }

            return null;
        }

        #region methods to stop all modules, scouts and release data structures
        private void StopAllModules()
        {
                List<int> driverSecrets = new List<int>();
                List<int> appSecrets = new List<int>();

                lock (runningModules)
                {
                
                foreach (VModule module in runningModules.Keys) // enumeration cannot continue while dictionary is being modified (e.g., modules stopped). Lets first read the secrets of all modules.
                {
                        logger.Log("Module to stop: " + module.GetInfo().BinaryName());
                        if (registeredPorts.Values.Contains(module))
                            driverSecrets.Add(module.Secret());
                        else
                            appSecrets.Add(module.Secret());
                }
                }

                foreach (int secret in appSecrets)
                {
                    StopModule(secret);
                }

                foreach (int secret in driverSecrets)
                {
                    StopModule(secret);
                }

            
          }

        private void DisposeAllScouts()
        {
            lock (runningScouts)
            {
                foreach (string scout in runningScouts.Keys)
                {
                    logger.Log("Disposing scout: "+scout);
                    runningScouts[scout].Item2.Dispose();
                }
                runningScouts.Clear();
            }

        }

#region static helpers for Update Manager tool to access modules, scouts
        public static Dictionary<string, ModuleInfo> GetConfigModules(string configDir)
        {
            Settings.Initialize();            
            Configuration config = new Configuration(configDir);
            config.ParseSettings();
            config.ReadConfiguration();
            return config.allModules;
        }

        public static List<ScoutInfo> GetConfigScouts(string configDir)
        {
            Settings.Initialize();
            Configuration config = new Configuration(configDir);
            config.ParseSettings();
            config.ReadConfiguration();
            return config.GetAllScouts();
        }

        public static Dictionary<string /*binaryName*/, string /*version*/> GetAllModuleBinaries(string outputRoot)
        {
            Dictionary<string,string> moduleDict = new Dictionary<string,string>();
            string addInRoot = outputRoot + "\\binaries\\Pipeline";
            Collection<AddInToken> tokens = GetModuleList(addInRoot);
            foreach (AddInToken token in tokens)
            {
                string version = GetVersionFromBinaryName(addInRoot + "\\AddIns", token.Name);
                if (!(moduleDict.ContainsKey(token.Name)))
                {
                    moduleDict.Add(token.Name, version);
                }
            }

            return moduleDict;
        }

        private static string GetVersionFromBinaryName(string binaryRoot, string binaryName)
        {
            string version = Constants.UnknownHomeOSUpdateVersionValue;
            string baseDir = binaryRoot + "\\" + binaryName;
            string dllPath = baseDir + "\\" + binaryName + ".dll";

            string dllFullPath = Path.GetFullPath(dllPath);

            if (!File.Exists(dllFullPath))
                return version;
            version = Utils.GetHomeOSUpdateVersion(dllFullPath + ".config", null);

            return version;
        }

        public static Dictionary<string /*binaryName*/, string /*version*/> GetAllScoutBinaries(string outputRoot)
        {
            string scoutRoot = outputRoot + "\\binaries\\Scouts";
            string[] scoutDirPaths = Directory.GetDirectories(scoutRoot);

            Dictionary<string, string> scoutDict = new Dictionary<string,string>();
            foreach (string scoutPath in scoutDirPaths)
            {
                string binName = scoutPath.Split(new char[] { '\\' }).Last();
                string version = GetVersionFromBinaryName(scoutRoot, binName);
                scoutDict.Add(binName, version);
            }
            return scoutDict;
        }
#endregion 

        public void Restart()
        {
            logger.Log("Restarting platform with config from {0}", Settings.ConfigDir);
            this.LoadConfigFromDir(Settings.ConfigDir);
        }

        public void ForceShutdown()
        {
            logger.Log("Forcing Platform Shutdown", Settings.ConfigDir);

            SafeThread shutdownThread = new SafeThread(delegate { Shutdown(); }, "shutdown thread", logger);
            shutdownThread.Start();

            //wait for 2 minutes
            shutdownThread.Join(new TimeSpan(0, 2, 0));

            System.Environment.Exit(0);
        }

        private void Shutdown()
        {
            this.StopAllModules();
            this.DisposeAllScouts();
            this.Dispose();
            if(homeService!=null) 
                homeService.Stop();


            this.runningScouts = null;
            this.runningModules = null;
            this.runningModulesStates = null;
            this.registeredPorts = null;
            this.registeredPorts = null;
            this.config = null;
            this.allAddinTokens = null;
            this.policyEngine = null;
            //this.gadgetListener = null;
            this.guiService = null;
            this.homeService = null;
            this.infoService = null;
            this.discoveryHelperService = null;
            this.heartbeatService = null;
            this.random = null;

            GC.Collect();

            //finally close the logger object
            if (logger != null)
                logger.Close();
        }
        #endregion

        #region Update module state method -rayman

        public void UpdateState(VModule module , VModuleState state )
        {

       //     Console.WriteLine("*** PLATFORM HAS UPDATED THE STATE of "+module.GetInfo().BinaryName()+" with secret "+module.Secret()+" to "+state.GetSimpleState());
          
            ResultCode result = new ResultCode();
            if (runningModules.ContainsKey(module)) // This will check if the module exists in runningModules AND the secrets match
            {
                lock (this)
                {
                    if (!runningModulesStates.ContainsKey(module)) // updating the first time. must be an "EnterStart" state
                    {
                        runningModulesStates[module] = state;
                    }
                    else
                    {
                        runningModulesStates[module].Update(state);
                    }
                    result = ResultCode.Success;
                }
            }
            else
            {
                result = ResultCode.ModuleNotFound;
            }


            if (result == ResultCode.Success  )
            {
          //      logger.Log(this + " updated state of module {0} to {1} ",  module.ToString(), ((ModuleState.SimpleState)state.GetSimpleState()).ToString());
            }
            else
            {
                logger.Log(this + " got invalid state update: {0} from module {1}", state.GetSimpleState().ToString(), module.ToString());
            }

        }

        private bool CanEnterPortRegistered(VModule module)
        {
            if (runningModulesStates.ContainsKey(module))
            {
                if (runningModulesStates[module].GetSimpleState() == (int)ModuleState.SimpleState.ExitStart 
                    || runningModulesStates[module].GetSimpleState() == (int)ModuleState.SimpleState.ExitPortDeregistered
                    || runningModulesStates[module].GetSimpleState() == (int)ModuleState.SimpleState.ExitPortRegistered )
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        private bool CanEnterPortDeregistered(VModule module)
        {
            if (runningModulesStates.ContainsKey(module))
            {
                if (runningModulesStates[module].GetSimpleState() == (int)ModuleState.SimpleState.ExitStart
                    || runningModulesStates[module].GetSimpleState() == (int)ModuleState.SimpleState.ExitPortDeregistered
                    || runningModulesStates[module].GetSimpleState() == (int)ModuleState.SimpleState.ExitPortRegistered)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }




        #endregion

        #region  Method to remove ports from config when modules stop -rayman

        private void RemovePortFromConfig(VPort port , VModule module)
        {
            PortInfo targetPortInfo = new PortInfo(port.GetInfo().ModuleFacingName(), module.GetInfo());
            PortInfo matchingPortInfo = config.GetMatchingPortInfo(targetPortInfo);
            if (matchingPortInfo == null)
            {
            }
            else
            {
                config.RemovePort(matchingPortInfo);
            } 

        }

        #endregion

        #region Method to cleanup AddIn after module is stopped. Currently implemented only for AppDomain Isolation. -rayman

        private void AddInCleanup(VModule moduleStopped)
        {

            //cleanup 
            if (Constants.ModuleIsolationLevel == ModuleIsolationModes.AppDomain)
            {
                logger.Log("AppDomain cleanup for "+ moduleStopped.GetInfo().AppName());
                bool done = false;
                AddInController aiController = AddInController.GetAddInController(moduleStopped);
                SafeThread t = new SafeThread(delegate()
                {
                    while (!done)
                    {
                        try
                        {
                            aiController.Shutdown();
                            done = true;
                        }
                        catch (CannotUnloadAppDomainException)
                        {
                            logger.Log("AppDomain Unload did not succeed. Retrying.");
                            System.Threading.Thread.Sleep(1000);
                            // keep trying to unload until it gets unloaded
                        }
                    }
                }, moduleStopped.ToString()+"-UnloadingAppDomain", logger);
                t.Start();
                t.Join(TimeSpan.FromMilliseconds(Settings.MaxFinallyBlockExecutionTime));
                if(t.IsAlive())
                    t.Abort();
            }
            else if (Constants.ModuleIsolationLevel == ModuleIsolationModes.Process)
            {
                //TODO: test this
                AddInController aiController = AddInController.GetAddInController(moduleStopped);
                aiController.Shutdown();
            }
            else if (Constants.ModuleIsolationLevel == ModuleIsolationModes.NoAddInAppDomain)
            {
                // TODO handle cleanup here
            }
            else
            {// Globals.ModuleIsolationLevel == ModuleIsolationModes.None
                // TODO handle cleanup here
            }

        }

        #endregion

        #region Method to invoke portderegistered on module -rayman

        private void InvokePortDeregistered(VModule module, VPort port)
        {
            bool done = false;
            while (!done)
            {      
                if (this.CanEnterPortDeregistered(module))
                //*** Check if the module is ready to deregister a port (e.g., it has finished its start() or its portregistered() ) 
                {
                    try
                    {
                        module.PortDeregistered(port);
                        done = true;
                    }
                    catch (NullReferenceException)
                    {
                        logger.Log("NullReferenceException thrown in Bcast Port deregistration");
                        //Ratul TODO: not quite sure why this gets thrown, but for now this works
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
        }
        #endregion


        #region method to cancel all of a module's subscriptions -rayman

        public void CancelAllSubscriptions(VModule module , VPort controlPort, VCapability controlportcap)
        {
            foreach (VPort p in registeredPorts.Keys)
            {
                if (!registeredPorts[p].Equals(module))
                {
                    foreach (VRole r in p.GetInfo().GetRoles())
                    {
                        foreach (VOperation o in r.GetOperations())
                        {
                            logger.Log("Unsubscribing " + controlPort + " from  port: " + p + " operation: " + o);
                            p.Unsubscribe(r.Name(), o.Name(), controlPort, controlportcap);
                        }
                    }
                }
            }

        }


        #endregion

        #region methods to load new configuration from a new config directory 

        public void LoadConfigFromDir(String configdir)
        {
            logger.Log("Loading configuration from configdir: " + configdir+ ". updating any -c/--ConfigDir command line args.");
            
            this.Shutdown();// shutdown all modules and free memory
            
            // if the initial command line arguments specified a config directory we update it to reflect the 
            // new config dir, from which config is being loaded now
            if (this.arguments.Contains("-c") && Array.IndexOf(this.arguments, "-c")!=arguments.Length-1)
            {
                this.arguments[Array.IndexOf(this.arguments,"-c") + 1] = configdir;
            }
            else if (this.arguments.Contains("--ConfigDir") && Array.IndexOf(this.arguments, "--ConfigDir") != arguments.Length - 1)
            {
                this.arguments[Array.IndexOf(this.arguments, "--ConfigDir") + 1] = configdir;
            }
            else
            {
                Array.Resize(ref arguments, arguments.Length + 2);
                arguments[arguments.Length - 2] = "-c";
                arguments[arguments.Length - 1] = configdir;
            }


            this.Initialize(this.arguments);
            
            try
            {
                if (this.configLookup != null)
                    this.configLookup.Reset(config, this.logger, Settings.ConfigLookupFrequency, (LoadConfig)this.LoadConfigFromDir);
            }
            catch (Exception e)
            {
                logger.Log("Exception in Resetting config updater " + e);
            }

            this.Start();

            

        }


     
        #endregion


        #region methods to support adding AddIns at runtime -rayman

        private bool CompareModuleVersions(string moduleVersion1, string moduleVersion2)
        {
            if (moduleVersion1 == null && moduleVersion2 == null)
                return true;
            else if (moduleVersion1 != null && moduleVersion2 == null)
                return false;
            else if (moduleVersion1 == null && moduleVersion2 != null)
                return false;
            else
                return moduleVersion1.Equals(moduleVersion2); 

        }

        private bool GetAddInFromRep(ModuleInfo moduleInfo , bool rebuild = true)
        {

            Dictionary<string,bool> repAvailability = AvailableOnRep(moduleInfo) ; 
            if (!repAvailability.ContainsValue(true))
            {
                logger.Log("Can't find "+moduleInfo.BinaryName() + " v "+ moduleInfo.GetDesiredVersion()+" on any rep.");
                return false;
            }
                      
            // on fetching the binaries of a module existing older versions shall be overwritten
            // making sure older version module is not running.
            foreach (ModuleInfo runningModuleInfo in runningModules.Values)
            {
                if (runningModuleInfo.BinaryName().Equals(moduleInfo.BinaryName()))
                    throw new Exception(String.Format("Attempted to fetch module, with same binary name module running. Running: ({0}, v {1}) Fetching: ({2}, v{3})", runningModuleInfo.BinaryName(), runningModuleInfo.GetRunningVersion() , moduleInfo.BinaryName() , moduleInfo.GetDesiredVersion()));
            }

            //unloading addin
            AddInToken removeToken = null;
            foreach (AddInToken t in allAddinTokens)
            {
                if (t.Name.Equals(moduleInfo.BinaryName()))
                {
                    removeToken = t;
                    break;
                }
            }
            allAddinTokens.Remove(removeToken);
            removeToken = null;
            GC.Collect();
            try
            {
                CreateAddInDirectory(Constants.AddInRoot + "\\AddIns\\", moduleInfo.BinaryName());
                DownloadFile(repAvailability.FirstOrDefault(x => x.Value == true).Key, Constants.AddInRoot + "\\AddIns\\" + moduleInfo.BinaryName(), moduleInfo.BinaryName() + ".zip");
                UnpackZip(Constants.AddInRoot + "\\AddIns\\" + moduleInfo.BinaryName() + "\\" + moduleInfo.BinaryName() + ".zip", Constants.AddInRoot + "\\AddIns\\" + moduleInfo.BinaryName());
                if(rebuild)
                    rebuildAddInTokens();

                File.Delete(Constants.AddInRoot + "\\AddIns\\" + moduleInfo.BinaryName() + "\\" + moduleInfo.BinaryName() + ".zip");

            }
            catch (Exception e)
            {
                logger.Log("Exception : " + e);
                return false;
            }
            return true; 
        }

        private bool GetScoutFromRep(ScoutInfo scoutInfo)
        {
            logger.Log("fetching scout "+scoutInfo.Name +" v "+scoutInfo.DesiredVersion+" from reps");
            Dictionary<string, bool> repAvailability = AvailableOnRep(scoutInfo);
            if (!repAvailability.ContainsValue(true))
            {
                logger.Log("Can't find " + scoutInfo.DllName + " v" + scoutInfo.DesiredVersion + " on any rep.");
                return false;
            }

            foreach (Tuple<ScoutInfo,IScout> runningScoutTuple in runningScouts.Values)
            {
                if (runningScoutTuple.Item1.DllName.Equals(scoutInfo.DllName))
                    throw new Exception(String.Format("Attempted to fetch scout, with same binary name scout running. Running: ({0}, v {1}) Fetching: ({2}, v{3})",
                               runningScoutTuple.Item1.DllName, runningScoutTuple.Item1.DesiredVersion , scoutInfo.DllName, scoutInfo.DesiredVersion));
            }

            try
            {
                string baseDir = Constants.ScoutRoot + "\\" + scoutInfo.Name;

                CreateAddInDirectory(Constants.ScoutRoot, scoutInfo.Name);
                DownloadFile(repAvailability.FirstOrDefault(x => x.Value == true).Key, baseDir , scoutInfo.DllName + ".zip");
                UnpackZip(baseDir + "\\" + scoutInfo.DllName + ".zip", baseDir );
                
                File.Delete(baseDir + "\\" + scoutInfo.DllName + ".zip");
            }
            catch (Exception e)
            {
                logger.Log("Exception : " + e);
                return false;
            }
            return true; 
        }

        private Dictionary<string,bool> AvailableOnRep(ModuleInfo moduleInfo)
        {
            string[] URIs = Settings.RepositoryURIs.Split('|'); 
            String[] path = moduleInfo.BinaryName().Split('.');
            Dictionary<string, bool> retval = new Dictionary<string,bool>();

            foreach (string uri in URIs)
            {
                //logger.Log("Checking " + moduleInfo.BinaryName() + " v" + moduleInfo.GetVersion() + "  availability on Rep: " + uri);

                //string zipuri = uri +'/' + path[0] + '/' + path[1] + '/' + path[2] + '/' + path[3] + '/' + moduleInfo.GetVersion() + '/' + moduleInfo.BinaryName() + ".zip";

                string zipuri = uri;

                foreach (string pathElement in path)
                    zipuri += "/" + pathElement;


                //by default platform should point to the Latest on the repository if a version of the binary isn't specified

                string binaryversion = "Latest";

                if (moduleInfo.GetDesiredVersion() != null && moduleInfo.GetDesiredVersion() != "0.0.0.0")
                {
                    binaryversion = moduleInfo.GetDesiredVersion();
                }

                zipuri += '/' + binaryversion + '/' + moduleInfo.BinaryName() + ".zip";

                if (UrlIsValid(zipuri))
                {
                    retval[zipuri]=true;
                    return retval;
                }
            }
            retval[""] = false;
            return retval;
        }

        private Dictionary<string, bool> AvailableOnRep(ScoutInfo scoutInfo)
        {
            string[] URIs = Settings.RepositoryURIs.Split('|');
            String[] path = scoutInfo.DllName.Split('.');
            Dictionary<string, bool> retval = new Dictionary<string, bool>();

            foreach (string uri in URIs)
            {
                //logger.Log("Checking " + scoutInfo.DllName + " v" + scoutInfo.Version+ "  availability on Rep: " + uri);
                string zipuri = uri;

                //by default platform should point to the Latest on the repository if a version of the binary isn't specified
                string binaryversion = "Latest"; 
                if (scoutInfo.DesiredVersion != null && scoutInfo.DesiredVersion != Constants.UnknownHomeOSUpdateVersionValue)
                {
                    binaryversion = scoutInfo.DesiredVersion;
                }

                foreach (string pathElement in path)
                    zipuri += "/" + pathElement;


                zipuri += '/' + binaryversion + '/' + scoutInfo.DllName + ".zip";

                if (UrlIsValid(zipuri))
                {
                    retval[zipuri] = true;
                    return retval;
                }
            }
            retval[""] = false;
            return retval;
        }


        private bool UrlIsValid(string url)
        {

            WebResponse response = null;

            bool valid = false;

            try
            {
                Uri siteUri = new Uri(url);
                WebRequest request = WebRequest.Create(siteUri);
                response = request.GetResponse();

                if (response is HttpWebResponse)
                {
                    var httpResponse = (HttpWebResponse)response;

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        valid = true;
                    }
                    else
                    {
                        logger.Log("URI: " + url + " is invalid. Http Response code: " + httpResponse.StatusCode);
                    }
                }
                else if (response is FileWebResponse)
                {
                    //the fact that we made it here indicates that the file exists
                    valid = true;
                }
                else if (response is FtpWebResponse)
                {
                    var ftpResponse = (FtpWebResponse)response;

                    //i am guessing that this is how you check for validity of a FTP response
                    if (ftpResponse.StatusCode == FtpStatusCode.CommandOK)
                    {
                        valid = true;
                    }
                    else
                    {
                        logger.Log("URI: " + url + " is invalid. Http Response code: " + ftpResponse.StatusCode);
                    }
                }
                else
                {
                    logger.Log("URI: " + url + " has unhandled type");
                }
            }
            catch (WebException)
            {
                logger.Log("URI: " + url + " is invalid. Got WebException.");
            }
            //exception that i don't understand
            catch (Exception e)
            {
                logger.Log("Exception in checking URI validity (" + url + "): " + e);
            }
            finally
            {
                if (response != null)
                    response.Close();
            }

            return valid;
        }


        private void UnpackZip(String zipPath, string extractPath)
        {
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
            }
            catch (Exception e)
            {
                logger.Log("Exception in unpacking: "+e);
            }
       
        }

        private void DownloadFile(String url, String dir, String zipname)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(url, dir+"\\"+zipname);
            }
            catch (Exception e)
            {
                logger.Log("Getting new addin zip exception: "+e);
            }


        }

        private void CreateAddInDirectory(String addinsDir , String name)
        {
            try
            {
                int numTries = 3;

                while (numTries > 0)
                {
                    try
                    {
                        String completePath = addinsDir;
                        if (!System.IO.Directory.Exists(completePath))
                            System.IO.Directory.CreateDirectory(completePath);

                        completePath += "\\" + name;
                        if (Directory.Exists(completePath))
                            Directory.Delete(completePath, true);

                        DirectoryInfo info = Directory.CreateDirectory(completePath);
                        System.Security.AccessControl.DirectorySecurity security = info.GetAccessControl();
                        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(Environment.UserName, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.InheritanceFlags.ContainerInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                        info.SetAccessControl(security);
                    }
                    //this exception occurs when Windows is still holding a lock on the relevant dll
                    catch (System.UnauthorizedAccessException e)
                    {
                        logger.Log("Got unauthorized exception while creating  directory {0}. Will try again in 10 seconds. NumTries left = {1}", addinsDir, numTries.ToString());
                        Thread.Sleep(10 * 1000);
                    }
                    catch (System.IO.DirectoryNotFoundException e)
                    {
                        logger.Log("Got directorynotfound exception while creating directory {0}. Will try again in 10 seconds. NumTries left = {1}", addinsDir, numTries.ToString());
                        Thread.Sleep(10 * 1000);
                    }

                    numTries--;
                }


            }
            catch (Exception e)
            {
                logger.Log("Directory exception: "+e);
            }
            
        }

        /*
        private void PackZip(string startPath, String zipPath)
        {
            try
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath);
            }
            catch (Exception e)
            {
                logger.Log("Exception in compressing " + startPath + ":" + e);
            }

        }
         */
     
        #endregion


        private VModule GetModule(string moduleFriendlyName)
        {
            lock (this)
            {        
                foreach (VModule module in runningModules.Keys)
                {
                    if (module.GetInfo().FriendlyName().Equals(moduleFriendlyName))
                    {
                        return module;
                    }
                }

                return null;
            }
        }

        internal string GetImageUrlFromModule(string moduleFriendlyName, string hint=null)
        {

            VModule module = GetModule(moduleFriendlyName);

            if (module == null)
                throw new Exception("Module with friendlyName " + moduleFriendlyName + " not found!");

            return module.GetImageUrl(hint);
        }

        internal string GetDescriptionFromModule(string moduleFriendlyName, string hint = null)
        {

            VModule module = GetModule(moduleFriendlyName);

            if (module == null)
                throw new Exception("Module with friendlyName " + moduleFriendlyName + " not found!");

            return module.GetDescription(hint);
        }

        public Tuple<bool,string> AddLiveIdUser(string userName, string parentGroup, string liveId, string liveIdUniqueUserToken)
        {
            var result = config.AddLiveIdUser(userName, parentGroup, liveId, liveIdUniqueUserToken);

            if (result.Item1 != null)
            {
                policyEngine.AddUser(result.Item1);
                return new Tuple<bool, string>(true, "");
            }
            else
            {
                return new Tuple<bool, string>(false, result.Item2);
            }
        }

        public Tuple<bool, string> RemoveLiveIdUser(string userName)
        {
            var result = config.RemoveLiveIdUser(userName);

            if (result.Item1 != null)
            {
                policyEngine.RemoveUser(result.Item1);
                return new Tuple<bool, string>(true, "");
            }
            else
            {
                return new Tuple<bool, string>(false, result.Item2);
            }
        }

        


    }
}

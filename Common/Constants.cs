using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common
{
    public class Constants
    {
        // .... where the platform binary is seating. this is used to locate other resources
        public static readonly string PlatformBinaryDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

        // ..... the base address at which we host all the services
        public const int InfoServicePort = 51430;
        public static readonly string InfoServiceAddress = "http://localhost:" + InfoServicePort;

        // .... the port where platform is discovered
        public const int PlatformDiscoveryPort = 51432;

        public const string PlatformDiscoveryQueryStr = "Are you HomeOS platform?";
        public const string PlatformDiscoveryResponseStr = "Yes, I am HomeOS platform.";

        //constants used in version management
        public const string UnknownHomeOSUpdateVersionValue = "0.0.0.0";
        public const string ConfigAppSettingKeyHomeOSUpdateVersion = "HomeOSUpdateVersion";

        // ..... where to find various resources
        public static string AddInRoot = System.IO.Path.GetFullPath(PlatformBinaryDir + "\\..\\Pipeline");
        public static string ScoutRoot = System.IO.Path.GetFullPath(PlatformBinaryDir + "\\..\\Scouts");
        public static string DashboardRoot = System.IO.Path.GetFullPath(PlatformBinaryDir + "\\DashboardWeb");

        // ..... what isolation level to run the modules in; this is a debugging feature if you are having trouble with the addin framework
        public static readonly ModuleIsolationModes ModuleIsolationLevel = ModuleIsolationModes.AppDomain;

        // ..... a dummy user that apps pretend is using them
        public static readonly UserInfo UserSystem = new UserInfo(0, "system", "system", DateTime.MinValue, DateTime.MaxValue, "");

        // .... timeout used for invoke calls between modules
        public static TimeSpan nominalTimeout = new TimeSpan(0, 0, 5);

        // .... special names for service endpoint; apps should never use these names
        public const string GuiServiceSuffixWebSec = "GuiWebSec";
        public const string GuiServiceSuffixWeb = "GuiWeb";
        public const string ScoutsSuffixWeb = "scouts";
        public const string OrphanedDeviceIndicator = "OrphanedDevice";

        // .... names of various local configuration files
        public const string SettingsFileName = "Settings.xml";
        public const string PrivateSettingsFileName = "PrivateSettings.xml";
        public const string UsersFileName = "Users.xml";
        public const string ModulesFileName = "Modules.xml";
        public static string LocationsFileName = "Locations.xml";
        public const string ServicesFileName = "Services.xml";
        public const string RulesFileName = "Rules.xml";
        public const string DevicesFileName = "Devices.xml";
        public const string ScoutsFileName = "Scouts.xml";
        // the default config version definition if none is found in the config directory; value is read from the file
        public static string[] DefaultConfigVersionDefinition = { ModulesFileName, ServicesFileName, ScoutsFileName, RulesFileName };


        // ..... names of files containing homestore information
        public const string RoleDbFileName = "RoleDb.xml";
        public const string ModuleDbFileName = "ModuleDb.xml";
        public const string DeviceDbFileName = "DeviceDb.xml";
        public const string ScoutDbFileName = "ScoutDb.xml";


        // ..... constants related to LiveId based authentication
        public static readonly string LiveIdappId = "000000004C0FF8E2";
        public static readonly string LiveIdappsecret = "yVADO63r9bEqhjClpMTOTUM11ko0Msoh";
        public static readonly string LiveIdsecurityAlgorithm = "wsignin1.0";
        public static readonly string LiveIdpolicyURL = "http://foo";// these URLs are embedded in the encrypted live ID token 
        public static readonly string LiveIdreturnURL = "http://foo";// but we are not using these right now
        public static readonly Uri LiveIdLogoutURL = new Uri("http://login.live.com/logout.srf?appid=" + LiveIdappId + "&appctx=<APPCTX>");

        // Access Levels related constants
        public static readonly string SystemLow = "systemlow";
        public static readonly string LiveId = "liveid";// do not need to add user for this access level, because users in users.xml fall into this level
        public static readonly string SystemHigh = "systemhigh";

        public static readonly Uri SystemLowTokenEndpoint = new Uri("http://DOMAIN/<HOMEID>/auth/token?user=" + SystemLow + "&appctx=<APPCTX>");
        public static readonly Uri LiveIdTokenEndpoint = new Uri("http://login.live.com/wlogin.srf?appid=" + LiveIdappId + "&alg=" + LiveIdsecurityAlgorithm + "&appctx=<APPCTX>");
        public static readonly Uri SystemHighTokenEndpoint = new Uri("http://DOMAIN/<HOMEID>/auth/token?user=" + SystemHigh + "&appctx=<APPCTX>");


        public static readonly Dictionary<string, int> PrivilegeLevels = new Dictionary<string, int> { { SystemLow, 1 }, { LiveId, 2 }, { SystemHigh, 3 } };
        public static readonly Dictionary<string, Uri> PrivilegeLevelTokenEndpoints = new Dictionary<string, Uri> { { SystemLow, SystemLowTokenEndpoint }, { LiveId, LiveIdTokenEndpoint }, { SystemHigh, SystemHighTokenEndpoint } };
        public static readonly Dictionary<string, int> PrivilegeLevelTokenExpiry /* in seconds */ = new Dictionary<string, int> { { SystemLow, 24 * 3600 }, { LiveId, 24 * 3600 }, { SystemHigh, 3600 } };
        // Level 1 = "systemlow" < Level 2 ="liveid" < Level 3 = "system high"
        // Corresponding to each level we need a user (in users.xml) each with his own password. 
        // LiveId users each have their own password, so those liveid-based per-user access levels are represented by just one level (level2 named 'liveid').

        public static readonly string TokenEncryptionSecret = "randomsalt";
        //***********

        // ... when bad things happen during invoke
        public const string OpDoesNotExistName = "OperationNotFound";
        public const string OpTimeOut = "OperationTimeOut";
        public const string OpNullResponse = "NullResponse";

        // ... suffix for hosting service host endpoints for ajax calls
        public const string AjaxSuffix = "/webapp";

        
    }

    public enum ResultCode
    {
        Success = 0,
        Timeout,
        Failure,
        PortNotFound,
        ModuleNotFound,
        InSufficientPrivilege,
        InvalidUser,
        ForbiddenAccess,
        Allow
    };

    public enum ModuleIsolationModes { None, AppDomain, Process, NoAddInAppDomain };

}

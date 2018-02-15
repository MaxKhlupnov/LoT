using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Views
{
    public interface VPlatform
    {

     

        /// <summary>
        /// Register a port to declare it open for business from other modules
        /// </summary>
        /// <param name="port">The port to register</param>
        /// <param name="owner">The module to which the port belongs</param>
        /// <returns></returns>
        int RegisterPort(VPort port, VModule owner);

        /// <summary>
        /// Deregister a port to declare it unavailable for use by other modules
        /// </summary>
        /// <param name="port">The port to deregisted</param>
        /// <param name="owner">The module to which the port belongs</param>
        /// <returns></returns>
        int DeregisterPort(VPort port, VModule owner);

        /// <summary>
        /// Issues a portinfo object
        /// </summary>
        /// <param name="moduleFacingName">The local name used by the owning module for this port</param>
        /// <param name="module">The owning module</param>
        /// <returns></returns>
        VPortInfo GetPortInfo(string moduleFacingName, VModule module);

        /// <summary>
        /// Set the roles that are being exported by a port
        /// </summary>
        /// <param name="portInfo">the portinfo object of the port</param>
        /// <param name="roles">the list of roles to bind</param>
        /// <param name="module">the module that own the port</param>
        void SetRoles(VPortInfo portInfo, IList<VRole> roles, VModule module);

        /// <summary>
        /// Returns all ports that are currently registered
        /// </summary>
        /// <returns>the list of ports</returns>
        IList<VPort> GetAllPorts();

        /// <summary>
        /// Issues a capability
        /// </summary>
        /// <param name="module">The module that is asking for the capability</param>
        /// <param name="targetPort">The port for which the capability is being requested</param>
        /// <param name="userName">The name of the user on behalf of which the capability is being requested</param>
        /// <param name="password">The password of the user</param>
        /// <returns>The issued capability</returns>
        VCapability GetCapability(VModule module, VPort targetPort, string userName, string password);

        /// <summary>
        /// Signals to the platform that a particular module is terminating
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        int ModuleFinished(VModule module);

        /// <summary>
        /// Checks if the given username and password belong to a valid user
        /// </summary>
        /// <returns></returns>
        bool IsValidUser(string username, string password);

        /// <summary>
        /// returns the value of a global paramname
        /// </summary>
        string GetConfSetting(string paramName);

        /// <summary>
        /// returns the value of a global private paramname
        /// </summary>
        string GetPrivateConfSetting(string paramName);

        /// <summary>
        /// returns the ip address of the device from its unique Id
        /// </summary>
        string GetDeviceIpAddress(string deviceId);
        
        void UpdateState(VModule module, VModuleState state);
        void CancelAllSubscriptions(VModule module , VPort controlPort, VCapability controlportcap);

        /// <summary>
        /// For a given module access from a given URL, checks 
        /// (i) whether or not the privilege level (e.g., systemlow, liveid, systemhigh) is valid
        /// (ii) if it is, checks if the user has access to the module. 
        /// domainOfAccess = the domain e.g. localhost or homeosgatekeeper from which the module is being accessed
        /// privilegeLevel = level of token received e.g. systemlow, systemhigh or liveid
        ///  userIdentifier = a string that identifies the user in addition to the privilege level (e.g. LiveId Unique User Token for privilegeLevel = liveid)
        /// </summary>
        /// <param name="accessedModule"></param>
        /// <param name="domainOfAccess"></param>
        /// <param name="privilegeLevel"></param>
        /// <param name="userIdentifier"></param>
        /// <returns></returns>
        int IsValidAccess(string accessedModule, string domainOfAccess, string privilegeLevel, string userIdentifier);

        /// <summary>
        /// whether it is safe for cloud services to go online and start writing data
        /// </summary>
        /// <returns></returns>
        bool SafeToGoOnline();
    }
}

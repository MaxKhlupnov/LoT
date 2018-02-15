using System.Collections.Generic;

namespace HomeOS.Hub.Common
{
    public sealed class PortInfo : HomeOS.Hub.Platform.Views.VPortInfo
    {
        /// <summary>
        /// The friendly name of this portinfo object
        /// </summary>
        private string friendlyName;

        /// <summary>
        /// the moduleinfo of the owning module
        /// </summary>
        private HomeOS.Hub.Platform.Views.VModuleInfo moduleInfo;

        /// <summary>
        /// the module-local name assigned to this port by the owning module
        /// </summary>
        private string moduleFacingName;

        /// <summary>
        /// the physical location of this port
        /// </summary>
        private HomeOS.Hub.Platform.Views.VLocation location;

        /// <summary>
        /// the set of roles bound to this port
        /// </summary>
        private IList<HomeOS.Hub.Platform.Views.VRole> roles;

        /// <summary>
        /// does this port represent a high-security device
        /// </summary>
        private bool isSecure = false;

        public PortInfo(string moduleFacingName, HomeOS.Hub.Platform.Views.VModuleInfo moduleInfo)
            : this(moduleInfo, moduleFacingName, null, null)
        {
        }

        public PortInfo(string moduleFacingName, HomeOS.Hub.Platform.Views.VModuleInfo moduleInfo, 
                        List<HomeOS.Hub.Platform.Views.VRole> roles)
            : this(moduleInfo, moduleFacingName, roles, null)
        {
        }

        public PortInfo(HomeOS.Hub.Platform.Views.VModuleInfo moduleInfo, string moduleFacingName, 
                        List<HomeOS.Hub.Platform.Views.VRole> roles, string friendlyName)
        {
            this.friendlyName = friendlyName;
            this.moduleInfo = moduleInfo;
            this.moduleFacingName = moduleFacingName;

            if (roles != null)
            {
                this.roles = roles;
            }
            else
            {
                this.roles = new List<HomeOS.Hub.Platform.Views.VRole>();
            }
        }

        public void SetFriendlyName(string name)
        {
            this.friendlyName = name;
        }

        public override string GetFriendlyName()
        {
            return this.friendlyName;
        }

        public override bool IsSecure()
        {
            return isSecure;
        }

        public void SetSecurity(bool security)
        {
            isSecure = security;
        }

        public override IList<HomeOS.Hub.Platform.Views.VRole> GetRoles()
        {
            List <HomeOS.Hub.Platform.Views.VRole> ret;
            lock(roles){
                ret = new List<HomeOS.Hub.Platform.Views.VRole>(this.roles);
            }
            return ret;
        }

        public override HomeOS.Hub.Platform.Views.VLocation GetLocation()
        {
            return this.location;
        }

        public void SetLocation(HomeOS.Hub.Platform.Views.VLocation location)
        {
            this.location = location;
        }

        public override string ModuleFacingName()
        {
            return moduleFacingName;
        }

        public override string ModuleFriendlyName()
        {
            return moduleInfo.FriendlyName();
        }

        public void SetRoles(IList<HomeOS.Hub.Platform.Views.VRole> roles)
        {
            this.roles = roles;
        }

        // Ratul
        public string GetDescription() {
            // TODO
            return "Device Description";
        }
    }
}

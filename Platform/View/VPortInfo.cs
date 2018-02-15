using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Views
{
    public abstract class VPortInfo: MarshalByRefObject
    {
        /// <summary>
        /// Gets the friendly name (must be unique across all ports)
        /// </summary>
        /// <returns></returns>
        public abstract string GetFriendlyName();

        /// <summary>
        /// Gets friendly name of the owning module
        /// </summary>
        /// <returns></returns>
        public abstract string ModuleFriendlyName();

        /// <summary>
        /// Gets the local name given by the owning module
        /// </summary>
        /// <returns></returns>
        public abstract string ModuleFacingName();
        
        /// <summary>
        /// Get the list of roles exported by the port
        /// </summary>
        /// <returns></returns>
        public abstract IList<VRole> GetRoles();

        /// <summary>
        /// Get the physical location of the port
        /// </summary>
        /// <returns></returns>
        public abstract VLocation GetLocation();

        /// <summary>
        /// Does the port represent a high security device
        /// </summary>
        /// <returns></returns>
        public abstract bool IsSecure();

        /// <summary>
        /// Checks for equality with another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj != null &&
                   obj is VPortInfo &&
                   this.Equals((VPortInfo)obj);
        }

        /// <summary>
        /// Checks for equality with other portinfo object. 
        /// The two are equal if the modulefacingname and modulefriendlynames are equal
        /// </summary>
        /// <param name="otherPort"></param>
        /// <returns></returns>
        public bool Equals(VPortInfo otherPort)
        {
            return ModuleFacingName().Equals(otherPort.ModuleFacingName()) && 
                   ModuleFriendlyName() == otherPort.ModuleFriendlyName();
        }

        public override int GetHashCode()
        {
            return ModuleFacingName().GetHashCode() ^ ModuleFriendlyName().GetHashCode();
        }
    }
}

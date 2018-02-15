using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;

namespace HomeOS.Hub.Platform.Views
{
    public abstract class VModuleInfo: MarshalByRefObject
    {
        /// <summary>
        /// Gets the friendlyname of the module (must be unique across the system)
        /// </summary>
        /// <returns></returns>
        public abstract string FriendlyName();

        /// <summary>
        /// The arguments to be pass when starting the module
        /// </summary>
        /// <returns></returns>
        public abstract string[] Args();

        /// <summary>
        /// the name of the application. this is the string that is displayed to the user.
        /// </summary>
        /// <returns></returns>
        public abstract string AppName();

        /// <summary>
        /// the directory of the binary
        /// </summary>
        /// <returns></returns>
        public abstract string BinaryDir();

        /// <summary>
        /// the name of the primary binary
        /// </summary>
        /// <returns></returns>
        public abstract string BinaryName();

        /// <summary>
        /// The working directory that the module should use for any persistent state
        /// </summary>
        /// <returns></returns>
        public abstract string WorkingDir();

        /// <summary>
        /// The base URL of a module 
        /// </summary>
        /// <returns></returns>
        public abstract string BaseURL();


        public override string ToString()
        {
            return FriendlyName();
        }

        public override bool Equals(object obj)
        {
            return (obj != null &&
                    obj is VModuleInfo &&
                    this.Equals((VModuleInfo)obj));
        }

        public bool Equals(VModuleInfo otherModuleInfo)
        {
            return FriendlyName().Equals(otherModuleInfo.FriendlyName());
        }

        public override int GetHashCode()
        {
            return FriendlyName().GetHashCode();
        }

    }
}

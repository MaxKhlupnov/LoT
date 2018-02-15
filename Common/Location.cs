using System.Collections.Generic;

namespace HomeOS.Hub.Common
{
    public sealed class Location : HomeOS.Hub.Platform.Views.VLocation
    {
        private string name;
        private int id;

        public HomeOS.Hub.Platform.Views.VLocation Parent { get; private set; }
        public List<Location> ChildLocations { get; private set; }
        public List<PortInfo> ChildPorts { get; private set; }

        public Location(string name)
            : this(-1, name)
        {
        }

        public Location(int ID, string name)
        {
            this.name = name;
            this.id = ID;
            ChildLocations = new List<Location>();
            ChildPorts = new List<PortInfo>();
        }

        public override int ID()
        {
            return this.id;
        }

        public override string Name()
        {
            return this.name;
        }

        public override string ToString()
        {
            return name;
        }

        public void SetParent(Location parent)
        {
            this.Parent = parent;
        }

        public void AddChildLocation(Location child)
        {
            ChildLocations.Add(child);
        }

        public void AddChildPort(PortInfo child)
        {
            ChildPorts.Add(child);
        }

        // ***
        public void RemoveChildPort(PortInfo child)
        {
            ChildPorts.Remove(child);
        }
        //***


        /// <summary>
        /// Does this location contain this port?
        /// </summary>
        public bool ContainsPort(HomeOS.Hub.Platform.Views.VPortInfo portInfo)
        {
            foreach (var childPort in ChildPorts)
            {
                if (childPort.Equals(portInfo))
                    return true;
            }

            foreach (var childLocation in ChildLocations)
            {
                if (childLocation.ContainsPort(portInfo))
                    return true;
            }

            return false;
        }

    }
}
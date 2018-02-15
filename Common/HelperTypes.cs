
namespace HomeOS.Hub.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Xml;

    public enum AccessMode { All, Allow, Ask, Notify };

    public class DefaultUserGroups
    {
        public const string Everyone = "everyone";
        public const string Residents = "residents";
        public const string Guests = "guests";
    }

    public class HomeStoreApp
    {
        public string AppName { get; set; }
        public string BinaryName { get; set; }
        public string Description { get; set; }
        public int Rating { get; set; }
        public string Version { get; set; }
        public string IconUrl { get; set; }
        public Manifest Manifest { get; set; }
        public bool CompatibleWithHome { get; set; }
        public string MissingRolesString { get; set; }
        public List<List<HomeOS.Hub.Platform.Views.VRole>> MissingRoles { get; set; }

        // Ratul
        public string CompatibleDevices
        {
            get
            {
                // TODO:
                return "...";
            }
        }

        public override string ToString()
        {
            return AppName;
        }
    }

    public class HomeStoreDevice
    {
        //for the user to see
        public string DeviceName { get; set; }
        public string ManufacturerName { get; set; }
        public int Rating { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }

        //for internal use
        public string Model { get; set; }
        public List<String> ValidDrivers { get; set; }
        public List<String> Roles;

        public override string ToString()
        {
            return DeviceName;
        }
    }

    public class HomeStoreScout
    {
        public string Name { get; set; }
        public string DllName { get; set; }
        public string Description { get; set; }
        public int Rating { get; set; }
        public string Version { get; set; }
        public string IconUrl { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class AccessRule
    {
        public string RuleName { get; set; }
        public string ModuleName { get; set; }
        public string UserGroup { get; set; }
        public AccessMode AccessMode { get; set; }
        public int Priority { get; set; }
        public List<string> DeviceList { get; set; }
        public List<TimeOfWeek> TimeList { get; set; }
    }

    public class TimeOfWeek
    {
        public int DayOfWeek { get; private set; }
        public int StartMins { get; private set; }
        public int EndMins { get; private set; }

        public TimeOfWeek(int day, int start, int end)
        {
            this.DayOfWeek = day;
            this.StartMins = start;
            this.EndMins = end;
        }

        public bool Valid()
        {
            return (-1 <= DayOfWeek && DayOfWeek <= 6 &&
                     0 <= StartMins && StartMins <= 2400 &&
                     0 <= EndMins && EndMins <= 2400 &&
                     StartMins <= EndMins);
        }

        public bool Overlaps(TimeOfWeek other)
        {
            //neither is -1 and the two aren't equal, so there can be no overlap
            if (this.DayOfWeek != -1 &&
                other.DayOfWeek != -1 &&
                this.DayOfWeek != other.DayOfWeek)
            {
                return false;
            }

            //days of week overlap, so lets check for timeoverlap now
            if ((this.StartMins <= other.StartMins && this.EndMins >= other.StartMins) ||
                 (other.StartMins <= this.StartMins && other.EndMins >= this.EndMins))
                return true;

            return false;
        }

        public static TimeOfWeek Parse(string time)
        {
            if (time == null)
            {
                return new TimeOfWeek(-1, 0, 2400);
            }
            else
            {
                throw new NotImplementedException("sophisticated timeofweek parsing not implemented yet. got " + time);
            }
        }
    }
}

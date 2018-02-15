using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using HomeOS.Hub.Platform.Views;
using HomeOS.Hub.Platform.DeviceScout;
using HomeOS.Hub.Common;

namespace HomeOS.Hub.Scouts.DigitalMedia
{
    public class DigitalMediaConfiguration

    {
        /// <summary>
        /// Modules that are currently running
        /// </summary>
        List<DigitalMediaPanelDescription> dmConnections = new List<DigitalMediaPanelDescription>();
        public DigitalMediaConfiguration(string baseDir, ScoutViewOfPlatform platform, VLogger logger)
         {
     
             //TODO: Read XML configuratoin file

             DigitalMediaPanelDescription myDummyPannel =  new DigitalMediaPanelDescription(IPID: "77", IPAddress: "10.101.50.61",
                 IPPort: 41794, UserName: "", Password: "", UseSSL: false );
           /*  DigitalMediaSignalDescription myDummySignal = new DigitalMediaSignalDescription(Name:"Mydigitalsignal1",

                 Driver: "HomeOS.Hub.Drivers.DigitalMedia", Join: "0", Slot: "1");
             myDummyPannel.AddSignalDescription(myDummySignal);*/


             dmConnections.Add(myDummyPannel);

         }

        public IEnumerable<DigitalMediaPanelDescription> GetPanelDescriptions
        {
            get
            {
                return this.dmConnections;
            }
        }
    }

    public class DigitalMediaPanelDescription
    {
        public int IPID {get; set;}
        public string IPAddress {get; set;}

        public int IPPort {get; set;}
        
        public string UserName {get; set;}

        public string Password {get; set;}
        
        public bool UseSSL {get; set;}

    //    List<DigitalMediaSignalDescription> dmSignals = new List<DigitalMediaSignalDescription>();


        internal DigitalMediaPanelDescription(string IPID, string IPAddress, int IPPort, string UserName, string Password, bool UseSSL)
        {

            this.IPID = Convert.ToInt32(IPID, 16);
            this.IPAddress = IPAddress;
            this.IPPort = IPPort;
            this.UserName = UserName;
            this.Password = Password;
            this.UseSSL = UseSSL;
        }

 /*       public void AddSignalDescription(DigitalMediaSignalDescription signalDescription){
            dmSignals.Add(signalDescription);
        }

        public IEnumerable<DigitalMediaSignalDescription> GetSignalDescriptions{
            get{
                return this.dmSignals;
            }
        }*/
    }

/*
       public class DigitalMediaSignalDescription
    {
        public string Name {get; set;}
        public string Driver {get; set;}
        
        public string Join {get; set;}

        public string Slot {get; set;}

        internal  DigitalMediaSignalDescription(string Name, string Driver, string Join, string Slot){

              this.Name = Name;
              this.Driver = Driver;
              this.Join = Join;
              this.Slot = Slot;
          } 

    }*/
}

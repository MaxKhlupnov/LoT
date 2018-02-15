using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.BluetoothApp.Azure {
    abstract class LoTDevice {
        public enum RequestCode {
            ALL = 110,
            TEMPERATURE = 111,
            ACCELEROMETER = 112,
            MAGNETOMETER = 113,
            LIGHT = 114,
            PRESSURE = 115,
            LOCATION = 116,
            PROXIMITY = 117,
            GYROSCOPE = 118
        };

        public int ID { get; set; }
        public string MAC { get; set; }
        
        public LoTDevice() { }

        public LoTDevice(string MAC) {
            this.MAC = MAC;
        }

        public abstract void parseMessage(string message);
    }
}

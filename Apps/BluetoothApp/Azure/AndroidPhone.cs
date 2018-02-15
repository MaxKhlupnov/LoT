using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.BluetoothApp.Azure {
    class AndroidPhone : LoTDevice {
        public string Accelerometer { get; set; }
        public string Location { get; set; }
        public float Pressure { get; set; }
        public float Light { get; set; }
        public float Proximity { get; set; }
        public string Magnetometer { get; set; }
        public string Gyroscope { get; set; }

        public AndroidPhone() { }

        public AndroidPhone(string mac, string accelerometer, string location, float pressure, float light, float proximity, string magnetometer, string gyroscope) {
            this.MAC = mac;
            this.Accelerometer = accelerometer;
            this.Location = location;
            this.Pressure = pressure;
            this.Light = light;
            this.Proximity = proximity;
            this.Magnetometer = magnetometer;
            this.Gyroscope = gyroscope;
        }

        override
        public void parseMessage(string message) {
            message = message.Substring(1, message.Length - 2);
            string[] splitMessage = message.Split(';');
            RequestCode requestCode = (RequestCode)Int32.Parse(splitMessage[1]);
            switch (requestCode) {
                case RequestCode.ALL:
                    this.Accelerometer = splitMessage[2];
                    this.Magnetometer = splitMessage[3];
                    this.Gyroscope = splitMessage[4];
                    this.Light = float.Parse(splitMessage[5]);
                    this.Pressure = float.Parse(splitMessage[6]);
                    this.Location = splitMessage[7];
                    this.Proximity = float.Parse(splitMessage[8]);
                    break;
                case RequestCode.ACCELEROMETER:
                    this.Accelerometer = splitMessage[2];
                    break;
                case RequestCode.GYROSCOPE:
                    this.Gyroscope = splitMessage[2];
                    break;
                case RequestCode.LIGHT:
                    this.Light = float.Parse(splitMessage[2]);
                    break;
                case RequestCode.LOCATION:
                    this.Location = splitMessage[2];
                    break;
                case RequestCode.MAGNETOMETER:
                    this.Magnetometer = splitMessage[2];
                    break;
                case RequestCode.PRESSURE:
                    this.Pressure = float.Parse(splitMessage[2]);
                    break;
                case RequestCode.PROXIMITY:
                    this.Proximity = float.Parse(splitMessage[2]);
                    break;
            }
            //{1;110;175.82813,1.5625,2.1875;-6.8125,-60.125,-52.375;239.0;0.0;100.0;0;0}
        }
    }
}

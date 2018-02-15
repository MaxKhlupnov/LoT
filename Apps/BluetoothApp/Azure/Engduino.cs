using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.BluetoothApp.Azure {
    class Engduino : LoTDevice {

        public float Temperature { get; set; }
        public int Light { get; set; }
        public string Magnetometer { get; set; }
        public string Accelerometer { get; set; }

        public Engduino() {}

        public Engduino(string mac, float temperature, int light, string magnetometer, string accelerometer) {
            this.MAC = mac;
            this.Temperature = temperature;
            this.Light = light;
            this.Magnetometer = magnetometer;
            this.Accelerometer = accelerometer;
        }

        override
        public void parseMessage(string message) {
            message = message.Substring(1, message.Length - 2);
            string[] splitMessage = message.Split(';');
            RequestCode requestCode = (RequestCode)Int32.Parse(splitMessage[1]);
            switch (requestCode) {
                case RequestCode.ALL:
                    this.Temperature = float.Parse(splitMessage[2]);
                    this.Accelerometer = splitMessage[3];
                    this.Magnetometer = splitMessage[4];
                    this.Light = int.Parse(splitMessage[5]);
                    break;
                case RequestCode.TEMPERATURE:
                    this.Temperature = float.Parse(splitMessage[2]);
                    break;
                case RequestCode.ACCELEROMETER:
                    this.Accelerometer = splitMessage[2];
                    break;
                case RequestCode.LIGHT:
                    this.Light = int.Parse(splitMessage[2]);
                    break;
                case RequestCode.MAGNETOMETER:
                    this.Magnetometer = splitMessage[2];
                    break;
            }
        }
    }
}

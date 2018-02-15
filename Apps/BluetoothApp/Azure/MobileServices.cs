using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.BluetoothApp.Azure {
    class MobileServices {
        public static MobileServiceClient MobileService = new MobileServiceClient(
            "<Your mobile service URL>",
            "<Your mobile service security code>"
            );

        public async Task WriteDevice(LoTDevice device) {
            if (device is Engduino) {
                try {
                    await MobileService.GetTable<Engduino>().InsertAsync((Engduino)device);
                } catch (HttpRequestException e) {
                    //cache it for later retry
                    throw;
                }
            } else if (device is AndroidPhone) {
                try {
                    await MobileService.GetTable<AndroidPhone>().InsertAsync((AndroidPhone)device);
                } catch (HttpRequestException e) {
                    //cache it for later retry
                    throw;
                }
            }
        }
    }
}

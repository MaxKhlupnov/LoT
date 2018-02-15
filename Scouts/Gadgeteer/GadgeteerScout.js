jQuery.support.cors = true;

//For parameters passed in on URL
var DEVICEID = "";  


//This file sets up gadgeteer devices. 
//  There are three posibilities  
//      a) device on setup network
//      b) device already on home wifi
//     c) device on usb  (in which case scout should have already automatically sent wifi creds)
//
//   First check IsDeviceOnHostedNetwork 
//   If so, show the UI for typing in key -> send key -> then check if on HomeWifi
//   If not, check if it is on HomeWifi
//
//   If on home wifi -> go to Final setup setup
//
//  In an ideal world (c) device on usb becomes (a) and we find it on wifi.


//Expect URL to be called with DeviceID
$(document).ready(
    function () {
        var qs = getQueryStringArray();
        if (qs.DeviceId !== 'undefined' && qs.DeviceId) {
            DEVICEID = qs.DeviceId;
            UpdateDebugInfo(this, "Device name " + DEVICEID);
            IsDeviceOnHostedNetwork();  //check first if it's on the setup network
        }
        else {
            UpdateDebugInfo(this, "Could not extract DeviceID URL " + window.location);
        }
    }
);

function IsDeviceOnHostedNetwork() {

    var url2 = "webapp/IsDeviceOnHostedNetwork";
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '"}';
    new PlatformServiceHelper().MakeServiceCall(url2, data2, IsDeviceOnHostedNetworkCallback);
   
}

function IsDeviceOnHostedNetworkCallback(context, result) {
    if (result[0] == "") {
       
        if (result[1] == "true") { //on the setup network
            SetupNetworkDeviceSetup();
        }
        else {
  
            CheckDeviceOnHomeWifi();  //hopefully device is on home wifi       
        }
      
    }
    else {
        UpdateDebugInfo(this, "IsDeviceOnHostedNetwork:" + result[0]);   
    }


}

function SetupNetworkDeviceSetup() {
    $("#pageTitle").html("Send wireless credentials to device");
    $("#homeWirelessQuestion").hide();
    //on setup network show secret key input option
    $("#secretKeyInput").show();
}

function SendKey() {
    var key = $('#keyText').val();
    UpdateDebugInfo(this, "Key:" + key);
    var url2 = "webapp/SendWifiCredentials";
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","authCode": "' + key + '"}';
    new PlatformServiceHelper().MakeServiceCall(url2, data2, SendKeyCallback);
}

function SendKeyCallback(context, result) {   

    if (result[0] == "") {
        $("#secretKeyInput").hide();
        CheckDeviceOnHomeWifi();
    }
    else {
        UpdateDebugInfo(this, "SendKeyCallback:" + result[0]);
    }
}


function CheckDeviceOnHomeWifi() {
    var url2 = "webapp/IsDeviceOnHomeWifi";
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '"}';
    new PlatformServiceHelper().MakeServiceCall(url2, data2, IsDeviceOnHomeNetworkCallback);
}

function IsDeviceOnHomeNetworkCallback(context, result) {
    if (result[0] == "") {

        if (result[1] == "true") { //on the wifi network
            GoToFinalSetup(DEVICEID);
        }
        else {
            //Might want to wait and retry here until we find it on home network 
            $("#pageTitle").html("Waiting for device to join home network");
            $("#homeWirelessQuestion").show();
            UpdateDebugInfo(this, "IsDeviceOnHomeNetworkCallback" + result[1]);
        }
    }
    else {
        UpdateDebugInfo(this, "IsDeviceOnHomeNetwork:" + result[0]);
    }

}


function GoNext() {
    GoToFinalSetup(DEVICEID);
}

jQuery.support.cors = true;

//For parameters passed in on URL
var DEVICEID = "";  //debug if this still works if no homeid is set
var CAMERA_USER_NAME = "root";
var CAMERA_PWD = "";


//Expect URL to be called with DeviceID parameters, 
$(document).ready(
    function () {
        var qs = getQueryStringArray();
        if (qs.DeviceId !== 'undefined' && qs.DeviceId) {
            DEVICEID = qs.DeviceId;
            UpdateDebugInfo(this, "Device name " + DEVICEID);
            CheckCameraCreds(CAMERA_USER_NAME, CAMERA_PWD);
        }
        else {
            UpdateDebugInfo(this, "Could not extract DeviceID URL " + window.location);
        }
    }
);

function CheckCameraCreds(uName, pword) {

    CAMERA_USER_NAME = uName;
    CAMERA_PWD = pword;
    $("#cameraCreds").hide();  //may need to be hidden
    updateInformationText("Verifying access to camera");
    var url2 = "webapp/AreCameraCredentialsValid";
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","username": "' + CAMERA_USER_NAME + '","password": "' + CAMERA_PWD + '"}';

    new PlatformServiceHelper().MakeServiceCall(url2, data2, CheckCameraCredsCallback);

}

function CheckCameraCredsCallback(context, result) {

    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        UpdateDebugInfo(context, "Creds checked");  

        if (result[1] == "true") {

            var url2 = "webapp/SetCameraCredentials";
            var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","username": "' + CAMERA_USER_NAME + '","password": "' + CAMERA_PWD + '"}';
            new PlatformServiceHelper().MakeServiceCall(url2, data2, SettingCameraCredsCallback);
        }
        else {
            $("#cameraCreds").show();
        }
        
    }
    else {
        
        //retry button
        $("#retryButton").show();
        updateInformationText(result[0]);
        //show cancel button
        $("#cButton").show();
    }

}

function SettingCameraCredsCallback(context, result) {

    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        UpdateDebugInfo(context, "Set creds");

        GoToFinalSetup(DEVICEID);
        
        //updateInformationText("Checking for device on network");
        //CallDeviceRelatedFunctions("IsDeviceOnWifi", IsDeviceOnWifiCallback);
    }
    else {
        ShowDebugInfo();
        UpdateDebugInfo(this, "Setting Creds Callback:" + result[0]);
        //retry button
        $("#retryButton").show();
    }

}


////Helper function to call web functions with only uniqueDeviceId paramters
//function CallDeviceRelatedFunctions(functionName, callbackname) {
//    var url2 = "webapp/" + functionName;
//    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '"}';
//    new PlatformServiceHelper().MakeServiceCall(url2, data2, callbackname);
//}


//function IsDeviceOnWifiCallback(context, result) {
//    UpdateDebugInfo(context, result);
//    if (result[0] == "") {
//        UpdateDebugInfo(context, "Device call on wireless" + result);
//        if (result[1] == "true") {
//            GoToFinalSetup(DEVICEID);
//        }
//        else {
//            updateInformationText("Found wired device");
//            $("#wirelessQuestion").show(); //no device on wireless, ask what type of device it is
//        }
        
//    }
//    else {
//        UpdateDebugInfo(this, "IsDeviceOnWireless Callback:" + result[0]);
//    }

//}


function updateInformationText(newText) {
    clearInformationText();
    $("#divInformationText").html("<p>" + newText + "</p>");
}

function clearInformationText() {
    $("#divInformationText").html("");
}

function WirelessCameraSetup() {
    //Send Wifi-credentials
    $("#wirelessQuestion").hide();
    $("#retryButton2").hide();
    updateInformationText("Sending wireless credentials to camera");
    CallDeviceRelatedFunctions("SendWifiCredentials", SendWifiCredsCallback);
}

function SendWifiCredsCallback(context, result) {
    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        //show instructions
        updateInformationText("Wireless credentials have been sent, getting additional instructions");
        new PlatformServiceHelper().MakeServiceCall("webapp/GetInstructions", "", GetInstructionsCallback);
    }
    else {
        //DisplayDebugging(this, "SendWifiCredsCallback:" + result[0]);
        updateInformationText(result[0]);
        //show cancel button
        $("#cButton").show();
        //show the retry button
        $("#retryButton2").show();
        //$("#divDebugInfo").show();
      
    }

}

function GetInstructionsCallback(context, result) {
    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        updateInformationText(result[1]);
        $("#goButton").show();
    }

    else {
        UpdateDebugInfo(this, "GetInstructionsCallback:" + result[0]);

    }

}

//Wired cameras don't need any network passwords so we go straight to final setup
function WiredCameraSetup() {
    //go to the final setup
    $("#wirelessQuestion").hide();
    GoToFinalSetup(DEVICEID);
}

function RetryButton() {
    $("#retryButton").hide();
    CheckCameraCreds(CAMERA_USER_NAME, CAMERA_PWD);
}

function RetrySendWifiButton() {
    $("#retryButton2").hide();
    WirelessCameraSetup();
}

//Wireless camera should now be on wireless, look for it
function GoButton() {
    CallDeviceRelatedFunctions("IsDeviceOnWifi", IsDeviceOnWifiCallback);
}

function TryCreds() {
    var userName = $('#userNameCamera').val();
    CAMERA_USER_NAME = userName;
    var pwd = $('#pwdCamera').val();
    CAMERA_PWD = pwd;
    CheckCameraCreds(CAMERA_USER_NAME, CAMERA_PWD);
}
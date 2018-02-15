jQuery.support.cors = true;

//For parameters passed in on URL
var DEVICEID = "";  //debug if this still works if no homeid is set
var API_USER_NAME = "homeosuser";

//Expect URL to be called with DeviceID parameters, 
$(document).ready(
    function () {
        var qs = getQueryStringArray();
        if (qs.DeviceId !== 'undefined' && qs.DeviceId) {
            DEVICEID = qs.DeviceId;
            UpdateDebugInfo(this, "Device name " + DEVICEID);
        }
        else {
            UpdateDebugInfo(this, "Could not extract DeviceID URL " + window.location);
        }
    }
);

function SetAPIUsername() {
    $("#hueInstructions").hide();  
    updateInformationText("Setting API access to the bridge");
    var url2 = "webapp/SetAPIUsername";
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","username": "' + API_USER_NAME + '"}';

    new PlatformServiceHelper().MakeServiceCall(url2, data2, SetAPIUsernameCallback);
}

function SetAPIUsernameCallback(context, result) {

    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        UpdateDebugInfo(context, "API username set");  

        // Just need to start the driver for hue
        updateInformationText("Starting Driver");
        new PlatformServiceHelper().MakeServiceCall("../../GuiWeb/webapp/StartDriver", '{"uniqueDeviceId": "' + DEVICEID + '"}', StartDriverCallback);
        
    }
    else {
        $("#retryButton").show();
        updateInformationText(result[0]);
        $("#cButton").show();
    }

}

function StartDriverCallback(context, result) {
    if (result[0] == "") {
        updateInformationText("Hue bridge connected, go to add devices to add lights");
        $("#dButton").show();
    }
    else {

        $("#retryButton").show();
        updateInformationText(result[0]);
        $("#cButton").show();
    }


}

function updateInformationText(newText) {
    clearInformationText();
    $("#divInformationText").html("<p>" + newText + "</p>");
}

function clearInformationText() {
    $("#divInformationText").html("");
}


function RetryButton() {
    $("#retryButton").hide();
    SetAPIUsername();
}

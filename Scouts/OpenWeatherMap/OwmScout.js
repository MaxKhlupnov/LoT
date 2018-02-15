jQuery.support.cors = true;

//For parameters passed in on URL
var DEVICEID = "";  //debug if this still works if no homeid is set

//Expect URL to be called with DeviceID parameters, 
$(document).ready(
    function () {
        var qs = getQueryStringArray();
        if (qs.DeviceId !== 'undefined' && qs.DeviceId) {
            DEVICEID = qs.DeviceId;
            UpdateDebugInfo(this, "Device name " + DEVICEID);

            GetAppId();
        }
        else {
            UpdateDebugInfo(this, "Could not extract DeviceID URL " + window.location);
        }
    }
);

function SetAppId() {
    updateInformationText("Setting AppId");
    var url2 = "webapp/SetAppId";
    var appId = $('#appIdTextbox').val();
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","appId": "' + appId + '"}';

    new PlatformServiceHelper().MakeServiceCall(url2, data2, SetAppIdCallback);
}

function SetAppIdCallback(context, result) {

    UpdateDebugInfo(context, result);

    if (result[0] == "") {
        UpdateDebugInfo(context, "AppId set");
        updateInformationText("AppId set");
        SetLocation();
    }
    else {
        updateInformationText(result[0]);
    }
}

function GetAppId() {
    updateInformationText("Getting AppId");
    var url2 = "webapp/GetAppId";
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '"}';

    new PlatformServiceHelper().MakeServiceCall(url2, data2, GetAppIdCallback);
}

function GetAppIdCallback(context, result) {

    UpdateDebugInfo(context, result);

    if (result[0] == "") {
        UpdateDebugInfo(context, "AppId got");
        clearInformationText();

        $('#appIdTextbox').val(result[1]);
    }
    else {
        updateInformationText(result[0]);
    }
}

function QueryLocation() {
    updateInformationText("Querying location");
    var url2 = "webapp/QueryLocation";
    var location = $('#locationHintTextbox').val();
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","locationHint": "' + location + '"}';

    new PlatformServiceHelper().MakeServiceCall(url2, data2, QueryLocationCallback);
}

function QueryLocationCallback(context, result) {

    UpdateDebugInfo(context, result);

    if (result[0] == "") {
        UpdateDebugInfo(context, "QueryLocation complete");

        //did we get anything?
        if (result.length < 2)
        {
            updateInformationText("No valid location found. Try again");
            return;
        }

        clearInformationText();

        $("#locationList").empty();

        //Add the resulting list to the cameraList 
        var selected = '<option value="o1" selected ="selected">';
        var normal = '<option value="o1">';
        var end = "</option>";

        //first one is selected
        //$("#locationList").append(selected + result[1] + end);
        $("#locationList").append(normal + result[1] + end);

        for (j = 2; j < result.length; j = j + 1)
        {
            $("#locationList").append(normal + result[j] + end);
        }

        $("#cameraList").trigger("chosen:updated");

        $("#locationListDiv").show();
    }
    else {
        updateInformationText(result[0]);
        $("#cButton").show();
    }
}

function SetLocation() {
    updateInformationText("Setting location");
    var url2 = "webapp/SetLocation";

    var location = $("#locationList :selected").text();

    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","location": "' + location + '"}';

    new PlatformServiceHelper().MakeServiceCall(url2, data2, SetLocationCallback);
}


function SetLocationCallback(context, result) {

    UpdateDebugInfo(context, result);

    if (result[0] == "") {
        UpdateDebugInfo(context, "SetLocation complete");
        GoToFinalSetup(DEVICEID);
    }
    else {
        updateInformationText(result[0]);
        $("#cButton").show();
    }
}

function Go() {
    //we'll call SetAppId, which will call SetLocation, which will call GoToFinalSetup
    SetAppId();
}


function updateInformationText(newText) {
    clearInformationText();
    $("#divInformationText").html("<p>" + newText + "</p>");
}

function clearInformationText() {
    $("#divInformationText").html("");
}

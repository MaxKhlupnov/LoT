jQuery.support.cors = true;

//Global variables;
var HOMEID;
var HOMEPWD;
var URLTOGOTO;  //need more elegant way of handling callback


$(document).ready(
    function () {

        //1. Ask if IsConfigNeeded
        //2. If so, then show the HomeID/PWD, possibly homewireless stuff
        //3. If no config, set the title bar with the homeID, load the apps, 

        UpdateDebugInfo(this, "document loaded");
        new PlatformServiceHelper().MakeServiceCall("webapp/IsConfigNeededWeb", "", IsConfigNeededWebCallback);
    }
);

function IsConfigNeededWebCallback(context, result) {

    if (result[0] == "") {
        UpdateDebugInfo(context, result);
        if (result[1] == "False") {
            //Get the HomeID
            new PlatformServiceHelper().MakeServiceCall("webapp/GetConfSettingWeb", '{"confKey":"HomeId"}', GetHomeIDCallback);
        }
        else {
           
            window.location.href = encodeURI("HomeOSSetup1Wireless.html");
        }
    }
    else {
        UpdateDebugInfo(this, "IsConfigNeededWebCallback:" + result[0]);
    }

}

function GetHomeIDCallback(context, result) {

    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        HOMEID = result[1];
        //Check if our URL doesn't already include our home id
        var hrefToLower = window.location.href.toLowerCase();
        var HOMEID_toLower = HOMEID.toLowerCase();
        if (hrefToLower.indexOf(HOMEID_toLower) == -1) {
            //We aren't at HomeID url - go there now.
            var url = "../" + HOMEID + "/GuiWeb/index.html"
            GoToHTMLPage(url);
            return;
        }
        $("#homeIDDisplay").html(HOMEID);
       //figure out what apps are installed
       new PlatformServiceHelper().MakeServiceCall("webapp/GetInstalledAppsWeb", "", GetInstalledAppsWebCallback);
    }
    else {
        UpdateDebugInfo(this, "GetHomeIDCallback:" + result[0]);
    }
}
    

function GetInstalledAppsWebCallback(context, result) {

    //Add all the installed apps to a list
    //Example of link http://localhost:51430/Brush/SmartCam/index.html
  

  //Convention is that first result should be empty if success.
    if (result[0] == "") {

        if (result.length == 1) {
           // $("#InstalledApps").html("No applications currently installed");
            return;
        }
     
        for (i = 1; i + 3 < result.length; i = i + 4) {
            //i is appName, i+1 is description i+2 is image file, i+3 is version
            var url = buildAppURL(result[i]);
            var iconURL = buildIconURL(result[i]);
            //AJB: TODO doing show image if we have one
            // $("#appsInstalled").append('<div class="media_block col"><a href="' + url + '">' + result[i] + "</a> " + result[i + 1] + "</div>");
           //Have to make sure images are loaded <img class="app_image" src="' + iconURL + '" />
            $("#appsInstalled").append('<div class="media_block col" onclick=CheckServiceAndThenGoToPage("' + url + '")><a>' + result[i] + "</a> <div class='app_desc'>" + result[i + 1] + '</div></div>');
        }
    }
    else {
        UpdateDebugInfo(context, result);
    }


}

function getFullURL(relurl) {

    //relurl has this format: "../"+ aName  + "/index.html"; 
    var appURL = relurl.substring(3);  //get rid of "../"
    var hrefToLower = window.location.href.toLowerCase();
    var endIdx = hrefToLower.indexOf("guiweb");

    var fullURL = "";
   
    if (endIdx != -1) { 
        fullURL = window.location.href.substring(0, endIdx) + appURL;
    }
    return fullURL;
}

function CheckServiceAndThenGoToPage(url) {
    //check service is ready - then go to page 
    //otherwise load alert, wait for a second and then go
  //  List<string> IsServiceReady(string absoluteUrl);
   
    var fullURL = getFullURL(url);
    URLTOGOTO = fullURL; //setup for callback, need more elegant way of doing this
    var data2 = '{"absoluteUrl": "' + fullURL + '"}';
    new PlatformServiceHelper().MakeServiceCall("webapp/IsServiceReady", data2, IsServiceReadyCallback);
    
}

function IsServiceReadyCallback(context, result) {

    if (result[0] == "") {
        if (result[1] == "true") {
            $("#status").hide();
            GoToHTMLPage(URLTOGOTO);
        }
        else {
            $("#status").show();
            $("#status").html("Service not ready, retrying");
            setTimeout(function () {
                var data2 = '{"absoluteUrl": "' + URLTOGOTO + '"}';
                new PlatformServiceHelper().MakeServiceCall("webapp/IsServiceReady", data2, IsServiceReadyCallback);
            }, 1000 /* milliseconds – this is the delay until your function gets called */);
           
        }
    }
    else {
        ShowDebugInfo();
        UpdateDebugInfo(context, result[0]);
    }

}

function buildAppURL(aName) {

   var appUrl = "../"+ aName  + "/index.html";
    return encodeURI(appUrl);  //encodeURI in case there are spaces or other weirdness in deviceName
    
}

function buildIconURL(aName) {
    
    var appUrl = "../"+ aName  + "/icon.png";
    return encodeURI(appUrl);  

}


    function SetHomeID() {
        HOMEID = $('#homeID').val();
        HOMEPWD = $('#homePWD').val();
    
        var data = '{"homeId": "' + HOMEID + '","password": "' + HOMEPWD + '"}';
        new PlatformServiceHelper().MakeServiceCall("webapp/SetHomeIdWeb", data, SetHomeIDCallback);
    }

    function SetHomeIDCallback(context, result) {

        if (result[0] == "") {

            UpdateDebugInfo(context, result);
            //hide home id question show normal stuff and start from the "get homeID" logic
            $("#divHomeID").hide();
            $("#NormalView").show();
            new PlatformServiceHelper().MakeServiceCall("webapp/GetConfSettingWeb", '{"confKey":"HomeId"}', GetHomeIDCallback);
        }
        else {
            UpdateDebugInfo(context, result[0]);
        }

    }




    function GetVersion(userid, password) {
  
    }

    function GoToAddUnconfiguredDevice(scoutName) {
        //call format: AddDeviceUnconfiguredDevicesForScout.html?ScoutName=HomeOS.Hub.Scouts.WebCam or empty string for all scouts
        var url = "AddDeviceUnconfiguredDevicesForScout.html?ScoutName=" + scoutName;
        UpdateDebugInfo(this, url);
        window.location.href = encodeURI(url);
    }







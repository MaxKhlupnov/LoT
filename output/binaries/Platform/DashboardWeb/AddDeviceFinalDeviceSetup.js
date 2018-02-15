jQuery.support.cors = true;

//Global variables
var DEVICEID = ""; //For parameters passed in on URL
var DEVICETYPE = "";
var DEVICE_IMAGE_PATH = "";
var APPS_USER_PERMITS = []; //for the already installed apps the user permits to use this device
var MAX_ATTEMPTS = 5; //maximum times to call to see if device is ready
var CURR_ATTEMPT = 1;
var LOCATION_OPTION_HIDDEN = true;
var APPS_TO_INSTALL = []; //the apps that should be installed
var APPS_TO_INSTALL_COUNTER = 0;  //we are going to walk backwards through array of apps to install at -1 we get to call configure

//This file deals with final setup for a device
//-          1. call StartDriver()  
//           2. Call GetLocations (gives driver time to start :-))
//           3. IsDeviceReady 
//           4. GetDeviceDetails
//-          5. call GetCompatibleAppsNotInstalledWeb and 
//           6. GetCompatibleAppsInstalledWeb to setup page
//           After the user clicks done
//-          Then call InstallAppWeb if needed
//-          Then call ConfigureDeviceWeb

//Expect URL to be called with DeviceId parameter
//e.g. http://localhost:51430/GuiWeb/AddDeviceUnconfiguredDevicesForScout.html?DeviceId=Camera
//and optionally with the Orphan parameter.  If Orphan=1 then this is a orphaned device and we should not start driver

$(document).ready(
    function () {
        var qs = getQueryStringArray();
        if (qs.DeviceId != undefined && qs.DeviceId) {
            DEVICEID = qs.DeviceId;
 
            var isZwave = IsZwaveDevice(DEVICEID);

         //if ZWAVE OR Orphaned skip starting the driver
            if (isZwave || (qs.Orphan != undefined && qs.Orphan == "1"))
                GetAllLocations(GetAllLocationsCallback);
            else 
                CallDeviceRelatedFunctions("StartDriver", StartDriverCallback);
            
        }
        else {
            UpdateDebugInfo(this, "Could not extract DeviceId from the URL " + window.location);
        }
    }
);


//Step 1. Callback after device driver is called.
//[0]: error code, empty if success, 
function StartDriverCallback(context, result) {
    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        UpdateDebugInfo(context, "driver started");
        GetAllLocations(GetAllLocationsCallback);
    }
    else {
        UpdateDebugInfo(this, "StartDriver:" + result[0]);
    }
}

//Step 2:  Get Locations (this is called in setup flow and when new location is added)
function GetAllLocations(callbackFunc) {
    //Get locations
    new PlatformServiceHelper().MakeServiceCall("webapp/GetAllLocations", "", callbackFunc);
}

//Step 2: Callback for Get Locations
function GetAllLocationsCallback(context, result) {
    if (result[0] == "") {
        PopulateLocationLists(result);
    }
    else {
        UpdateDebugInfo(this, "GetAllLocationsCallback: " + result[0]);
    }


    //Check if device is ready
    CallDeviceRelatedFunctions("IsDeviceReady", IsDeviceReadyCallback);
    

}

function PopulateLocationLists(result) {
    //Read through the list and add them to the locationList
    //clear both lists
    $("#locationList").html("");
    $("#locationList2").html("");

    for (i = 1; i < result.length; i++) {
        $("#locationList").append("<option>" + result[i] + "</option>");
        $("#locationList2").append("<option>" + result[i] + "</option>"); //just used if we add new
    }
}

//Step 3: Check if the device is ready
function IsDeviceReadyCallback(context, result) {

    if (result[0] == "") {
        ClearInformationText();
        CallDeviceRelatedFunctions("GetDeviceDetails", GetDeviceDetailsCallback);      
    }
    else {
        //device is not ready wait a bit and call IsDeviceReady again
        UpdateInformationText("Device still loading, please wait");
        if (CURR_ATTEMPT <= MAX_ATTEMPTS) {
            setTimeout(function () {
                // This function gets called after the delay.  Meanwhile, javascript can do other useful things
                CallDeviceRelatedFunctions("IsDeviceReady", IsDeviceReadyCallback);
                CURR_ATTEMPT += 1;
            }, 1000 /* milliseconds – this is the delay until your function gets called */);
        }
        else {
            UpdateInformationText("Device did not start before timeout, use retry button");
            UpdateDebugInfo(this, "Device is not ready");
            $("#retryButton").show();
            CURR_ATTEMPT = 1;
        }
    }
}

//Step 4: Get device information including picture
function GetDeviceDetailsCallback(context, result) {

    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        UpdateDebugInfo(context, "GetDeviceDetails callback");
        //set the image and device type information       
        DEVICE_IMAGE_PATH = result[1];

        if ((DEVICE_IMAGE_PATH != "") & (DEVICE_IMAGE_PATH != null)) {
            //do a check if the service is ready and image will show up
            new PlatformServiceHelper().MakeServiceCall("webapp/IsServiceReady", '{"absoluteUrl": "' + DEVICE_IMAGE_PATH + '"}', IsServiceReadyCallback);
        }
        
        DEVICETYPE = result[2];
        if (DEVICETYPE != "") {
            var newpageTitle = "Configure your " + DEVICETYPE
            $("#pageTitle").html(newpageTitle);
        }

    }
    else {
        UpdateDebugInfo(this, "GetDeviceDetailsCallback:" + result[0]);
    }

    CallDeviceRelatedFunctions("GetCompatibleAppsNotInstalledWeb", GetCompatiableAppsNotInstalledWebCallback);

}

//Step 4.5 (in parallel I think)
//Used to check if pictures is ready
function IsServiceReadyCallback(context, result) {

    if (result[0] == "") {
        //reset image path
        // ShowDebugInfo();
        //UpdateDebugInfo("Service is ready");
        if (result[1] == "true") {
            if (DEVICE_IMAGE_PATH != "") {
                jQuery("#devicePicture").attr('src', DEVICE_IMAGE_PATH);
                //the date forces a reload and shows the picture
                //jQuery("#devicePicture").attr('src', DEVICE_IMAGE_PATH + "?" + new Date().getTime());
                $("#divDevicePicture").show();
            }
        }
        else {
            setTimeout(function () {
                // This function gets called after the delay.  Meanwhile, javascript can do other useful things
                UpdateDebugInfo("Service is ready");
                var data2 = JSON.stringify({ absoluteUrl: DEVICE_IMAGE_PATH }); // use JSON.stringify to safely build parameter lists
                new PlatformServiceHelper().MakeServiceCall("webapp/IsServiceReady", data2, IsServiceReadyCallback);
            }, 1000 /* milliseconds – this is the delay until your function gets called */);
        }
    }
    else {
        ShowDebugInfo();
        UpdateDebugInfo(this, "IsServiceReadyCallback: " + result[0]);
    }


}


//Step 5: Get apps that aren't  installed that are compatiable with this device
function GetCompatiableAppsNotInstalledWebCallback(context, result) {
    UpdateDebugInfo(context, result);
    if (result[0] == "") {
        UpdateDebugInfo(context, "AppsNotInstalled callback");
        parseReturnedAppList(result, "#divNIApps");
        CallDeviceRelatedFunctions("GetCompatibleAppsInstalledWeb", GetCompatiableAppsInstalledWebCallback);
    }
    else {
        UpdateDebugInfo(this, "GetCompatiableAppsNotInstalled:" + result[0]);
    }
}

//Step 6: Get already installed applications that are compatible
function GetCompatiableAppsInstalledWebCallback(context, result) {
    UpdateDebugInfo(context, result);
    if (result[0] == "") {
       // UpdateDebugInfo(context, "AppsInstalled callback");

        parseReturnedAppList(result, "#divIApps");
    }
    else {
        UpdateDebugInfo(this, "GetCompatiableAppsInstalled:" + result[0]);
    }
}

//Helper function to call web functions with only uniqueDeviceId paramters
function CallDeviceRelatedFunctions(functionName, callbackname) {
    var url2 = "webapp/" + functionName;
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '"}';
    new PlatformServiceHelper().MakeServiceCall(url2, data2, callbackname);
}

//[0]: error code, empty if success, this is going to be our new convention for Gui/Platform communication
//[1]: app name
//[2]: friendly name (??)
//[3]: url (??)
//[4-6] next app, etc.
function parseReturnedAppList(result, divName) {
    if (result[0] == "") {
        if (result.length > 2)
            $(divName).html("");

        for (var i = 1; i + 3 <= result.length; i = i + 3) {
            //i is deviceName, i+1 is description, i+2 is imageURL
            $(divName).append('<input type="checkbox" id="' + result[i] + '" />&nbsp&nbsp' + result[i] + " <br /> ");
            //NOTE, only using first returned parameter about app right now

            
        }
    }
}

//Called to Install applications user has selected.
function InstallApps() {

    //go through array of apps to install from back to front, ensuring that each install succeeds or showing warning
    //if counter > 0 we have apps to try to install  else call configure device
    if (APPS_TO_INSTALL_COUNTER > 0) {
        APPS_TO_INSTALL_COUNTER--;
        //call the install app web
        var url2 = "webapp/InstallAppWeb";
        var data2 = '{"appName": "' + APPS_TO_INSTALL[APPS_TO_INSTALL_COUNTER] + '"}';
        new PlatformServiceHelper().MakeServiceCall(url2, data2, InstallAppCallback);
    }
    else {
        //any new apps were succesfully are installed and APPS_USER_PERMITS has the list of permitted apps
        //Record the name and location of the device, callback notifies platform it is done
        ConfigureDevice();
    }
}

function InstallAppCallback(context, result) {

    var dataAsObject = JSON.parse(context.Data);
    var appName = dataAsObject.appName;  //figure out what app was being installed
    var idString = '#' + appName;

    if (result[0] != "") {
       // UpdateDebugInfo(this, "Problem installing apps:" + result[0]);
        $("#status").show();
        $("#status").html(appName + ":" + result[0]);

        //refresh UI in case stuff was installed
        CallDeviceRelatedFunctions("GetCompatibleAppsNotInstalledWeb", GetCompatiableAppsNotInstalledWebCallback);

    }
    else {
        //add since install succeeds 
        APPS_USER_PERMITS[APPS_USER_PERMITS.length] = appName;
     
        //Call install apps again in case there are more apps to install
        InstallApps();
    }
}

//Called to configure the device with friendly names 
function ConfigureDevice() {


    //Setup to call ConfigureDeviceWeb(string uniqueDeviceId, string friendlyName, bool highSecurity, string location, string[] apps);
    var url2 = "webapp/ConfigureDeviceWeb";
    //get the friendly name and location
    var friendlyName = $('#friendlyName').val();

   
    var locationTmp = $('#locationList option:selected').val();
  
    var APPS_USER_PERMITS_NAME = "apps";
   
    var appText = JSON.stringify(APPS_USER_PERMITS);
    var data2 = '{"uniqueDeviceId": "' + DEVICEID + '","friendlyName": "' + friendlyName + '","highSecurity": "false","location": "' + locationTmp + '",' + '"' + APPS_USER_PERMITS_NAME + '":' + appText + '}';


    
    new PlatformServiceHelper().MakeServiceCall(url2, data2, ConfigureDeviceCallback);
}
function ConfigureDeviceCallback(context, result) {
    if (result[0] == "") {


        //May be able to share function for this with other files: stubbed out at goToDashboardWebMainPage() but relative path is a problem;
        //Go to main page http://localhost:51430/GuiWeb/index.html
        var url = "../../../GuiWeb/index.html";
        window.location.href = encodeURI(url); //reroute
        
    }
    else {
        DisplayDebugging("Problem configuring apps:" + result[0]);
    }
}

function DoneButtonClicked() {
    UpdateDebugInfo(this, "Done button clicked");
    $("#status").hide();
    APPS_TO_INSTALL = [];
    APPS_TO_INSTALL_COUNTER = 0;

    //No devices named empty string
    if ($('#friendlyName').val() == "") {
        $("#status").html("Please name the device")
        $("#status").show();
        return;
    }


    //Install any new apps
    $('#divNIApps').children('input').each(function () {
        if (this.checked == true) {   
            UpdateDebugInfo(this, this.id + " app to install");
            //add to the candidate list of apps to install
            APPS_TO_INSTALL[APPS_TO_INSTALL.length] = this.id;
            APPS_TO_INSTALL_COUNTER++;
        }
    });


    //create array of which already installed apps are permitted to use this device
    $('#divIApps').children('input').each(function () {
        if (this.checked == true) {
            APPS_USER_PERMITS[APPS_USER_PERMITS.length] = this.id;
            UpdateDebugInfo(this, this.id + " app to permit");
        }
    });

    //Try to install all the new applications -> will either success and call configure device in callback or fail and show debugging.
    InstallApps();
}


//Code for handling adding of locations
function ToggleAddLocationOption() {
    if (LOCATION_OPTION_HIDDEN) {
        $("#divAddNewLocationOptions").show();
        LOCATION_OPTION_HIDDEN = false;
    }

    else {
        $("#divAddNewLocationOptions").hide();
        LOCATION_OPTION_HIDDEN = true;

    }
}

function AddNewLocation() {
    var newLocation = $('#newLocation').val();

    //don't let them add an empty location
    if (newLocation == "")
        return;

    var parentLocation = $('#locationList2 option:selected').val();

    var dataParm = '{"locationToAdd": "' + newLocation + '","parentLocation": "' + parentLocation + '"}';
   
    UpdateDebugInfo(this, dataParm);
    new PlatformServiceHelper().MakeServiceCall("webapp/AddLocation", dataParm, AddNewLocationCallback);
    ToggleAddLocationOption(); //hid the option
}

function AddNewLocationCallback(context, result) {
    if (result[0] == "") {      
        //refresh the list of locations and select the one that was added
        $('#newLocation').val(''); //clear the text box since location was added
        GetAllLocations(UpdateLocationListsCallback)
    }
    else {
        ShowDebugInfo();
        UpdateDebugInfo(this, "Can't add new location: " + result[0]);
    }
}

//Similar to first location list callback, but adds selecting new item and customized debug
function UpdateLocationListsCallback(context, result) {
    if (result[0] == "") {
        PopulateLocationLists(result);
    }
    else {
        UpdateDebugInfo(this, "UpdateLocationListCallback: " + result[0]);
    }
    //select the new item
    var numOptions = $('#locationList option').length;
    var selectOption = numOptions - 1;
    if (numOptions > 1) {
        var setSelected = '#locationList option:eq(' + selectOption + ')';
        $(setSelected).attr("selected", "selected");
    }
}


//User pushed retry button
function RetryDeviceReady() {
    CallDeviceRelatedFunctions("IsDeviceReady", IsDeviceReadyCallback);
}
jQuery.support.cors = true;


//This .js file supports the two html pages that do setup of wireless network and homeid, 
//HomeOSSetup1Wireless.html, HomeOSSetup2HomeID.html

var HOMEID = "";
var HOMEPWD = "";
var DEFAULTEMAIL = "";
var REMOTEACCESSLINK = "TODO";

function getAvailableWirelessNetworks() {
    new PlatformServiceHelper().MakeServiceCall("webapp/GetVisibleWifiNetworksWeb", "", GetVisibleWifiNetworksWebCallback);
}

function GetVisibleWifiNetworksWebCallback(context, result) {
    if (result[0] == "") {
        PopulateWifiList(result);
        $("#joinButton").show();
    }
    else {
        UpdateDebugInfo(this, "GetVisibleWifiNetworksWebCallback: " + result[0]);
        $("#joinButton").hide();
        $("#statusInfo").html(result[0]);
    }
}

function PopulateWifiList(result) {
    //Read through the network and add to the list   
    $("#wirelessNetworkList").html("");
    for (i = 1; i < result.length; i++) {
        $("#wirelessNetworkList").append("<option>" + result[i] + "</option>"); 
    }
}

function JoinWirelessNetwork() {

    $("#statusInfo").html("Joining wireless network");
    var homeWireless = $('#wirelessNetworkList option:selected').val();     //figure out the selected option
    var passPhrase = $('#networkSecurityKey').val();
    var data = '{"targetSsid": "' + homeWireless + '","passPhrase": "' + passPhrase + '"}';
    new PlatformServiceHelper().MakeServiceCall("webapp/ConnectToWifiNetworkWeb", data, ConnectToWifiNetworksWebCallback);
}

function ConnectToWifiNetworksWebCallback(context, result) {

    if (result[0] == "") {
        GoToHTMLPage("HomeOSSetup2HomeID.html")
    }
    else {
        UpdateDebugInfo(this, "GetVisibleWifiNetworksWebCallback: " + result[0]); //only shows up if debug div is set to normal in .html file
        $("#statusInfo").html(result[0]);
    }

}

function RetryWirelessNetwork() {
    $("#statusInfo").html("");
    getAvailableWirelessNetworks();
}


//Functions for HomeOSSetup2HomeID

function SetHomeID() {
    HOMEID = $('#homeID').val();
    HOMEPWD = $('#homeIDPassword').val();
    DEFAULTEMAIL = $('#defaultEmail').val();
    if ((HOMEID == "") || (HOMEPWD == "") || (DEFAULTEMAIL == "")) {
        $("#statusInfo").html("Home ID, Home Password and Default Email are required");
        return;
    }
    $("#statusInfo").html("Setting home ID");
    //do we want error checking here on homeID?
    var data = '{"homeId": "' + HOMEID + '","password": "' + HOMEPWD + '"}';
    new PlatformServiceHelper().MakeServiceCall("webapp/SetHomeIdWeb", data, SetHomeIdWebCallback); 
}

function SetHomeIdWebCallback(context, result) {

    if (result[0] == "") {
        $("#statusInfo").html("");
        SetDefaultEmail();
    }
    else {
        $("#statusInfo").html(result[0]);
        HOMEID = "";
        HOMEPWD = "";
    }
   
}

function SetDefaultEmail() {

    DEFAULTEMAIL = $('#defaultEmail').val();
    if (DEFAULTEMAIL != "") {
        $("#statusInfo").html("Setting default email address");
        var data = '{"emailAddress": "' + DEFAULTEMAIL + '"}';
        new PlatformServiceHelper().MakeServiceCall("webapp/SetNotificationEmailWeb", data, SetNotificationEmailWebCallback);
    }
}

function SetNotificationEmailWebCallback(context, result) {
    if (result[0] == "") {
        //setup complete show the final information
        $("#setupHomeID").hide();
        SetupFinalInformation();

    }
    else {
        $("#statusInfo").html(result[0]);
    }
}

    function SetupFinalInformation() {

        $("#statusInfo").hide();
        $("#showFinalInformation").show();

        //call to find out remote access link
        //GetRemoteAccessUrlWeb
        new PlatformServiceHelper().MakeServiceCall("webapp/GetRemoteAccessUrlWeb", "", GetRemoteAccessUrlWebCallback);

    }

    function GetRemoteAccessUrlWebCallback(context, result) {
        if (result[0] == "") {
            REMOTEACCESSLINK = result[1];
        }
    
        var infoText = "Your Home Hub has been successfully configured.<br />Home ID: " + HOMEID + "<br /> Home Password:" + HOMEPWD +
             "<br />Remote Access: <a href='" + REMOTEACCESSLINK + "' target='_blank'>" + REMOTEACCESSLINK + "</a><br />Default Email: " + DEFAULTEMAIL + "<br />";
        $("#houseSetupInformation").html(infoText);

    }

    function GoToMainPageWithHomeID() {
        var urlWithHomeID = "../../" + HOMEID + "/GuiWeb/index.html";
        GoToHTMLPage(urlWithHomeID);

    }
    function EmailInformation() {


    }


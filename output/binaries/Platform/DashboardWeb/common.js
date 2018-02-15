jQuery.support.cors = true;

//Used in making WCF service calls
var Type;
var Url;
var Data;
var ContentType;
var DataType;
var ProcessData;


//Call this way
//var qs = getQueryStringArray(); 
//then qs.<parameterName> will give value
function getQueryStringArray() {
    
    var assoc = [];
    var decodedParams = decodeURI(window.location.search); //decodeURI in case there were spaces.
    var items = decodedParams.substring(1).split('&');
    for (var j = 0; j < items.length; j++) {
        var a = items[j].split('=');
        assoc[a[0]] = a[1];
    }
    return assoc;
}


function GoToFinalSetup(deviceID) {
    var url = "../../GuiWeb/AddDeviceFinalDeviceSetup.html?DeviceId=";
    window.location.href = encodeURI(url + deviceID); //reroute to final setup
}

function GoToHTMLPage(url) {
    window.location.href = encodeURI(url);
}


function IsZwaveDevice(deviceID) {
    if (deviceID.indexOf("ZwaveNode::") != "-1") {
        return true;
    }
    else {
        return false;
    }
}

//Pages typically have  div with this id: divInformationText to the bottom of pages as place to display message to the user

function UpdateInformationText(newText) {
    ClearInformationText();
    ShowInformationText();
    $("#divInformationText").html("<p>" + newText + "</p>");
}

function ClearInformationText() {
    $("#divInformationText").html("");
}

function ShowInformationText() {
    $("#divInformationText").show();
}
function HideInformationText() {
    $("#divInformationText").hide();
}

function DisplayDebugging(msg) {
    ShowDebugInfo();
    UpdateDebugInfo(this, msg);
}
//By convention we add div with this id: divDebugInfo to the bottom of pages as place to display debugging info
function UpdateDebugInfo(object, string) {
    if ($("#divDebugInfo").is(':hidden'))
        return;
   // $("#controlDebugInfoText").html("<p>" + string + "</p>"); //old way - should be removed, keeping for backward compat
    $("#divDebugInfo").html(string);
}

function ShowDebugInfo() {
    $("#divDebugInfo").show();
}

function HideDebugInfo() {
    $("#divDebugInfo").hide();
}


function PlatformServiceHelper() {

    this.ClearFields = function () {
        this.Type = null;
        this.Url = null;
        this.Data = null;
        this.ContentType = null;
        this.DataType = null;
        this.ProcessData = null;

        this.CountMax = null;
        this.ClipUrl = null;
        this.Callback = null;
    }

 
    this.MakeServiceCall = function (url_parm, data_parm, callback) {
        this.Type = "POST";
        this.Url = url_parm;
        this.Data = data_parm;
        this.ContentType = "application/json; charset=utf-8";
        this.DataType = "json";
        this.ProcessData = true;

        this.Callback = callback;
        this.CallService();
    }


    // Function to call WCF  Service       
    this.CallService = function () {
        var Type = this.Type;
        var Url = this.Url;
        var Data = this.Data;
        var ContentType = this.ContentType;
        var DataType = this.DataType;
        var ProcessData = this.ProcessData;

        var ClipUrl = this.ClipUrl;
        var CountMax = this.CountMax;
        var Callback = this.Callback;

        var SucceededServiceCallback = this.SucceededServiceCallback;
        var FailedServiceCallback = this.FailedServiceCallback;
        var Context = this;

        UpdateDebugInfo(this, 'call: ' + this.DataType + " URL: " + this.Url + " Data: " + this.Data);

        

        $.ajax({
            type: Type, //GET or POST or PUT or DELETE verb
            url: Url, // Location of the service
            data: Data, //Data sent to server
            contentType: ContentType, // content type sent to server
            dataType: DataType, //Expected data format from server
            processdata: ProcessData, //True or False
            //********* AUTH CODE Adding this function so that ajax call can put token in header
            beforeSend: function (req) {
                req.setRequestHeader('Authorization', HOMEOSTOKEN);// HOMEOSTOKEN is set by homeos.js
            },
            //***
            success: function (msg) {//On Successfull service call
                SucceededServiceCallback(this, msg);
            },
            error: function (msg) {
                this.FailedServiceCallback(this, msg);
                //********* AUTH CODE Adding this function so that when ajax call fails. homeos.js redirects to respective endpoint e.g. liveid or systemhigh
                homeosErrorHandler(this, msg); // error handler defined in homeos.js
                //****
            },
            context: Context
        }); 
    }

    this.FailedServiceCallback = function (context, result) {
        ShowDebugInfo();
        UpdateDebugInfo(this, 'failed: ' + context.DataType + " URL: " + context.Url + " Data: " + context.Data + "result: " + result.status + ' ' + result.statusText);
    }

    this.SucceededServiceCallback = function (context, result) {
        if (null != context) {
            UpdateDebugInfo(context, "succeeded: " + context.DataType + " URL: " + context.Url + " Data: " + context.Data + " Result: " +result);
        }
        if (context != null && context.DataType == "json" && result != null && context.Callback != null) {
            context.Callback(context, result);
        }
    }
}
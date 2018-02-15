jQuery.support.cors = true;

var USER_ID = 'jeff';
var USER_PASSWORD = 'Jeff';

$(document).ready(
);

function ShowDoorjambPortsInfo() {
    new DoorjambServiceHelper().GetReceivedMessages(USER_ID, USER_PASSWORD, GetReceivedMessagesCallback);
}

//UX Helper
function UpdateDebugInfo(object, string) {
    if ($("#divDoorjambServiceDebug").is(':hidden'))
        return;
    $("#doorjambServiceDebugInfoText").html("<p>" + string + "</p>");
}

function GetReceivedMessagesCallback(context, result) {
    
    var portsInfo = result.GetReceivedMessagesResult;
    $("#DoorjambList").html('');
    for (i = 0; i < result.GetReceivedMessagesResult.length; i++) {
        $("#DoorjambList").append("<br />" + portsInfo[i]);
    }
}

function DoorjambServiceHelper() {

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

    this.GetReceivedMessages = function (userid, password, callback) {
        this.Type = "POST";
        this.Url = "webapp/GetReceivedMessages";
        this.Data = '{"username": "' + userid + '","password": "' + password + '"}';
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

        UpdateDebugInfo(this, 'call: ' + this.DataType + " URL: " + this.Url );

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
                this.SucceededServiceCallback(this, msg);
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
        UpdateDebugInfo(this, 'failed: ' + context.DataType + " URL: " + context.Url + "Result: " + result.status + ' ' + result.responseText);
        context.ClearFields();
    }

    this.SucceededServiceCallback = function (context, result) {
       
        if (null != context) {
            UpdateDebugInfo(context, "succeeded: " + context.DataType + " URL: " + context.Url + " Data: " + context.Data + " Number of Results Rx: " + result.GetReceivedMessagesResult.length);
        }
        GetReceivedMessagesCallback(context, result); 
        if (context != null && context.DataType == "json" && result != null && context.Callback != null) {
            context.Callback(context, result);
        }
    }
}

jQuery.support.cors = true;

// TODO: No authentication currently
var USER_ID = 'jeff';
var USER_PASSWORD = 'Jeff';

$(document).ready(
);

function getCurrentUtcTime() {
    var date = new Date();
    var dateDayMMDDYYYY = date.getUTCMonth()+1 + "/" + date.getUTCDate() + "/" + date.getUTCFullYear();
    var dateTimeHHMMSSXM = "";
    
    if (date.getUTCHours() == 12)
    {
        dateTimeHHMMSSXM = date.getUTCHours() + ":" + date.getUTCMinutes() + ":" + date.getUTCSeconds() + " PM";
    }
    else if (date.getUTCHours() > 12) {
        dateTimeHHMMSSXM = date.getUTCHours() - 12 + ":" + date.getUTCMinutes() + ":" + date.getUTCSeconds() + " PM";
    }
    else
    {
        dateTimeHHMMSSXM = date.getUTCHours() + ":" + date.getUTCMinutes() + ":" + date.getUTCSeconds() + " AM";
    }

    return dateDayMMDDYYYY + " " + dateTimeHHMMSSXM;
}

function ShowHeartbeatInfo() {
    new HeartbeatMonitorServiceHelper().GetHeartbeatInfoRangeByCloudTime(USER_ID, USER_PASSWORD,
        getCurrentUtcTime(), "-00:30:00", GetHeartbeatInfoRangeByCloudTimeCallback);

}

//UX Helper
function UpdateDebugInfo(object, string) {
    if ($("#divHeartbeatPortalServiceDebug").is(':hidden'))
        return;
    $("#heartbeatPortalServiceDebugInfoText").html("<p>" + string + "</p>");
}

function GetHeartbeatInfoRangeByCloudTimeCallback(context, result) {
    var portsInfo = result.GetHeartbeatInfoRangeByCloudTimeResult;
    $("#HeartbeatList").val('');
    for (i = 0; i < result.GetHeartbeatInfoRangeByCloudTimeResult.length; i++) {
        $("#HeartbeatList").append("<br />" + "Service UTC Time =" + portsInfo[i].key + ", HomeId=" + portsInfo[i].value.HomeId + ", HubTimestamp=" + portsInfo[i].value.HubTimestamp + ", TotalCpuPercentage=" + portsInfo[i].value.TotalCpuPercentage + ", PhysicalMemoryBytes=" + portsInfo[i].value.PhysicalMemoryBytes);
    }
}

function HeartbeatMonitorServiceHelper() {

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

    this.GetHeartbeatInfoRangeByCloudTime = function (userid, password, startTimeUtc, timeOffset, callback) {
        this.Type = "POST";
        //this.Url = "http://localhost:5003/HeartbeatMonitorService.svc/GetHeartbeatInfoRangeByCloudTime";
        // this.Url = "http://1bffa230aa8c49c49e7ad853014cecd0.cloudapp.net:5003/HeartbeatMonitorService.svc/GetHeartbeatInfoRangeByCloudTime";
        this.Url = "http://homelab.cloudapp.net:5003/HeartbeatMonitorService.svc/GetHeartbeatInfoRangeByCloudTime";
        this.Data = '{"startTimeUtc": "' + startTimeUtc + '","timeOffset": "' + timeOffset + '"}';
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
            success: function (msg) {//On Successfull service call
                SucceededServiceCallback(this, msg);
            },
            error: function (msg) {
                FailedServiceCallback(this, msg);
            }, // When Service call fails
            context: Context
        });
    }

    this.FailedServiceCallback = function (context, result) {
        UpdateDebugInfo(this, 'failed: ' + context.DataType + " URL: " + context.Url + " Data: " + context.Data + "result: " + result.status + ' ' + result.statusText);
        context.ClearFields();
    }

    this.SucceededServiceCallback = function (context, result) {
        if (null != context) {
            UpdateDebugInfo(context, "succeeded: " + context.DataType + " URL: " + context.Url + " Data: " + context.Data);
        }
        if (context != null && context.DataType == "json" && result != null && context.Callback != null) {
            context.Callback(context, result);
        }
    }
}

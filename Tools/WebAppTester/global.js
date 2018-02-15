// globals
jQuery.support.cors = true;

var ENABLE_DEBUG_TRACES = false;

function WebAppTester() {
    if (ENABLE_DEBUG_TRACES) {
        this.DebugDiv = $(document.createElement('div'));
        $(document.body.appendChild(this.DebugDiv));
    }
    this.ClearFields = function () {
        this.Type = null;
        this.Url = null;
        this.Data = null;
        this.ContentType = null;
        this.DataType = null;
        this.ProcessData = null;

        this.Callback = null;
    }

    this.UpdateDebugInfo = function (string) {
        if ($(this.debugDiv).is(':hidden'))
            return;
        var statusTimeStamp = new Date();
        $(this.DebugDiv).html("<p>" + statusTimeStamp.getHours() + ':' + statusTimeStamp.getMinutes() + ':' + statusTimeStamp.getSeconds() + '.' + statusTimeStamp.getMilliseconds() + ' ' + string + "</p>");
    }

    this.TestNow = function (url, key1, val1, callback) {
        this.Type = "POST";
        this.Url = url;
        this.Data = '{"' + key1 + '": "' + val1 + '"}';
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

        var SucceededServiceCallback = this.SucceededServiceCallback;
        var FailedServiceCallback = this.FailedServiceCallback;
        var Context = this;

        this.UpdateDebugInfo('call: ' + this.DataType + " URL: " + this.Url + " Data: " + this.Data);

        $.ajax({
            type: Type, //GET or POST or PUT or DELETE verb
            url: Url, // Location of the service
            data: Data, //Data sent to server
            contentType: ContentType, // content type sent to server
            dataType: DataType, //Expected data format from server
            processdata: ProcessData, //True or False
            success: function (msg) {//On Successfull service call
                this.SucceededServiceCallback(this, msg);
            },
            error: function (msg) {
                this.FailedServiceCallback(this, msg);
            }, // When Service call fails
            context: Context
        });
    }

    this.FailedServiceCallback = function (context, result) {
        this.UpdateDebugInfo('Failed: ' + context.DataType + " URL: " + context.Url + " Data: " + context.Data + "result: " + result.status + ' ' + result.statusText);
        this.ClearFields();
    }

    this.SucceededServiceCallback = function (context, result) {
        if (null != context) {
            this.UpdateDebugInfo("Succeeded: " + context.DataType + " URL: " + context.Url + " Data: " + context.Data);
        }
        if (context != null && context.DataType == "json" && result != null && context.Callback != null) {
            context.Callback(context, result);
        }
    }

}

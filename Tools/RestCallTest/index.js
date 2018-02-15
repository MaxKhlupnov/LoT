jQuery.support.cors = true;

$(document).ready(
);

function ArgValuePair() {

    this.Arg = null;
    this.Value = null;
}

function GetArgValuePairArray() {
    var argValuePairArray = new Array();
    var count = 0;
    $('.additionalArg input').each(function (index) {
        if (index % 2 == 0) {
            argValuePairArray[count] = new ArgValuePair();
            argValuePairArray[count].Arg = $(this).attr('value');
        }
        else {
            argValuePairArray[count++].Value = $(this).attr('value');
        }
    });

    return argValuePairArray;
}

function CallMethod() {
    new ServiceHelper().MethodCall($("input[name=httpUrl]").val(), GetArgValuePairArray(), MethodCallback);
}

//UX Helper
function UpdateDebugInfo(object, string) {
    if ($("#serviceDebugInfo").is(':hidden'))
        return;
    $("#serviceDebugInfoText").html("<p>" + string + "</p>");
}

function MethodCallback(context, result) {
    var objectText = new Object();
    objectText.text = "";
    jsonObjectDump(result, 0, objectText); // recursive
    $("#textAreaRestCallResults").val(objectText.text);
}

function jsonObjectDump(json, level, objectText) {
    if (level == null) {
        level = 0;
    }
    for (prop in json) {
        // add padding
        for (a = 0; a < level; a++) {
            objectText.text += ' ';
        }
        // add property
        objectText.text += '"' + prop + '": ';

        // display property value in case its string
        if (typeof json[prop] == "string") {
            objectText.text += '"' + json[prop] + '"' + "\n";
            // else run the jsonEcho recursion
        } else {
            objectText.text += '{' + "\n";
            jsonObjectDump(json[prop], level + 1, objectText);
            for (a = 0; a < level; a++) {
                objectText.text += ' ';
            }
            objectText.text += '}' + "\n";
        }
    }
}

function ServiceHelper() {

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

    this.MethodCall = function (url, argValuePairArray, callback) {
        this.Type = "POST";
        this.Url = url;
        this.ContentType = "application/json; charset=utf-8";
        this.DataType = "json";
        this.ProcessData = true;
        this.Data = '{';
        this.ArgValuePairArray = argValuePairArray;
        for (var j = 0; j < this.ArgValuePairArray.length; ++j) {
            if (j > 0)
                this.Data += ',';
            this.Data += '"' + this.ArgValuePairArray[j].Arg + '":"' + this.ArgValuePairArray[j].Value + '"';
        }
        this.Data += '}';

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

        var ArgValuePairArray = this.ArgValuePairArray;
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

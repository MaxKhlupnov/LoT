
// document load handler
// cache the frequently used GetImage Service Helpers
var g_smartCamGetImageServiceHelper = null;
var g_isRecording = false;
var g_keepalive;
var g_currentCamera = "";
var g_maxClipsToDisplay = 5;


var CAMERA_INFORMATION_ARRAY = null;

$(document).ready(
    function () {
        //Get the camera list
        new PlatformServiceHelper().MakeServiceCall("webapp/GetCameraList", "", GetCameraListCallback);
    }
);


function GetCameraListCallback(context, result) {

    //2 entries for each camera i:camera name  i+1:roles supported 
    CAMERA_INFORMATION_ARRAY = result.GetCameraListResult;

    if (CAMERA_INFORMATION_ARRAY[0] == "") {

        if (CAMERA_INFORMATION_ARRAY.length < 3) { //no cameras installed
            $("#status").show();
            $("#cameraGoodness").hide();
            $("#cameraList").hide();
            $("#videoClips").hide();
            return;
        }

        //Add the resulting list to the cameraList 
        var selected = '<option value="o1" selected ="selected">';
        var normal = '<option value="o1">';
        var end = "</option>";

        //first one is selected
        $("#cameraList").append(selected + CAMERA_INFORMATION_ARRAY[1] + end);

        for (j = 3; j + 1 < CAMERA_INFORMATION_ARRAY.length; j = j + 2) {
            $("#cameraList").append(normal + CAMERA_INFORMATION_ARRAY[j] + end);
        }

        CameraSelected();
        GetImage();
    }
}

//Called when selected camera changes
function CameraSelected() {

    if (g_isRecording == true) {   //if camera changes we need to stop recording. 
        RecordToggle();
    }

    g_currentCamera = $("#cameraList :selected").text();

    //figure out if we should show the pan/tilt for this camera
    if (isPTCamera(g_currentCamera)) {
        //doing this way for layout reasons - if you put in <div> for the pt button it goes on other row.
        $("#left").show();  //show the pan/tilt
        $("#right").show();
        $("#up").show();
        $("#down").show();   
    }
    else {
        $("#left").hide();  //show the pan/tilt
        $("#right").hide();
        $("#up").hide();
        $("#down").hide();
    }

    //Get the clips and show them
    GetRecordedClips();

    //Determine state of motion triggered recording and later video recording
    GetMotionRecordingState();

    //Get Video recording state - might combine this and motion recording state method
    GetVideoUploadingState();
}


function isPTCamera(cameraName) {
    if (null == CAMERA_INFORMATION_ARRAY)
        return false;
   
    for (var i = 1; i + 1 < CAMERA_INFORMATION_ARRAY.length; i = i + 2) {
        if (CAMERA_INFORMATION_ARRAY[i] == cameraName) {
            return (CAMERA_INFORMATION_ARRAY[i + 1].indexOf(":ptcamera") != "-1");
        }
    }
    return false;
}

function ControlCamera(direction) {
    new PlatformServiceHelper().MakeServiceCall("webapp/ControlCamera", '{"control": "' + direction + '","cameraFriendlyName": "' + g_currentCamera + '"}', ControlCameraCallback);
}

function ControlCameraCallback(context, result) {
    //Someday we might want to do something here
    if (result[0] != "") {
        DisplayDebugging("ControlCameraCallback:" + result[0]);
   }
   
}


function GetImage() {
 
    if (null == g_smartCamGetImageServiceHelper) {
        g_smartCamGetImageServiceHelper = new PlatformServiceHelper();
    }

    g_smartCamGetImageServiceHelper.MakeServiceCall("webapp/GetWebImage", '{"cameraFriendlyName": "' + g_currentCamera + '"}', GetWebImageCallback);
}

function GetWebImageCallback(context, result) {
    $('#camera1Image').attr('src', "data:image/jpg;base64," + result);
    GetImage();
}


function RecordToggle() {

    if (g_isRecording == false) {
        $('#recordBTN').attr('src', "Assets/MetroStop.png");
        $('#recordText').html("Recording");
        g_isRecording = true;
        StartRecord();
    }
    else {
        $('#recordBTN').attr('src', "Assets/MetroRecord.png");
        $('#recordText').html("");
        g_isRecording = false;
        StopRecord();
    }
}

//Starting video will sends start message and starts a timeout to send keepalives.
function StartRecord() {
    g_keepalive = setInterval(function () { DoKeepAlive() }, 25000);  //24 second call keepalive to keep recording going - if page changes recording will timeout after 30 seconds
    new PlatformServiceHelper().MakeServiceCall("webapp/StartOrContinueRecording", '{"cameraFriendlyName": "' + g_currentCamera + '"}', RecordVideoCallback);
}

function StopRecord() {
    //Stop the keepAlive
    clearInterval(g_keepalive);

    new PlatformServiceHelper().MakeServiceCall("webapp/StopRecording", '{"cameraFriendlyName": "' + g_currentCamera + '"}', RecordVideoCallback);

    //after a small delay for clip to write get new clips
    setTimeout(function () {
        GetRecordedClips();
    }, 1000 /* milliseconds – this is the delay until your function gets called */);
           
}



function RecordVideoCallback(context, result) {

    //if (result[0] != "") {
    //    DisplayDebugging("RecordVideoCallback:" + result[0]);
    //}

}


function DoKeepAlive() {
    //Send the KeepAlive;
    new PlatformServiceHelper().MakeServiceCall("webapp/StartOrContinueRecording", '{"cameraFriendlyName": "' + g_currentCamera + '"}', RecordVideoCallback);
}


function GetRecordedClips() {
    new PlatformServiceHelper().MakeServiceCall("webapp/GetRecordedClips", '{"cameraFriendlyName": "' + g_currentCamera + '","countMax": "' + g_maxClipsToDisplay + '"}', GetRecordedClipsCallback);
}

function isRemoteRequest() {
    if (window.location.href.indexOf("cloudapp") != "-1") {
        return true;
    }

    return false;
}


function GetRecordedClipsCallback(context, result) {

    var remoteRequest = isRemoteRequest();

    $("#videoClips").html("");

    if (result[0] == "") {
        for (i = 1; i < result.length; i++) {

            //  $("#videoClips").append('<div>Screen width' + screen.width + ':' + screen.availWidth + ':' + window.innerWidth + '</div>');
            if (screen.width > 770)  //not on smart phone - smart phones are giving us trouble with showing video
                $("#videoClips").append('<div><video class="snapshot_image col"  src="' + result[i] + '" controls="controls" /></div>');
            else {
                $("#videoClips").append('<div><a href="' + result[i] + '">Recording: ' + i + '</a></div>');
                //$("#videoClips").append('<div class="snapshot_image col"><a href="' + result[i] + '"><img src ="Assets/poster-image.png" /></a></div>');  //Maybe add something like this in remote case: preload="none"  poster="Assets/poster-image.png"
            }
        }

    }
    else {
        DisplayDebugging("GetRecordedClipsCallback:" + result[0]);
    }
}

//Figure out toggle state for whether or not motion based recording is happening

function GetMotionRecordingState() {
    new PlatformServiceHelper().MakeServiceCall("webapp/IsMotionTriggerEnabled", '{"cameraFriendlyName": "' + g_currentCamera +  '"}', GetIsMotionTriggeredCallback);
}

function GetIsMotionTriggeredCallback(context, result) {
    if (result.IsMotionTriggerEnabledResult[0] == "") {
        if (result.IsMotionTriggerEnabledResult[1] == "True") {
            document.getElementById("mCheckbox").checked = true
        }
    }
    else {
        DisplayDebugging("GetMotionTriggeredCallback:" + result[0]);
    }
}

//set the new state for motion recording
function ToggleMotionRecording(cbox) {

    var newState = false;
    if (cbox.checked) {
        newState = true;
    }
    new PlatformServiceHelper().MakeServiceCall("webapp/EnableMotionTrigger", '{"cameraFriendlyName": "' + g_currentCamera + '","enable": "' + newState  + '"}', ToggleMotionTriggeredCallback);
}

function ToggleMotionTriggeredCallback(context, result) {
    //possibly do something here to show errors
    ;

}

//set the new state for motion recording
function ToggleVideoUploading(cbox) {

    var newState = false;
    if (cbox.checked) {
        newState = true;
    }
    new PlatformServiceHelper().MakeServiceCall("webapp/EnableVideoUpload", '{"cameraFriendlyName": "' + g_currentCamera + '","enable": "' + newState + '"}', ToggleMotionTriggeredCallback);
}

function GetVideoUploadingState() {
    new PlatformServiceHelper().MakeServiceCall("webapp/IsVideoUploadEnabled", '{"cameraFriendlyName": "' + g_currentCamera + '"}', GetIsVideoUploadingCallback);
}

function GetIsVideoUploadingCallback(context, result) {
    if (result.IsVideoUploadEnabledResult[0] == "") {
        if (result.IsVideoUploadEnabledResult[1] == "True") {
            document.getElementById("videoCheckbox").checked = true
        }
    }
    else {
        DisplayDebugging("GetVideoUploadingCallback:" + result[0]);
    }
}

function RefreshVideos() {
    //Get the clips and show them
    GetRecordedClips();
}
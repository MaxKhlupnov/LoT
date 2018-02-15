var appctx = document.URL;
var HOMEOSTOKEN;
var isDebugMode = false; 

//constants 
var LiveIdAppId = "000000004C0FF8E2";
var cookieName = "homeostoken";
var cookieStackHead = cookieName + ":tok";
var liveIdLogoutUrl = "http://login.live.com/logout.srf?appid="+LiveIdAppId+"&appctx=" + appctx;
var policyEndpointSuffix = "/auth/policy";
var statusEndpointSuffix = "/auth/status";
var GuiWebEndpointSuffix = "../GuiWeb/SignIn.html";
var GateKeeperDomain = "lab-of-things.net"; // If the page is being accessed from this domain i.e. remotel, then we dont query the hub for the status of the endpoint 
//


var currTokenCookieName = getCookie(cookieStackHead);
if (!currTokenCookieName || currTokenCookieName == null) // no stack present or head points to invalid
{
    HOMEOSTOKEN = "";
}
else
{
    var currentTokenName = getCurrentToken(cookieStackHead, 'name');
    
    if (currentTokenName == null || !currentTokenName)
    {
        popToken(cookieStackHead, cookieName);
        redirect(document.URL);
    }
    
    HOMEOSTOKEN = currentTokenName + " " + getCurrentToken(cookieStackHead, 'token');
}


function getCurrentToken(cookieStackHead, attribute)
{
    var currTokenName = getCookie(cookieStackHead);
    var tokenCookie = getCookie(currTokenName);

    if (!tokenCookie) return null;

    var retVal = ""; 
    if(attribute=="level")
    {
        var level = tokenCookie.split('&')[0];
        retVal = level.split('=')[1]; 
    }
    if(attribute=="name")
    {
        var name = tokenCookie.split('&')[1];
        retVal = name.split('=')[1];
    }
    if (attribute == "user") {
        var user = tokenCookie.split('&')[2];
        retVal = user.split('=')[1];
    }
    if(attribute=="token")
    {
        var token = tokenCookie.split('&')[3];
        retVal = token.substring(token.indexOf('=') + 1); 
    }
   
    return retVal;
}



function homeosSignout() 
{
    if (getCurrentToken(cookieStackHead, 'name') == "liveid") // handle liveid signout separately
    {
        popToken(cookieStackHead, cookieName);
        //   document.location.href = liveIdLogoutUrl;
        redirect(GuiWebEndpointSuffix);
    }
    else
    {
        popToken(cookieStackHead, cookieName);
        redirect(GuiWebEndpointSuffix);
    }
    
}

function homeosSignedInUsing() {
    return getCurrentToken(cookieStackHead, 'name');
}

function homeosSignedInAs()
{
    return getCurrentToken(cookieStackHead, 'user');
}

function popToken(cookieStackHead, cookieName)
{
    var i = null;
    var currTokName = getCookie(cookieStackHead);
    if (currTokName == null || !currTokName)
    {
        delCookie(cookieStackHead);
        return null;
    }
    else
    {
        i = currTokName.split(':')[1];
    }

    i = parseInt(i) - 1;
    if (i >= 0)
    {
        setCookie(cookieStackHead, cookieName+':' + i);
    }
    else
    {
        delCookie(cookieStackHead);
    }
    var retVal = getCookie(currTokName);
    delCookie(currTokName);
    return retVal;
}

function getCookie(c_name)
{
    var c_value = document.cookie;
    var c_start = c_value.indexOf(' ' + c_name + '=');
    if (c_start == -1)
    { c_start = c_value.indexOf(c_name + '='); }
    if (c_start == -1)
    { c_value = null; }
    else
    {
        c_start = c_value.indexOf('=', c_start) + 1;
        var c_end = c_value.indexOf(';', c_start);
        if (c_end == -1)
        { c_end = c_value.length; }
        c_value = unescape(c_value.substring(c_start, c_end));
    }
    return c_value;
}
function setCookie(c_name, value, expirySeconds)
{
    var exdate = new Date();
    exdate.setTime(exdate.getTime() + 1000 * expirySeconds);
    var c_value = escape(value) + ((expirySeconds == null) ? '' : '; expires=' + exdate.toGMTString());
    c_value += '; path=/';
    document.cookie = c_name + '=' + c_value;
}

function redirect(url)
{
   
    if (isDebugMode)
    {
        document.write("\nRedirecting to " + url);
    }
    else
        document.location.href = url;
}

function delCookie(c_name, value)
{
    var c_value = escape(value) + "; expires=" + "Thu, 01 Jan 1970 00:00:01 GMT";
    document.cookie = c_name + "=" + c_value +"; path=/; ";
}

function getPolicyEndpoint()
{
    var host = window.location.host;
    var path = window.location.pathname;
    var protocol = window.location.protocol; 

    var homeId = path.split('/')[1]; 
    var policyEndpointUrl = window.location.protocol + "//" + window.location.host + "/" + homeId + policyEndpointSuffix ;
    return policyEndpointUrl;
}

function getStatusEndpoint() {
    var host = window.location.host;
    var path = window.location.pathname;
    var protocol = window.location.protocol;

    var homeId = path.split('/')[1];
    var statusEndpointUrl = window.location.protocol + "//" + window.location.host + "/" + homeId + statusEndpointSuffix;
    return statusEndpointUrl;
}


function homeosErrorHandler(context, result)
{
    
    if (result.status == "403" || result.status == "401" || result.status == "400" ) //Forbidden, Unauthorized or bad request 
    {
        var policyEndpoint = getPolicyEndpoint();
        new PolicyEndpointRequest().GetPolicy(document.URL, policyEndpoint, cookieStackHead, cookieName, result.status);
    }
    
    else if (result.status == "404")
    {
        document.write("\n Cannot find application web service endpoints");
    }

    else
    {
        if(isDebugMode)
            document.write("Error in invoking application web endpoints (Response Code Received: "+result.status+","+JSON.stringify(result)+")");
        document.write(".");
    }
    
    

}


function PolicyEndpointRequest() {

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

    this.GetPolicy = function (urlToAccess, policyEndpoint, cookieStackHead, cookieName, receivedStatus) {
        this.Type = "POST";
        this.Url = policyEndpoint;
        this.Data = '{"url": "' + urlToAccess+'"}';
        this.ContentType = "application/json; charset=utf-8";
        this.DataType = "json";
        this.ProcessData = true;
        this.cache = false;
        this.cookieStackHead = cookieStackHead;
        this.cookieName = cookieName;
        this.receivedStatus = receivedStatus;
        this.CallService();
    }

    // Function to WCF endpoint 
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
        var receivedStatus = this.receivedStatus;
        this.cache = false;
        var cookieStackHead = this.cookieStackHead;
        var cookieName = this.cookieName;

        $.ajax({
            type: Type, //GET or POST or PUT or DELETE verb
            url: Url, // Location of the service
            data: Data, //Data sent to server
            contentType: ContentType, // content type sent to server
            dataType: DataType, //Expected data format from server
            processdata: ProcessData, //True or False
            cache: false,
            async: false, 
            success: function (msg) {//On Successfull service call
                SucceededServiceCallback(this, msg);
            },    
            error: function (msg) {
                FailedServiceCallback(this, msg);
            },  // 
            context: Context
        });
        return "";
    }

    this.FailedServiceCallback = function (context, result)
    {
        alert("Initiating access control....Please Wait");
        //Quering Policy Engine for access....Please Wait
    }

    this.SucceededServiceCallback = function (context, result)
    {
        
        var goToPrivilegeLevelIndex = 0;

        if (isDebugMode)
            document.write("\n received url status: "+context.receivedStatus+" result.length = "+result.length);

        if (result.length == 0)
        {
            message = "Access Unavailable. Remote access requires adding Microsoft Account users on Home Hub using the Settings page ";
            //alert(message);
            redirect(GuiWebEndpointSuffix + "?message=" + encodeURIComponent(message));
            return;
        }
        else if (context.receivedStatus == "400" || context.receivedStatus == "401") {
            for (var i = 0; i < result.length; i++) // if the token I sent is in the list of "accepted privileges" and I got unauthorized => my token is expired/invalid/etc => get it it again
            {
                if (result[i].Level == getCurrentToken(cookieStackHead, 'level')) {
                    popToken(cookieStackHead, cookieName);
                    redirect(document.URL);
                    break;
                }
            }
        }

        else if (context.receivedStatus == "403") {
            for (var i = 0; i < result.length; i++)
                // if the token I sent is in the list of "accepted privileges" and I got forbidden => my token is NOT expired, but it is not of sufficient privilege Level (e.g. either i am not a 
                // valid hub liveid user, or i am not allowed to access due to policy) => redirect to next higher endpoint
            {
                if (result[i].Level == getCurrentToken(cookieStackHead, 'level') && i != result.length - 1)
                {
                    goToPrivilegeLevelIndex = i + 1;
                    break;
                }
                else if (result[i].Level == getCurrentToken(cookieStackHead, 'level')) // there is no higher endpoint e.g., there is only one level
                {
                    if(isDebugMode)
                        document.write("\nCurrent privilegeLevel is highest. But user does not match. ");
                    popToken(cookieStackHead, cookieName);
                    message = "User Access Denied for given " + result[i].Name + " user";
                    //alert(message);
                    redirect(GuiWebEndpointSuffix + "?message=" + encodeURIComponent(message));
                    return;
                }

            }
        }

        if (isDebugMode)
            document.write("\n redirecting to privilege level index= " + goToPrivilegeLevelIndex); 

        // if the current token is less than the required privilege get to the first-available endpoint
        handleRedirection(result, goToPrivilegeLevelIndex);
       

        
    }
}

function handleRedirection(privilegeLevels,i)
{
    
    if (document.domain.indexOf(GateKeeperDomain) !== -1)
        redirect(privilegeLevels[i].TokenEndpoint);

    if (isDebugMode)
        document.write("\n handling redirection for level " + i + " . checking status of  " + privilegeLevels[i].TokenEndpoint);

    $.ajax({
        type: "GET", //GET or POST or PUT or DELETE verb
        url: getStatusEndpoint() + "?url=" + encodeURI(privilegeLevels[i].TokenEndpoint), // Location of the service
        dataType: "JSON", //Expected data format from server
        processdata: true, //True or False
        cache: false,
        async : false,
        success: function (msg) {//On Successfull service call
            if(isDebugMode)
                document.write("\n URL status received : " + msg);

            if (msg == "true")
                redirect(privilegeLevels[i].TokenEndpoint);
            else if (msg == "false" && i < privilegeLevels.length - 1)
                handleRedirection(privilegeLevels, i + 1);
            else if (msg == "false" && i >= privilegeLevels.length - 1)
            {
                document.write("Redirecting .... Please Wait")
                redirect(privilegeLevels[i].TokenEndpoint);
            }
        },
        error: function (msg) {
            alert("Redirecting...");//Error in Reaching Status Endpoint. Attempting redirection.
            redirect(privilegeLevels[i].TokenEndpoint);
        },  
        
    });

    return "";
}


/*
function redirectIfOnline(redirectUrl, endpoints, currentIndex)
{
    var img = document.body.appendChild(document.createElement("img"));
    img.onload = function () {
        redirect(redirectUrl);
    };
    img.onerror = function () {
        redirect(endpoints[currentIndex + 1].TokenEndpoint); 
    };
    img.src = imageFileOnWeb;
}

    //setup ajax error handling
    $.ajaxSetup({


        beforeSend: function (req) {
            req.setRequestHeader('Authorization', HOMEOSTOKEN);
        },

        error: function (x, status, error) {
            if (x.status == 403) {
                alert("Sorry, your session has expired. Please login again to continue");
                window.location.href = "/Account/Login";
            }
            else {
                alert("An error occurred: " + status + "\nError: " + error);
            }
            
        }
    });

    */

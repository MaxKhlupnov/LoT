jQuery.support.cors = true;

//Global variables;
//var HOMEID;

$(document).ready(
    function () {
        //GetVersion();
        UpdateDebugInfo(this, "HomeStore loaded");
        //get the homeid
        //get installed applications and add them to the list
        GetApps();
    }

);


 function GetApps() {
 
    new PlatformServiceHelper().MakeServiceCall("webapp/GetApps", "", GetAppsCallback);
 }

 function GetAppsCallback(context, result) {

     UpdateDebugInfo(context, result);
     if (result[0] == "") {
         UpdateDebugInfo(context, "Apps returned");
         var htmlForApp;
         //Apps come in list with seven items per apps
         //AppName, Description, Rating, icon path, Compatible, Missing roles, installed
         var installed = false;
         var url = "";
         var missingRolesString = "";
         for (i = 1; i + 6 < result.length; i = i + 7) {
             //AppName:i,Description:i+1 Rating:i+2 icon:i+3 compatible:i+4, missing roles:i+5, installed:i+6
             url = buildAppConfigureURL(result[i], result[i + 6]);
             //stuff that is the same bring this back once straighten out installing instances
            //htmlForApp = '<div class="media_block col" onclick="GoToHTMLPage(' + "'" + url + "'" + ')"><a>' + result[i] + '</a><div class="app_desc">' + result[i + 1] + '</div><div class="app_rating"> Rating:' + result[i + 2] + "</div></div>"; 
             if (result[i + 6] == "True") { //INSTALLED?
                 //installed (and thus compatible)
                 htmlForApp = '<div class="media_block col">' + result[i] + '<div class="app_desc">' + result[i + 1] + '</div><div class="app_rating"> Rating:' + result[i + 2] + "</div></div>";
                 $("#installedCompat").append(htmlForApp);
             }
             else {  //NOT INSTALLED
                 if (result[i + 4] == "True") {  //COMPATIBLE?
                     htmlForApp = '<div class="media_block col" onclick="GoToHTMLPage(' + "'" + url + "'" + ')"><a>' + result[i] + '</a><div class="app_desc">' + result[i + 1] + '</div><div class="app_rating"> Rating:' + result[i + 2] + "</div></div>";
                     $("#notInstalledCompat").append(htmlForApp);
                 }
                 else {
                     //Not installed and missing roles
                     missingRolesString = result[i + 5];
                     if (result[i + 5].length > 0)
                         missingRolesString = result[i + 5].substr(1, (result[i + 5].length - 2));
                     htmlForApp = '<div class="media_block col">' + result[i] + '<div class="app_desc">' + result[i + 1] + '</div><div class="app_rating"> Rating:' + result[i + 2] + '</div><div class="app_reqs"> Missing:' + missingRolesString + "</div></div>";
                     $("#notInstalledNotCompat").append(htmlForApp);
                 }
             }
         } //END FOR LOOP
     }  //END IF result[0] empty
     else {
         UpdateDebugInfo(this, result);
     }
 }


     function buildAppConfigureURL(appName, installed) {
         var url = "InstallApp.html?AppName=" + appName + "&Installed=" + installed;
         return encodeURI(url);
     }

 

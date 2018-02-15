
$(document).ready(
    function () {
        new PlatformServiceHelper().MakeServiceCall("webapp/GetWeather", "", GetWeatherCallback);
    }
);

    
function ShowWeatherInfo() {
    new PlatformServiceHelper().MakeServiceCall("webapp/GetWeather", "", GetWeatherCallback);
}


function GetWeatherCallback(context, result) {
    var realResult = result.GetWeatherResult
    if (realResult[0] == "")
    {
        $("#WeatherText").html('');
        for (i = 1; i < realResult.length; i++) {
            $("#WeatherText").append(realResult[i] + "\n");
        }
    }
    else
    {
        $("#WeatherText").html('');
        $("#WeatherText").html('Got error while fetching weather data: ');
        $("#WeatherText").append(realResult[0]);
    }
}
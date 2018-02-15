/***
*  Digital Media HTML UI
@Author Maxim Khlupnov
*
**/
//run when document loads

$(document).ready(function () {

	$(".imageButton").kendoButton({
		click: function (e) {
			var elm = e.event.target;
			//  $(elm).addClass("k-state-hover");
			var slot = $(elm).attr("slot");
			var join = $(elm).attr("join");
			var value = $(elm).attr("value");

			SetDigitalMediaSignal(slot, join, value);
		}
		// imageUrl: "../GuiWeb/Assets/devices-icon.png"
	});

	//Set the hubs URL for the connection
	$.connection.hub.url = "http://localhost:8080/signalr";
	$.connection.hub.logging = true;
	/// // Declare a proxy to reference the SignalR Hub
	var eventHub = $.connection.digitalMediaHub;

	// Create a function that the hub can call to broadcast messages.
	eventHub.client.onConnectEvent = function () {
		$("#DigitalMediaList").append("OnConnectionEvent" + "&#13;&#10");
	};

	eventHub.client.onDigitalEvent = function (slot, join, value) {
		SetDigitalButtonState(slot, join, value);
		$("#DigitalMediaList").append("OnDigitalEvent slot=" + slot + ", join=" + join + ", value=" + value + "&#13;&#10");
	};

	eventHub.client.onDisconnectEvent = function (DisconnectReasonMessage) {
		$("#DigitalMediaList").append("onDisconnectEvent DisconnectReasonMessage: " + DisconnectReasonMessage + "&#13;&#10");
	};

	eventHub.client.onErrorEvent = function (ErrorMessage) {
		$("#DigitalMediaList").append("onErrorEvent ErrorMessage: " + ErrorMessage + "&#13;&#10");
	};

	// Start the connection.
	$.connection.hub
		.start(({ transport: ['webSockets', 'serverSentEvents', 'longPolling'] }))
		.done(function () {
			$("#DigitalMediaList").append("SignalR hub connected.");
		}).fail(function (reason) {
			$("#DigitalMediaList").append("SignalR connection failed: " + reason);
		});

});

function ShowDigitalMediaPortsInfo() {
	new PlatformServiceHelper().MakeServiceCall("webapp/SetDigitalSignal", "", MakeServiceCall("webapp/GetReceivedMessages", "", GetReceivedMessagesCallback));
}


function GetReceivedMessagesCallback(context, result) {
	$("#DigitalMediaList").html('');
	for (i = 0; i < result.length; i++);

	{
		$("#DigitalMediaList").append(result[i] + "&#13;&#10");
		//replace <br /> with &#13;   &#10 because that makes newlines in more browsers;
	}

}


function SetDigitalSignalCallback(context, result) {
	/// TODO: Change state of the button for (i = 0; i < result.length; i++);

	{
		var setDigitalData = JSON.parse(context.Data);
		SetDigitalButtonState(setDigitalData.slot, setDigitalData.join, result[0] != "True");
		//  $("#DigitalMediaList").append(result[i] + "&#13;&#10");
	}

}

function SetDigitalMediaSignal(slot, join, value) {
	new PlatformServiceHelper()
		.MakeServiceCall("webapp/SetDigitalSignal", '{"slot": "' + slot + '","join": "' + join + '","value": "High"}',
			SetDigitalSignalCallback);
}

function SetDigitalButtonState(slot, join, isOn) {
	var buttons = $(".imageButton")
			.filter("[join='" + join + "']")
			.filter("[slot='" + slot + "']").each(function (index, elm) {
				if (isOn) {
					$(elm).addClass("k-state-hover");
				} else {
					$(elm).removeClass("k-state-hover");
				}
			});
}


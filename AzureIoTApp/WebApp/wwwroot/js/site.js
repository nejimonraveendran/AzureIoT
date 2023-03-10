"use strict"

var buttonId = '#btn-on-off';
var logoId = '#img-logo';

$(document).ready(function () {
    setUiToUnknownState();

    callApi('/appliance/status', 'GET', null, setUiStates);


    $(buttonId).click(event => {
        event.preventDefault();
        callApi('/appliance/status/toggle', 'POST', null, setUiStates);
    });
    
});

///appliance/status/toggle
function callApi(path, verb, payload, callback) {
    $.ajax({
        type: verb,
        url: path,
        data: payload,
        success: function (response) {
            callback(response);
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log(textStatus, errorThrown);
        }
    });
}


function setUiStates(status) {
    switch (status) {
        case 1: //appliance on
            setUiToOnState();
            break;
        case 0: //appliance off
            setUiToOffState();
            break;
        default: //unknown state
            setUiToUnknownState();
            break;
    }
}

function setUiToUnknownState() {
    $(buttonId).removeClass('btn-on');
    $(buttonId).removeClass('btn-off');
    $(buttonId).addClass('btn-waiting');
    $(buttonId).html("Can't reach Device, try refreshing!");
    $(logoId).attr('src', '/img/bulb-off.png');
}

function setUiToOnState() {
    $(buttonId).removeClass('btn-waiting');
    $(buttonId).removeClass('btn-off');
    $(buttonId).addClass('btn-on');
    $(buttonId).html("TURN OFF");
    $(logoId).attr('src', '/img/bulb-on.png');
}

function setUiToOffState() {
    $(buttonId).removeClass('btn-waiting');
    $(buttonId).removeClass('btn-on');
    $(buttonId).addClass('btn-off');
    $(buttonId).html("TURN ON");
    $(logoId).attr('src', '/img/bulb-off.png');
}


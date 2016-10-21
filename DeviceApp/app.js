'use strict';
var config = require('./config');
var firmwareManager = require('./firmware');
var Client = require('azure-iot-device').Client;
var Protocol = require('azure-iot-device-mqtt').Mqtt;

var connectionString = config.getConnectionString();
var client = Client.fromConnectionString(connectionString, Protocol);
firmwareManager.setClient(client);

function onWriteLine(request, response) {
    console.log(request.payload);

    response.send(200, 'Input was written to log.', function (err) {
        if (err) {
            console.error('An error ocurred when sending a method response:\n' + err.toString());
        } else {
            console.log('Response to method \'' + request.methodName + '\' sent successfully.');
        }
    });
}

function handleTwinChange (twin, desiredChange) {
    console.log("received changes: " + JSON.stringify(desiredChange));

    var patch = {};
    Object.keys(desiredChange).forEach(function(key) {   

        if (key && key.indexOf('$') == 0) {
            return;
        }

        if (key && key.indexOf('ignore') == -1) {
            patch[key] = desiredChange[key];
        }
        else {
            console.log('desired property ' + key + ' has been ignored');
        }
    });

    console.log("reporting changes: " + JSON.stringify(patch));
                    
    twin.properties.reported.update(patch, function (err) {
        if (err) {
            console.error('could not update twin');
        } 
        else {
            console.log('twin state reported');
        }
    });
}

function printResultFor(op) {
  return function printResult(err, res) {
    if (err) console.log(op + ' error: ' + err.toString());
    if (res) console.log(op + ' status: ' + res.constructor.name);
  };
}

client.open(function (err) {
    if (err) {
        console.error('could not open IotHub client');
    } else {
        console.log('client opened');

        client.onDeviceMethod('writeLine', onWriteLine);
        client.onDeviceMethod('firmwareUpdate', firmwareManager.onFirmwareUpdate);
        
        client.on('message', function (msg) {
            console.log('received message from cloud. Id: ' + msg.messageId + ' Body: ' + msg.data);
            client.complete(msg, printResultFor('completed'));
        });
 
        client.getTwin(function (err, twin) {
            if (err) {
                console.error('could not get twin');
            } 
            else {
                console.log('retrieved device twin');
                
                twin.on('properties.desired', function (desiredChange) {
                    handleTwinChange(twin, desiredChange);
                });
            }
        });
    }
});
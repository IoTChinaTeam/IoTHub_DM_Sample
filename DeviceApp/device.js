'use strict';
var Message = require('azure-iot-device').Message;

var Device = function (hostName, deviceId, key, info) {
    var Client = require('azure-iot-device').Client;
    var Protocol = require('azure-iot-device-mqtt').Mqtt;
    
    this.client = Client.fromConnectionString(this.getConnectionString(hostName, deviceId, key), Protocol);
    this.logger = require('./logger').create(deviceId);
    this.firmwareManager = require('./firmware').create(this.client, this.logger);
    this.info = info;

    this.logger.log("device created");
};

Device.create = function(hostName, deviceId, key, info) {
    return new Device(hostName, deviceId, key, info);
};

Device.prototype.getConnectionString = function (hostName, deviceId, key) {
    var domain = '.azure-devices.net';

    if (hostName.indexOf(domain, hostName.length - domain.length) == -1) {
        hostName += domain;
    }

    return 'HostName=' + hostName + ';DeviceId=' + deviceId + ';SharedAccessKey=' + key;
};

Device.prototype.onWriteLine = function(request, response) {
    var self = this;
    this.logger.log('writeLine: ' + JSON.stringify(request.payload));

    response.send(200, 'Input was written to log.', function (err) {
        if (err) {
            self.logger.error('An error ocurred when sending a method response:\n' + err.toString());
        } else {
            self.logger.log('Response to method \'' + request.methodName + '\' sent successfully.');
        }
    });
};

Device.prototype.handleTwinChange = function(twin, desiredChange) {
    var self = this;
    this.logger.log("received changes: " + JSON.stringify(desiredChange));

    var patch = {};
    Object.keys(desiredChange).forEach(function(key) {   

        if (key && key.indexOf('$') == 0) {
            return;
        }

        if (key && key.indexOf('ignore') == -1) {
            patch[key] = desiredChange[key];
        }
        else {
            self.logger.log('desired property ' + key + ' has been ignored');
        }
    });

    this.logger.log("reporting changes: " + JSON.stringify(patch));
                    
    twin.properties.reported.update(patch, function (err) {
        if (err) {
            self.logger.error('could not update twin: ' + err.message);
        } 
        else {
            self.logger.log('twin state reported');
        }
    });
};

Device.prototype.printResultFor = function(op) {
    var self = this;
    return function printResult(err, res) {
        if (err) self.logger.log(op + ' error: ' + err.toString());
        if (res) self.logger.log(op + ' status: ' + res.constructor.name);
    };
};

Device.prototype.messageProcesser = function(msg) {
    this.logger.log('received message: ' + msg.data);
    this.client.complete(msg, this.printResultFor('completed'));
};

Device.prototype.send = function(data) {
    var message = new Message(data);
    this.logger.log("Sending message");
    this.client.sendEvent(message, this.printResultFor('send'));
};

Device.prototype.sendUpdateDevice = function(data) {
    if (this.info) {
        this.info.SystemProperties = null;
        this.info.Version = "1.0";
        this.info.ObjectType = "DeviceInfo";
        this.info.Commands = this.getCommands();
        var data = JSON.stringify(this.info);
        this.send(data);
    }
};

Device.prototype.getCommands = function(data) {
    return [
        { "Name": "PingDevice", "Parameters": [] }, 
        { "Name": "StartTelemetry", "Parameters": [] }, 
        { "Name": "StopTelemetry", "Parameters": [] }, 
        { "Name": "ChangeSetPointTemp", "Parameters": [{ "Name": "SetPointTemp", "Type": "double" }] }, 
        { "Name": "DiagnosticTelemetry", "Parameters": [{ "Name": "Active", "Type": "boolean" }] }, 
        { "Name": "ChangeDeviceState", "Parameters": [{ "Name": "DeviceState", "Type": "string" }] },
        { "Name": "Test", "Parameters": [{ "Name": "TestParameter", "Type": "string" }] }
    ];
};

Device.prototype.run = function() {
    var self = this;
    this.client.open(function (err) {
        if (err) {
            self.logger.error('could not open IotHub client');
        } else {
            self.logger.log('client opened');

            self.client.onDeviceMethod('writeLine', self.onWriteLine.bind(self));
            self.client.onDeviceMethod('firmwareUpdate', self.firmwareManager.onFirmwareUpdate.bind(self.firmwareManager));
            
            self.client.on('message', function (msg) {
                self.logger.log('received message from cloud. Id: ' + msg.messageId + ' Body: ' + msg.data);
                self.client.complete(msg, self.printResultFor('completed'));
            });

            self.client.on('error', function (err) {
                self.logger.error('error:' + err);
            });
    
            self.client.getTwin(function (err, twin) {
                if (err) {
                    self.logger.error('could not get twin');
                } 
                else {
                    self.logger.log('retrieved device twin');
                    
                    twin.on('properties.desired', function (desiredChange) {
                        self.handleTwinChange(twin, desiredChange);
                    });
                }
            });

            self.sendUpdateDevice();
        }
    });
};

module.exports = Device;
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

    this.sendMethodResponse(response, 200, 'Input was written to log.');   
};

Device.prototype.onTest = function(request, response) {
    var self = this;
    this.logger.log('test method: ' + JSON.stringify(request.payload));

    if (request.payload && request.payload.TestParameter) {
        this.sendMethodResponse(response, 200, { message: "OK" });
    }
    else {
        this.sendMethodResponse(response, 500, { message: "Error: Invalid parameter" });
    }
};

Device.prototype.sendMethodResponse = function(response, code, payload) {
    response.send(code, payload, function (err) {
        if (err) {
            self.logger.error('An error ocurred when sending a method response:\n' + err.toString());
        } else {
            self.logger.log('Response to method sent successfully.');
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
        this.initDeviceInfo();
        this.info.SystemProperties = null;
        this.info.CommandHistory = [];
        this.info.Version = "1.0";
        this.info.ObjectType = "DeviceInfo";
        this.info.Commands = this.getCommands();
        var data = JSON.stringify(this.info);
        this.logger.log("sending 'DeviceInfo' message");
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
        { "Name": "Test", "DeliveryType": "Method", "Parameters": [{ "Name": "TestParameter", "Type": "string" }] }
    ];
};
Device.prototype.initDeviceInfo = function() {
    if (this.info && this.info.DeviceProperties.HubEnabledState == null) {
        var randomId = Math.floor(Math.random() * 10000); 
        this.info.DeviceProperties.HubEnabledState = true;
        this.info.DeviceProperties.Manufacturer = "Contoso Inc.";
        this.info.DeviceProperties.ModelNumber = "MD-" + randomId;
        this.info.DeviceProperties.SerialNumber = "SER" + randomId;
        this.info.DeviceProperties.FirmwareVersion = "1." + randomId;
        this.info.DeviceProperties.Platform = "Plat-" + randomId;
        this.info.DeviceProperties.Processor = "i3-" + randomId;
        this.info.DeviceProperties.InstalledRAM = randomId + " MB";
    }
};

Device.prototype.run = function() {
    var self = this;
    this.client.open(function (err) {
        if (err) {
            self.logger.error('could not open IotHub client');
        } else {
            self.logger.log('client opened');

            self.client.onDeviceMethod('Test', self.onTest.bind(self));
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
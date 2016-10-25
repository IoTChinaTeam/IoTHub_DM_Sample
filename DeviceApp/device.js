'use strict';

var Device = function (hostName, deviceId, key) {
    var Client = require('azure-iot-device').Client;
    var Protocol = require('azure-iot-device-mqtt').Mqtt;

    this.client = Client.fromConnectionString(this.getConnectionString(hostName, deviceId, key), Protocol);
    this.logger = require('./logger').create(deviceId);
    this.firmwareManager = require('./firmware').create(this.client, this.logger);

    this.logger.log("device created");
};

Device.create = function(hostName, deviceId, key) {
    return new Device(hostName, deviceId, key);
};

Device.prototype.getConnectionString = function (hostName, deviceId, key) {
    return 'HostName=' + hostName + '.azure-devices.net;DeviceId=' + deviceId + ';SharedAccessKey=' + key;
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
            self.logger.error('could not update twin');
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
        }
    });
};

module.exports = Device;
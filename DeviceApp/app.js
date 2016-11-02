'use strict';

var config = require('./config');
var logger = require('./logger').create("Main");
var fork = require('child_process').fork;
var debug = typeof v8debug === 'object';
var devices = {};

if (process.argv.length == 5) {
    // run device in main process
    var setting = config.parseFromArgv();
    var device = require('./device').create(setting.hostName, setting.deviceId, setting.key);
    device.run();
}
else if (process.argv.length == 3) {
    loadFromConfigFile(process.argv[2]); 
}
else {
    console.log("Command: node app.js <HostName> <DeviceId> <Key>");
    console.log("Command: node app.js <devices json file path or app setting file>");
    process.exit();
}

process.on('SIGINT', function(){
    logger.log('exiting');
    stopDevices();
    logger.log('waiting for child devices to be closed');
    setTimeout(function(){
        process.exit();
    }, 2000);
});

function loadFromConfigFile(path){
    var fs = require('fs');
    fs.readFile(path, 'utf8', function (err, data) {
        if (err) throw err;
        var setting = JSON.parse(data);
        if (setting.devices){
            createDevicesFromFile(setting);
        }
        else {
            createDevicesFromTable(setting);
        }
    });
}

function createDevicesFromFile(setting) {
    setting.devices.forEach(function(item) {
        createDevice(setting.hostName, item.deviceId, item.key);
    });
}

function createDevicesFromTable(setting) {
    
    var hostName = setting.iotHubName;
    var storage = require('./storage.js').create(logger, setting.serviceStoreAccountConnectionString);
    var docDb = require('./docDb.js').create(logger, setting.docDbEndPoint, setting.docDbKey, setting.docDbDatabaseId, setting.docDbDocumentCollectionId);

    storage.getDeviceList(function(deviceList){
        if (deviceList) {
            var newDeviceIds = deviceList.filter(function(device){
                return devices[device.deviceId] == null;
            }).map(function(device){
                return device.deviceId;
            });
            var removedDeviceIds = Object.keys(devices).filter(function(deviceId){
                return deviceList.filter(function(device){ 
                    return device.deviceId == deviceId
                }).length == 0;
            });

            logger.log("%d new devices found, %d devices removed", newDeviceIds.length, removedDeviceIds.length);

            //stop removed device
            removedDeviceIds.forEach(function(deviceId){
                removeDevice(deviceId);
            });
            //start new devices
            deviceList.forEach(function(device) {
                if(newDeviceIds.indexOf(device.deviceId) > -1) {
                    docDb.getDevice(device.deviceId, function(deviceInfo) {
                        createDevice(hostName, device.deviceId, device.key, deviceInfo);
                    }); 
                }
            });

            setTimeout(function(){
                createDevicesFromTable(setting)
            },20000);
        }
    });
}

function createDevice(hostName, deviceId, key, info) {
    // run device in child process
    logger.log('creating device %s', deviceId);
    var parameters = [ hostName, deviceId, key ];
    if (info) {
        var infoString = JSON.stringify(info);
        parameters.push(infoString);
    }
    
    var device = fork('./child.js', parameters, debug ? { execArgv: ['--debug=' + getRandomPort()] } : null);
    devices[deviceId] = device;
    device.on('close', function (code) {
        if (code == 1) {
            logger.error('device %s crashed unexpectedly, restarting the device...', deviceId);
            setImmediate(function(){
                createDevice(hostName, deviceId, key, info);
            });
        }
        else {
            logger.log('device %s stopped, code: %d', deviceId, code);
        }
    });
    device.on('message', (m) => {
        logger.log('got message:', m);
    });
}

function removeDevice(deviceId){
    logger.log('stoping device %s', deviceId);
    devices[deviceId].kill();
    delete devices[deviceId];
}
function stopDevices(){
    Object.keys(devices).forEach(function(deviceId) {
        removeDevice(deviceId);
    });
}

function getRandomPort() { 
    var max = 10000;
    var min = 5000;
    return Math.floor(Math.random() * (max - min + 1)) + min; 
} 


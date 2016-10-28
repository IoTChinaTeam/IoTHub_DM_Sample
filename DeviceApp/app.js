'use strict';

var config = require('./config');
var logger = require('./logger').create("Main");
var fork = require('child_process').fork;
var debug = typeof v8debug === 'object';
var devices = [];

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
    storage.getDeviceList(function(devices){
        if (devices) {
            devices.forEach(function(device) {
                docDb.getDevice(device.deviceId, function(deviceInfo) {
                    createDevice(hostName, device.deviceId, device.key, deviceInfo);
                }); 
            });
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
    devices.push(device);
    device.on('close', function (code) {
        logger.log('child process exited with code ' + code);
    });
    device.on('message', (m) => {
        logger.log('got message:', m);
    });
}

function stopDevices(){
    devices.forEach(function(device) {
        device.kill();
    });
}

function getRandomPort() { 
    var max = 10000;
    var min = 5000;
    return Math.floor(Math.random() * (max - min + 1)) + min; 
} 


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
    var fs = require('fs');
    fs.readFile(process.argv[2], 'utf8', function (err, data) {
        if (err) throw err;
        var setting = JSON.parse(data);
        setting.devices.forEach(function(item) {
            createDevice(setting.hostName, item.deviceId, item.key);
        });
    });
}
else {
    console.log("Command: node app.js <HostName> <DeviceId> <Key>");
    console.log("Command: node app.js <devices json file path>");
    process.exit();
}

process.on('SIGINT', function(){
    logger.log('exiting');
    stopDevices();
});

function createDevice(hostName, deviceId, key) {
    // run device in child process
    logger.log('creating device %s', deviceId);
    var device = fork('./child.js', [ hostName, deviceId, key ], debug ? { execArgv: ['--debug'] } : null);
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

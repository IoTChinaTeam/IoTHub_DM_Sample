'use strict';

var config = {};

config.parseFromArgv = function() {
    if (process.argv.length == 5) {
        return {
            hostName: process.argv[2],
            deviceId: process.argv[3],
            key: process.argv[4]
        };
    }
};

config.getConnectionString = function() {

    var setting = config.parseFromArgv();
    if (setting) {
        console.log('HostName: ' + setting.hostName + ' DeviceId: ' + setting.deviceId + ' Key: ' + setting.key);    

        return 'HostName=' + setting.hostName + '.azure-devices.net;DeviceId=' + setting.deviceId + ';SharedAccessKey=' + setting.key;
    }
    else {
        console.log("Command: node app.js <HostName> <DeviceId> <Key>");
        process.exit();
    }  
};

module.exports = config;
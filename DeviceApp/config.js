var config = {};

config.getConnectionString = function() {
    var hostName;
    var deviceId;
    var key;
    if (process.argv.length == 5) {
        var hostName = process.argv[2];
        var deviceId = process.argv[3];
        var key = process.argv[4];

        console.log('HostName: ' + hostName + ' DeviceId: ' + deviceId + ' Key: ' + key);    

        return 'HostName=' + hostName + '.azure-devices.net;DeviceId=' + deviceId + ';SharedAccessKey=' + key;
    }
    else {
        console.log("Command: node app.js <HostName> <DeviceId> <Key>");
        process.exit();
    }  
}
module.exports = config;
var config = {};

config.defaultHostName = 'DarrenIotHub';
config.defaultDeviceId = 'myDeviceId1';
config.defaultKey = 'OJZ4txH2GDJ+of9e0UVk3r05GXCphLx+2AsKdtLK6bU=';

config.getConnectionString = function() {
    var hostName;
    var deviceId;
    var key;
    if (process.argv.length == 5) {
        hostName = process.argv[2];
        deviceId = process.argv[3];
        key = process.argv[4];
    }
    else {
        hostName = config.defaultHostName;
        deviceId = config.defaultDeviceId;
        key = config.defaultKey;
    }

    console.log('HostName: ' + hostName + ' DeviceId: ' + deviceId + ' Key: ' + key);    

    return 'HostName=' + hostName + '.azure-devices.net;DeviceId=' + deviceId + ';SharedAccessKey=' + key;
}
module.exports = config;
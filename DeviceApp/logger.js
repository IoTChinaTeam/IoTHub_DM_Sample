'use strict';

var Logger = function(deviceId) {
    this.deviceId = deviceId;
};

Logger.create = function (deviceId) {
    return new Logger(deviceId);
};

Logger.prototype.addDefaultMessage = function (args) {
    if (args && args.length > 0) {
        var t = new Date().toISOString().replace(/T/, ' ').replace(/\..+/, '');
        args[0] = t + " [" + this.deviceId + "] " + args[0];
    }
};

Logger.prototype.log = function() {
    this.addDefaultMessage(arguments);
    console.log.apply(this, arguments)
};

Logger.prototype.error = function() {
    this.addDefaultMessage(arguments);
    console.error.apply(this, arguments)
}

module.exports = Logger;
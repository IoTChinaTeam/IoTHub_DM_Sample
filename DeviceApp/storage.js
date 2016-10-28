'use strict';

var Storage = function(logger, connectionString) {
    this.connectionString = connectionString;
    this.logger = logger;
    this.azure = require('azure-storage');
    this.tableSvc = this.azure.createTableService(this.connectionString);
};

Storage.create = function (logger, connectionString) {
    return new Storage(logger, connectionString);
};

Storage.prototype.getDeviceList = function (callback) {
    var self = this;
    this.tableSvc.queryEntities("DeviceList", null, null, function (err, result) { 
        if (err) {
            self.logger.error("query error: " + err);
        }

        var devices = [];
        if (result.entries) {
            result.entries.forEach(function(item){
                devices.push({
                    deviceId: item.PartitionKey._,
                    key: item.Key._
                });
            });
        }
        
        callback(devices);
    }); 

};

module.exports = Storage;
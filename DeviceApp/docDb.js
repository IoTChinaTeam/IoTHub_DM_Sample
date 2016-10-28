'use strict';

var DocDb = function(logger, endpoint, key, databaseId, collectionId) {
    this.logger = logger;
    var documentClient = require("documentdb").DocumentClient;
    this.client = new documentClient(endpoint, { "masterKey": key });
    this.collectionUrl = `dbs/${databaseId}/colls/${collectionId}`;
};

DocDb.create = function (logger, endpoint, key, databaseId, collectionId) {
    return new DocDb(logger, endpoint, key, databaseId, collectionId);
};

DocDb.prototype.getDevice = function (deviceId, callback) {
    var documentUrl =  `${this.collectionUrl}/docs/${deviceId}`;
    var self = this;
    var querySpec = { 
        query: 'SELECT * FROM d where d.DeviceProperties.DeviceID = @deviceId', 
        parameters: [ {name: "@deviceId", value: deviceId}]
    };

    this.client.queryDocuments(this.collectionUrl, querySpec).toArray(function (err, results) {
        if (err) {
            self.logger.error("get document error: " + err.message);
        } else {
            if (results.length == 1) {
                callback(results[0]);
            }
        }
    });
};

module.exports = DocDb;


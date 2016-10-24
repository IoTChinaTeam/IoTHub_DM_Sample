// This is a modified sample code  from https://azure.microsoft.com/en-us/documentation/articles/iot-hub-firmware-update/
'use strict';

var FirmwareManager = function (client, logger) {
  this.client = client;
  this.logger = logger;
};

FirmwareManager.create = function (client, logger) {
  return new FirmwareManager(client, logger);
};

FirmwareManager.prototype.reportFWUpdateThroughTwin = function(twin, firmwareUpdateValue) {
  var patch = {
      iothubDM : {
        firmwareUpdate : firmwareUpdateValue
      }
  };

  var self = this;
  twin.properties.reported.update(patch, function(err) {
    if (err) throw err;
    self.logger.log('twin state reported - ' + firmwareUpdateValue.status);
  });
};

FirmwareManager.prototype.simulateDownloadImage = function(imageUrl, callback) {
  var error = null;
  var image = "[fake image data]";

  this.logger.log("Downloading image from " + imageUrl);

  callback(error, image);
};

FirmwareManager.prototype.simulateApplyImage = function(imageData, callback) {
  var error = null;

  if (!imageData) {
    error = {message: 'Apply image failed because of missing image data.'};
  }

  callback(error);
};

FirmwareManager.prototype.waitToDownload = function(twin, fwPackageUriVal, callback) {
  var now = new Date();

  this.reportFWUpdateThroughTwin(twin, {
    fwPackageUri: fwPackageUriVal,
    status: 'waiting',
    error : null,
    startedWaitingTime : now.toISOString()
  });
  setTimeout(callback, 4000);
};

FirmwareManager.prototype.downloadImage = function(twin, fwPackageUriVal, callback) {
  var now = new Date();   
  var self = this;
  this.reportFWUpdateThroughTwin(twin, {
    status: 'downloading',
  });

  setTimeout(function() {
    // Simulate download
    self.simulateDownloadImage(fwPackageUriVal, function(err, image) {

      if (err)
      {
        self.reportFWUpdateThroughTwin(twin, {
          status: 'downloadfailed',
          error: {
            code: error_code,
            message: error_message,
          }
        });
      }
      else {        
        self.reportFWUpdateThroughTwin(twin, {
          status: 'downloadComplete',
          downloadCompleteTime: now.toISOString(),
        });

        setTimeout(function() { callback(image); }, 4000);   
      }
    });

  }, 4000);
};

FirmwareManager.prototype.applyImage = function(twin, imageData, callback) {
  var now = new Date();   
  var self = this;
  this.reportFWUpdateThroughTwin(twin, {
    status: 'applying',
    startedApplyingImage : now.toISOString()
  });

  setTimeout(function() {

    // Simulate apply firmware image
    self.simulateApplyImage(imageData, function(err) {
      if (err) {
        self.reportFWUpdateThroughTwin(twin, {
          status: 'applyFailed',
          error: {
            code: err.error_code,
            message: err.error_message,
          }
        });
      } else { 
        self.reportFWUpdateThroughTwin(twin, {
          status: 'applyComplete',
          lastFirmwareUpdate: now.toISOString()
        });    

      }
    });

    setTimeout(callback, 4000);

  }, 4000);
};

FirmwareManager.prototype.onFirmwareUpdate = function(request, response) {

  var self = this;
  // Respond the cloud app for the direct method
  response.send(200, 'FirmwareUpdate started', function(err) {
    if (!err) {
      self.logger.error('An error occured when sending a method response:\n' + err.toString());
    } else {
      self.logger.log('Response to method \'' + request.methodName + '\' sent successfully.');
    }
  });

  // Get the parameter from the body of the method request
  var fwPackageUri = request.payload.fwPackageUri;

  // Obtain the device twin
  self.client.getTwin(function(err, twin) {
    if (err) {
      self.logger.error('Could not get device twin.');
    } else {
      self.logger.log('Device twin acquired.');

      // Start the multi-stage firmware update
      self.waitToDownload(twin, fwPackageUri, function() {
        self.downloadImage(twin, fwPackageUri, function(imageData) {
          self.applyImage(twin, imageData, function() {});    
        });  
      });
    }
  });
};


module.exports = FirmwareManager;

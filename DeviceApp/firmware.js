// This is a modified sample code  from https://azure.microsoft.com/en-us/documentation/articles/iot-hub-firmware-update/

var client;

var reportFWUpdateThroughTwin = function(twin, firmwareUpdateValue) {
  var patch = {
      iothubDM : {
        firmwareUpdate : firmwareUpdateValue
      }
  };

  twin.properties.reported.update(patch, function(err) {
    if (err) throw err;
    console.log('twin state reported - ' + firmwareUpdateValue.status);
  });
};

var simulateDownloadImage = function(imageUrl, callback) {
  var error = null;
  var image = "[fake image data]";

  console.log("Downloading image from " + imageUrl);

  callback(error, image);
}

var simulateApplyImage = function(imageData, callback) {
  var error = null;

  if (!imageData) {
    error = {message: 'Apply image failed because of missing image data.'};
  }

  callback(error);
}

var waitToDownload = function(twin, fwPackageUriVal, callback) {
  var now = new Date();

  reportFWUpdateThroughTwin(twin, {
    fwPackageUri: fwPackageUriVal,
    status: 'waiting',
    error : null,
    startedWaitingTime : now.toISOString()
  });
  setTimeout(callback, 4000);
};

var downloadImage = function(twin, fwPackageUriVal, callback) {
  var now = new Date();   

  reportFWUpdateThroughTwin(twin, {
    status: 'downloading',
  });

  setTimeout(function() {
    // Simulate download
    simulateDownloadImage(fwPackageUriVal, function(err, image) {

      if (err)
      {
        reportFWUpdateThroughTwin(twin, {
          status: 'downloadfailed',
          error: {
            code: error_code,
            message: error_message,
          }
        });
      }
      else {        
        reportFWUpdateThroughTwin(twin, {
          status: 'downloadComplete',
          downloadCompleteTime: now.toISOString(),
        });

        setTimeout(function() { callback(image); }, 4000);   
      }
    });

  }, 4000);
};

var applyImage = function(twin, imageData, callback) {
  var now = new Date();   

  reportFWUpdateThroughTwin(twin, {
    status: 'applying',
    startedApplyingImage : now.toISOString()
  });

  setTimeout(function() {

    // Simulate apply firmware image
    simulateApplyImage(imageData, function(err) {
      if (err) {
        reportFWUpdateThroughTwin(twin, {
          status: 'applyFailed',
          error: {
            code: err.error_code,
            message: err.error_message,
          }
        });
      } else { 
        reportFWUpdateThroughTwin(twin, {
          status: 'applyComplete',
          lastFirmwareUpdate: now.toISOString()
        });    

      }
    });

    setTimeout(callback, 4000);

  }, 4000);
};

var onFirmwareUpdate = function(request, response) {

  // Respond the cloud app for the direct method
  response.send(200, 'FirmwareUpdate started', function(err) {
    if (!err) {
      console.error('An error occured when sending a method response:\n' + err.toString());
    } else {
      console.log('Response to method \'' + request.methodName + '\' sent successfully.');
    }
  });

  // Get the parameter from the body of the method request
  var fwPackageUri = request.payload.fwPackageUri;

  // Obtain the device twin
  client.getTwin(function(err, twin) {
    if (err) {
      console.error('Could not get device twin.');
    } else {
      console.log('Device twin acquired.');

      // Start the multi-stage firmware update
      waitToDownload(twin, fwPackageUri, function() {
        downloadImage(twin, fwPackageUri, function(imageData) {
          applyImage(twin, imageData, function() {});    
        });  
      });

    }
  });
};
var setClient = function (clientInstance) {
    client = clientInstance;
}

module.exports = { 
    onFirmwareUpdate: onFirmwareUpdate,
    setClient: setClient
};

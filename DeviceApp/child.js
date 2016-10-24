'use strict';

var config = require('./config');
var Device = require('./device');

var setting = config.parseFromArgv();
var device = new Device(setting.hostName, setting.deviceId, setting.key);
device.run();
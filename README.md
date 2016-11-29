# IoTHub_DM_Sample
A sample application for testing Device management feature of Azure IoT Hub


## DeviceApp
Device sample App using Nodejs 
Usage:

    node app.js <IoTHub name> <deviceId> <device key>

## IoTHubConsole
Service App to manage device using c#
Usage:

    >IoTHubConsole.exe
    /Action:{AddDevices|DeleteDevices|QueryDevices|UpdateTwin|ScheduleTwinUpdate|InvokeMethod|ScheduleDeviceMethod|QueryJobs|QueryJobSummary|CancelJobs|SendMessage}
    /Ids:<string>                   DeviceId or JobId. (short form /d)
    /QueryCondition:<string>        Query condition for query/schedule job actions (short form /q)
    /Names:<string>                 The name of tag, property or method (short form /n)
    /Values:<string>                The value of tag/property, or parameter in JSON for the method (short form /v)
    /StartOffsetInSeconds:<int>	    Default value:'0' (short form /o)
    /TimeoutInSeconds:<int>         Default value:'3600' (short form /t)
    /C2DMessage:<string>            (short form /m)
    
    

Example 1: Create four devices in the target IoT Hub, with tag ‘OEMName’ and desired property ‘fwversion’

    IoTHubConsole /a:AddDevices /d:Dev1 /d:Dev2 /d:Dev3 /d:Dev4 /n:tags.OEMName /v:"ACME Inc." /n:fwversion /v:0.9

Example 2: Query for all devices

    IoTHubConsole /a:QueryDevices
	
Example 3: Query for devices with tag city = beijing

    IoTHubConsole /a:QueryDevices /q:"SELECT * FROM devices WHERE tags.city='beijing'"

Example 4: Update tag location.city = shanghai for given devices

    IoTHubConsole /a:UpdateTwin /n:tags.location.city /v:shanghai /d:Dev1 /d:Dev2

Example 5: Schdule job to update device tag location.city = shanghai for devices which OEMName = ACME Inc.

    IoTHubConsole /a:ScheduleTwinUpdate /q:"tags.OEMName='ACME Inc.'" /n:tags.location.city /v:shanghai

Example 6: Query for all jobs

    IoTHubConsole /a:QueryJobs

Example 7: Query job for given devices

    IoTHubConsole /a:QueryJobs /q:"SELECT * FROM devices.jobs WHERE devices.jobs.deviceId IN ('Dev1', 'Dev2')"

Example 8: Query all failed twin updating jobs

    IoTHubConsole /a:QueryJobSummary /q:"{'Type': 'ScheduleUpdateTwin', 'Status': 'failed'}"

## Binary
Binary of IoTHubConsole
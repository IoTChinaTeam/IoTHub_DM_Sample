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
    /Action:{AddDevices|DeleteDevices|QueryDevices|UpdateTwin|ScheduleTwinUpdate|InvokeMethod|ScheduleDeviceMethod|QueryJobs|CancelJobs|SendMessage}  
    /DeviceIds:<string>       
    /QueryCondition:<string>  
    /Names:<string>           The name of tag, property or method (short form /n)
    /Values:<string>          The value of tag/property, or parameter in JSON for the method (short form /v)
    /StartOffsetInSeconds:<int>	Default value:'0' (short form /o)
    /TimeoutInSeconds:<int>         Default value:'3600' (short form /t)
    /C2DMessage:<string>            (short form /m)
    
    

Exampe: Create four devices in the target IoT Hub, with tag ‘OEMName’ and desired property ‘fwversion’

    IoTHubConsole /a:AddDevices /d:Dev1 /n:tags.OEMName /v:"ACME Inc." /n:fwversion /v:0.9


## Binary
Binary of IoTHubConsole



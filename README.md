# NukiBridge2Mqtt
 Connects Nuki Bridges to MQTT

## Installation
Extract the release build to a folder and run `Net.Bluewalk.NukiBridge2Mqtt.Service.exe --install`
This will install the service

## Configuration
Edit the `Net.Bluewalk.NukiBridge2Mqtt.Service.exe.config` file and set the following settings accordingly under 
```
<configuration>
  <appSettings>
    <!-- Address of your MQTT broker-->
    <add key="MQTT_Host" value="127.0.0.1" />
    <!-- Port of your MQTT broker (1883 if left empty)-->
    <add key="MQTT_Port" value="" />
    <!-- Root topic for MQTT messages (nukibridge when left empty) -->
    <add key="MQTT_RootTopic" value="" />
    <!-- IP Address for the Nuki Bridge callbacks (leave empty for auto detection) -->
    <add key="Bridge_Callback_Address" value="" />
    <!-- Port for the Nuki Bridge callbacks (8080 when left empty) -->
    <add key="Bridge_Callback_Port" value="" />
    <!-- Url of your bridge (http://xxx.xxx.xxx:port) -->
    <add key="Bridge_URL" value="http://192.168.1.10:8080" />
    <!-- Token to utilize the Nuki Bridge API -->
    <add key="Bridge_Token" value="" />     
    <!-- Use hashed token instead of plain token (true when left empty) -->
    <add key="Bridge_HashToken" value=""/>
  </appSettings>
  ```

| Configuration setting | Description | Default when empty |
|-|-|-|
| MQTT_Host | IP address / DNS of the MQTT broker | - |
| MQTT_Port | Port of the MQTT broker | `1883` |
| MQTT_RootTopic | This text will be prepended to the MQTT Topic | `nukibridge` |
| Bridge_Callback_Address | IP Address for the Nuki Bridge callbacks | Auto detection |
| Bridge_Callback_Port | Port for the Nuki Bridge callbacks | `8080` |
| Bridge_URL | Url of your bridge (http://xxx.xxx.xxx:port) | - |
| Bridge_Token | Token to utilize the Nuki Bridge API (check your Nuki App) | - |
| Bridge_HashToken | Hash the token on requests to ensure safety | true |

** If you don't know the address of your bridge you can find it via `https://api.nuki.io/discover/bridges`, response would be something like:
```json
{
    "bridges": [{
        "bridgeId": 2117604523,
        "ip": "192.168.1.50",
        "port": 8080,
        "dateUpdated": "2017-06-14 T06:53:44Z"
    }],
    "errorCode": 0
}
```
You can see the `ip` and `port` in the response, enter as `http://ip:port` in the configuration file.
> Calling the URL https://api.nuki.io/discover/bridges returns a JSON array with all bridges which have been connected to the Nuki Servers through the same IP address than the one calling the URL within the last 30 days. The array contains the local IP address, port, the ID of each bridge and the date of the last change of the entry in the JSON array.

## Starting/stopping
Go to services.msc to start/stop the `Bluewalk NukiBridge2Mqtt` service or run `net start BluewalkNukiBridge2Mqtt` or `net stop BluewalkNukiBridge2Mqtt`

Once started the service will start discovering connected locks to the bridge and subscribe to the lock specific MQTT topics. After discovery the service will also register a callback on the bridge to itself (only when it doesn't exist).

## MQTT Topics

___!NOTE: Prepend the root-topic if provided in the config, by default `nukibridge`___

| Topic | Type | Data |
|-|-|-|
| discover | write | `true` to start lock discovery after initial discovery upon start |
| reset | write | `true` to factory reset the bridge |
| reboot | write | `true` to reboot the bridge |
| fw-upgrade | write | `true` to immediately check for firmware updates and install it |
| callbacks | write | `true` to query all registered callbacks and log these to the logfile |
| {lockId}/lock-state `OR` {lockName}/lock-state | readonly | Contains the current lock state (`Locked`, `Unlocking`, `Unlocked`, `Locking`, `Unlatched`, `UnlockedLockNGo`, `Unlatching`, `MotorBlocked`, `Undefined`) |
| {lockId}/battery-critical `OR` {lockName}/battery-critical | readonly | `True` or `False` if the battery level is critical |
| {lockId}/lock-action `OR` {lockName}/lock-action | write | Performs an action on the lock (`Unlock`, `Lock`, `Unlatch`, `LockNGo`, `LockNGoWithUnlatch`) |

** Lock name is automatically generated based on the actual Lock Name, eg. `Front door` becomes `front-door`

## Log
The service will automatically create a log file under the directory of the service.
Log settings can be changed in `Net.Bluewalk.NukiBridge2Mqtt.Service.exe.config` under the `log4net` section. (See [Log4net help](https://logging.apache.org/log4net/release/manual/configuration.html) for more information)


## Uninstall
1. Stop the service
2. Run `Net.Bluewalk.NukiBridge2Mqtt.Service.exe --uninstall`
3. Delete files

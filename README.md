# NukiBridge2Mqtt
 Connects Nuki Bridges to MQTT

## Installation
### Service (.NET 4.7)
Extract the release build to a folder and run `Net.Bluewalk.NukiBridge2Mqtt.Service.exe --install`, this will install the service. Seconds update [configuration](#configuration).

### Console app (.NET Core)
Copy files to a directory and update [configuration](#configuration). You can optionally create a startup script to always run the console at boot.

### Docker image
Run the docker image as followed
```
docker run -d --name nukibridge2mqtt bluewalk/nukibridge2mqtt [-e ...]
```
You can specify configuration items using environment variables as displayed below, e.g.
```
docker run -d --name nukibridge2mqtt bluewalk/nukibridge2mqtt -e BRIDGE_TOKEN=123abc -e MQTT_HOST=192.168.1.2
```

You can alter the `log4net` settings by mapping a local `log4net.config` file to `/app/log4.net.config`, e.g.
```
docker run -d --name nukibridge2mqtt bluewalk/nukibridge2mqtt [-e ...] -v [configfile]:/app/log4net.config
```

## Configuration (non-docker)
Edit the `config.yml` file and set the following settings accordingly under 
```yml
bridge:
  callback:
    address: # leave empty for auto detection
    port: # 8080 when left empty
  url: # http://xxx.xxx.xxx:port, leave empty for auto discovery
  token: 
  hash-token: true
mqtt:
  host: # localhost if left empty
  port: # 1883 when left empty
  root-topic: # nukibridge when left empty
  username:
  password:
```

| Configuration setting | Environment variable (docker) | Description | Default when empty |
|-|-|-|-|
| bridge`:`callback`:`address | BRIDGE_CALLBACK_ADDRESS | IP address / DNS for the Nuki Bridge callbacks | Auto detection |
| bridge`:`callback`:`port | BRIDGE_CALLBACK_PORT | Port for the Nuki Bridge callbacks | `8080` |
| bridge`:`url | BRIDGE_URL | Url of your bridge (http://xxx.xxx.xxx:port) | Auto discovery |
| bridge`:`token | BRIDGE_TOKEN | Token to utilize the Nuki Bridge API (check your Nuki App) | - |
| bridge`:`hash-token | BRIDGE_HASH_TOKEN | Hash the token on requests to ensure safety | `true` |
| bridge`:`info-interval | BRIDGE_INFO_INTERVAL | The interval in seconds to send bridge info/status | `300` |
| mqtt`:`host | MQTT_HOST | IP address / DNS of the MQTT broker | - |
| mqtt`:`port | MQTT_PORT | Port of the MQTT broker | `1883` |
| mqtt`:`root-topic | MQTT_ROOT_TOPIC | This text will be prepended to the MQTT Topic | `nukibridge` |
| mqtt`:`username | MQTT_USERNAME | Username for client authentication | - |
| mqtt`:`password | MQTT_PASSWORD | Password for client authentication (requires username to be set) | - |

---

## Starting/stopping

### Service (.NET 4.7)
Go to services.msc to start/stop the `Bluewalk NukiBridge2Mqtt` service or run `net start BluewalkNukiBridge2Mqtt` or `net stop BluewalkNukiBridge2Mqtt`

### Console app (.NET Core)
Run the console application by running `dotnet Net.Bluewalk.NukiBridge2Mqtt.Console.dll`. Press any key to stop.

Once started the logic will start discovering connected locks to the bridge and subscribe to the lock specific MQTT topics. After discovery the logic will also register a callback on the bridge to itself (only when it doesn't exist).

---

## MQTT Topics

___!NOTE: Prepend the root-topic if provided in the config, by default `nukibridge`___

| Topic | Type | Data |
|-|-|-|
| discover | write | `true` to start lock discovery after initial discovery upon start |
| reset | write | `true` to factory reset the bridge |
| reboot | write | `true` to reboot the bridge |
| fw-upgrade | write | `true` to immediately check for firmware updates and install it |
| callbacks | write | `true` to query all registered callbacks and log these to the logfile |
| {deviceId}/device-state `OR` {deviceName}/device-state | readonly | Contains the current device state (`Uncalibrated`, `Locked`, `Unlocking`, `Unlocked`, `Locking`, `Unlatched`, `UnlockedLockNGo`, `Unlatching`, `MotorBlocked`, `Undefined`) or for the opener (`Untrained`, `Online`, `RtoActive`, `Open`, `Opening`, `BootRun`, `Undefined` |
| {deviceId}/device-mode `OR` {deviceName}/device-mode | readonly | Contains the current device mode (`DoorMode`, `ContinuousMode`) |
| {deviceId}/battery-critical `OR` {deviceName}/battery-critical | readonly | `True` or `False` if the battery level is critical |
| {deviceId}/device-action `OR` {deviceName}/device-action | write | Performs an action on the device (`Unlock`, `Lock`, `Unlatch`, `LockNGo`, `LockNGoWithUnlatch`) or for the opener (`ActivateRto`, `DeactivateRto`, `ElectricStrikeActuation`, `ActivateContinuousMode`, `DeactivateContinuousMode`) |

** Device name is automatically generated based on the actual Lock Name, eg. `Front door` becomes `front-door`

---

## Log
The service will automatically create a log file under the directory of the service.
Log settings can be changed in `Net.Bluewalk.NukiBridge2Mqtt.Service.exe.config` under the `log4net` section. (See [Log4net help](https://logging.apache.org/log4net/release/manual/configuration.html) for more information)

---

## Uninstall
### Service (.NET 4.7)
1. Stop the service
2. Run `Net.Bluewalk.NukiBridge2Mqtt.Service.exe --uninstall`
3. Delete files

### Console app (.NET Core)
1. Stop the console app by pressing any key
2. Delete files

### Docker
1. Stop the running container
2. Delete container if not started with `--rm`
3. Delete image

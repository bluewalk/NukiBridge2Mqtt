# NukiBridge2Mqtt
 Connects Nuki Bridges to MQTT

## Installation
### Service (.NET 4.7)
Extract the release build to a folder and run `Net.Bluewalk.NukiBridge2Mqtt.Service.exe --install`, this will install the service. Seconds update [configuration](#configuration).

### Console app (.NET Core)
Copy files to a directory and update [configuration](#configuration). You can optionally create a startup script to always run the console at boot.

---

## Configuration
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
```

| Configuration setting | Description | Default when empty |
|-|-|-|
| bridge`:`callback`:`address | IP Address for the Nuki Bridge callbacks | Auto detection |
| bridge`:`callback`:`port | Port for the Nuki Bridge callbacks | `8080` |
| bridge`:`url | Url of your bridge (http://xxx.xxx.xxx:port) | Auto discovery |
| bridge`:`token | Token to utilize the Nuki Bridge API (check your Nuki App) | - |
| bridge`:`hash-token | Hash the token on requests to ensure safety | true |
| mqtt`:`host | IP address / DNS of the MQTT broker | - |
| mqtt`:`port | Port of the MQTT broker | `1883` |
| mqtt`:`root-topic | This text will be prepended to the MQTT Topic | `nukibridge` |

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
| {lockId}/lock-state `OR` {lockName}/lock-state | readonly | Contains the current lock state (`Locked`, `Unlocking`, `Unlocked`, `Locking`, `Unlatched`, `UnlockedLockNGo`, `Unlatching`, `MotorBlocked`, `Undefined`) |
| {lockId}/battery-critical `OR` {lockName}/battery-critical | readonly | `True` or `False` if the battery level is critical |
| {lockId}/lock-action `OR` {lockName}/lock-action | write | Performs an action on the lock (`Unlock`, `Lock`, `Unlatch`, `LockNGo`, `LockNGoWithUnlatch`) |

** Lock name is automatically generated based on the actual Lock Name, eg. `Front door` becomes `front-door`

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
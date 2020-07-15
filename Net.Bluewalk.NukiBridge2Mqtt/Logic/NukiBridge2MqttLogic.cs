using log4net;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using Net.Bluewalk.NukiBridge2Mqtt.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using MQTTnet.Client.Options;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Logic
{
    public class NukiBridge2MqttLogic
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(NukiBridge2MqttLogic));

        private IManagedMqttClient _mqttClient;
        private string _mqttHost;
        private int _mqttPort;
        private string _mqttRootTopic;
        private string _bridgeUrl;
        private string _bridgeToken;
        private bool _hashToken;
        private NukiBridgeClient _nukiBridgeClient;
        private string _callbackAddress;
        private int _callbackPort;
        private HttpListener _httpListener;
        private bool _stopHttpListener;
        private Timer _infoTimer;

        private List<Device> _devices;

        /// <summary>
        /// Constructor
        /// </summary>
        public NukiBridge2MqttLogic()
        {
            var config = Configuration.Instance.Config;

            if (config == null)
                throw new Exception(
                    "Config has not yet been read, use Configuration.Instance.FromYaml() or Configuration.Instance.FromEnvironment()");

            Initialize(config.Mqtt.Host, config.Mqtt.Port, config.Mqtt.RootTopic, config.Bridge.Callback.Address,
                config.Bridge.Callback.Port, config.Bridge.Url, config.Bridge.Token, config.Bridge.HashToken,
                config.Bridge.InfoInterval);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mqttHost"></param>
        /// <param name="mqttPort"></param>
        /// <param name="mqttRootTopic"></param>
        /// <param name="callbackAddress"></param>
        /// <param name="callbackPort"></param>
        /// <param name="bridgeUrl"></param>
        /// <param name="token"></param>
        /// <param name="hashToken"></param>
        /// <param name="infoInterval"></param>
        public NukiBridge2MqttLogic(string mqttHost, int? mqttPort, string mqttRootTopic, string callbackAddress,
            int? callbackPort, string bridgeUrl, string token, bool hashToken, int? infoInterval)
        {
            Initialize(mqttHost, mqttPort, mqttRootTopic, callbackAddress, callbackPort, bridgeUrl, token, hashToken, infoInterval);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="mqttHost"></param>
        /// <param name="mqttPort"></param>
        /// <param name="mqttRootTopic"></param>
        /// <param name="callbackAddress"></param>
        /// <param name="callbackPort"></param>
        /// <param name="bridgeUrl"></param>
        /// <param name="token"></param>
        /// <param name="hashToken"></param>
        /// <param name="infoInterval"></param>
        private void Initialize(string mqttHost, int? mqttPort, string mqttRootTopic, string callbackAddress,
            int? callbackPort, string bridgeUrl, string token, bool hashToken, int? infoInterval)
        {
            _mqttRootTopic = !string.IsNullOrEmpty(mqttRootTopic) ? mqttRootTopic : "nukibridge";
            _mqttHost = !string.IsNullOrEmpty(mqttHost) ? mqttHost : "localhost";
            _mqttPort = mqttPort ?? 1883;

            _bridgeUrl = !string.IsNullOrEmpty(bridgeUrl) ? bridgeUrl : NukiBridgeClient.DiscoverBridge();
            _bridgeToken = token;

            if (string.IsNullOrEmpty(_bridgeUrl) ||
                string.IsNullOrEmpty(_bridgeToken))
                throw new Exception("No Bridge_URL and/or Bridge_Token defined");

            _hashToken = hashToken;

            // Setup MQTT
            _mqttClient = new MqttFactory().CreateManagedMqttClient();
            _mqttClient.UseApplicationMessageReceivedHandler(e => MqttClientOnApplicationMessageReceived(e));
            _mqttClient.UseConnectedHandler(e =>
            {
                _log.Info("MQTT: Connected");

                SubscribeTopic("discover");
                SubscribeTopic("reset");
                SubscribeTopic("reboot");
                SubscribeTopic("fw-upgrade");
            });
            _mqttClient.UseDisconnectedHandler(e =>
            {
                if (e.ClientWasConnected)
                    _log.Warn($"MQTT: Disconnected ({e.Exception?.Message ?? "clean"})");
                else
                    _log.Error($"MQTT: Unable to connect ({e.Exception?.Message ?? "clean"})");
            });

            _nukiBridgeClient = new NukiBridgeClient(_bridgeUrl, _bridgeToken, _hashToken);

            // Setup callback
            _callbackAddress = callbackAddress ?? LocalIpAddress().ToString();
            _callbackPort = callbackPort ?? 8080;
            
            _httpListener = new HttpListener
            {
                Prefixes = { $"http://+:{_callbackPort}/" }
            };

            _devices = new List<Device>();

            // Prevent info interval being set to 0
            if ((infoInterval ?? 0) == 0)
                infoInterval = 300;
                
            // Setup info interval
            _infoTimer = new Timer((infoInterval ?? 300) * 1000);
            _infoTimer.Elapsed += async (sender, args) =>
            {
                _infoTimer.Stop();
                await PublishBridgeInfo();
                _infoTimer.Start();
            };
        }

        /// <summary>
        /// Starts the callback listener
        /// </summary>
        private async void HttpListenAsync()
        {
            try
            {
                _httpListener.Start();
            }
            catch (Exception e)
            {
                _log.Error("An error occurred wen starting the callback listener", e);
                return;
            }

            while (!_stopHttpListener)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await _httpListener.GetContextAsync();
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode == 995) continue;
                }

                if (ctx == null) continue;

                _log.Info("Received callback from Brigde");

                var request = ctx.Request;
                string body;
                using (var reader = new StreamReader(request.InputStream,
                    request.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }

                _log.Debug(body);

                try
                {
                    var callback = JsonConvert.DeserializeObject<CallbackBody>(body);

                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    ctx.Response.Close();

                    if (callback == null) continue;

                    var device = _devices.FirstOrDefault(l => l.NukiId.Equals(callback.NukiId));
                    if (device == null) continue;

                    device.LastKnownState.BatteryCritical = callback.BatteryCritical;
                    device.LastKnownState.State = (StateEnum)callback.State;
                    device.LastKnownState.StateName = callback.StateName;

                    await PublishDeviceStatus(device);
                }
                catch (Exception e)
                {
                    _log.Error($"An error occurred while parsing the callback: {body}", e);
                    ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    ctx.Response.Close();
                }
            }
        }

        /// <summary>
        /// Discover locks connected to the bridge
        /// </summary>
        private async void DiscoverLocks()
        {
            _log.Info("Discovering locks on bridge");

            try
            {
                await PublishBridgeInfo();
                
                _devices = _nukiBridgeClient.List();
                _devices?.ForEach(async d => await PrepareDevice(d));
            }
            catch (ApplicationException e)
            {
                _log.Error(e.Message, e);
            }
        }

        /// <summary>
        /// Initialize the callback to our listener
        /// </summary>
        private void InitializeCallback()
        {
            _log.Info("Requesting registered callbacks");
            try
            {
                var callbacks = _nukiBridgeClient.ListCallbacks().Callbacks;
                callbacks.ForEach(c => _log.Info($"Registered callback #{c.Id}: {c.Url}"));

                var callback = new Uri($"http://{_callbackAddress}:{_callbackPort}/");

                _log.Info($"Checking if callback to {callback} has already been registered");

                if (callbacks.Any(c => c.Url.Equals(callback))) return;

                _log.Info($"Not registered, registering {callback}");
                _nukiBridgeClient.AddCallback(callback);
            }
            catch (ApplicationException e)
            {
                _log.Error(e.Message, e);
            }
        }

        /// <summary>
        /// Prepare device (MQTT subscriptions etc)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task PrepareDevice(Device device)
        {
            _log.Info($"Processing device {device.NukiId}");

            SubscribeTopic(
                $"{device.NukiId}/device-action",
                $"{device.NameMqtt}/device-action");

            await PublishDeviceStatus(device);
        }

        /// <summary>
        /// Publishes the device status to the appropriate topics
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task PublishDeviceStatus(Device device)
        {
            await Publish($"{device.NukiId}/device-state", device.LastKnownState?.StateName);
            await Publish($"{device.NameMqtt}/device-state", device.LastKnownState?.StateName);
            
            await Publish($"{device.NukiId}/door-state", device.LastKnownState?.DoorSensorStateName );
            await Publish($"{device.NameMqtt}/door-state", device.LastKnownState?.DoorSensorStateName );

            await Publish($"{device.NukiId}/device-mode", device.LastKnownState?.Mode.ToString());
            await Publish($"{device.NameMqtt}/device-mode", device.LastKnownState?.Mode.ToString());

            await Publish($"{device.NukiId}/battery-critical", device.LastKnownState?.BatteryCritical.ToString());
            await Publish($"{device.NameMqtt}/battery-critical", device.LastKnownState?.BatteryCritical.ToString());
        }

        /// <summary>
        /// Publishes bridge info
        /// </summary>
        /// <returns></returns>
        private async Task PublishBridgeInfo()
        {
            var info = _nukiBridgeClient.Info();
            if (info == null) return;

            await Publish("info", info);
        }
        #region MQTT


        /// <summary>
        /// Publish a message to an MQTT topic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="obj"></param>
        /// <param name="retain"></param>
        /// <returns></returns>
        private async Task Publish(string topic, object obj, bool retain = true)
        {
            await Publish(topic, JsonConvert.SerializeObject(obj), retain);
        }
        
        /// <summary>
        /// Publish a message to an MQTT topic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="retain"></param>
        /// <returns></returns>
        private async Task Publish(string topic, string message, bool retain = true)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected) return;
            topic = $"{_mqttRootTopic}/{topic}";
#if DEBUG
            topic = $"dev/{topic}";
#endif
            _log.Info($"MQTT: Publishing message to {topic}");
            _log.Debug($"MQTT: Message: {message}");

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithExactlyOnceQoS()
                .WithRetainFlag(retain)
                .Build();

            await _mqttClient.PublishAsync(msg);
        }

        /// <summary>
        /// Subscribe to given topics
        /// </summary>
        /// <param name="topics"></param>
        private void SubscribeTopic(params string[] topics)
        {
            topics.ToList().ForEach(async topic =>
            {
                topic = $"{_mqttRootTopic}/{topic}";

#if DEBUG
                topic = $"dev/{_mqttRootTopic}/{topic}";
#endif
                _log.Info($"MQTT: Subscribing to {topic}");

                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
            });
        }

        /// <summary>
        /// MQTT message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MqttClientOnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic.ToUpper().Split('/');
            var message = e.ApplicationMessage.ConvertPayloadToString();

            _log.Debug($"Received MQTT message for topic {e.ApplicationMessage.Topic}, Data: {message}");
#if DEBUG
            // Remove first part "dev"
            topic = topic.Skip(1).ToArray();
#endif
            /**
             * Topic[0] = _rootTopic
             * Topic[1] = {NukiId} | Discover
             * Topic[2] = Device-Action, Reset, Reboot, Fw-Upgrade, Callbacks
             */
            try
            {
                switch (topic[1])
                {
                    case "DISCOVER": 
                        DiscoverLocks();
                        break;
                    case "RESET":
                        _nukiBridgeClient.FactoryReset();
                        break;
                    case "REBOOT":
                        _nukiBridgeClient.Reboot();
                        break;
                    case "FW-UPGRADE":
                        _nukiBridgeClient.FwUpdate();
                        break;
                    case "CALLBACKS":
                        InitializeCallback();
                        break;

                    default:
                        var device = _devices.FirstOrDefault(l =>
                            l.NukiId.ToString().Equals(topic[1]) || l.NameMqtt.Equals(topic[1],
                                StringComparison.InvariantCultureIgnoreCase));
                        if (device == null) return;

                        switch (topic[2])
                        {
                            case "DEVICE-ACTION":
                                Enum.TryParse(message, true, out LockActionEnum action);
                                if (action == LockActionEnum.Unspecified) return;

                                _nukiBridgeClient.LockAction(device.NukiId, action);
                                break;

                            default:
                                _log.Warn($"MQTT: {topic[2]} is not a valid device topic");
                                break;
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"An error occurred processing the MQTT message (Topic {e.ApplicationMessage.Topic}, Message: {message}", ex);
            }
        }

        #endregion

        /// <summary>
        /// Start logic
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"BluewalkNukiBridge2Mqtt-{Environment.MachineName}-{Environment.UserName}")
                .WithTcpServer(_mqttHost, _mqttPort);

            if (!string.IsNullOrEmpty(Configuration.Instance.Config.Mqtt.Username))
                clientOptions = clientOptions.WithCredentials(Configuration.Instance.Config.Mqtt.Username,
                    Configuration.Instance.Config.Mqtt.Password);

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions);

            _log.Info($"MQTT: Connecting to {_mqttHost}:{_mqttPort}");
            await _mqttClient.StartAsync(managedOptions.Build());

            _log.Info($"Starting callback listener on {_httpListener.Prefixes.First()}");
            _stopHttpListener = false;
            HttpListenAsync();

            InitializeCallback();
            DiscoverLocks();

            if (_infoTimer.Interval > 0)
                _infoTimer.Start();
        }

        /// <summary>
        /// Stop logic
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            _infoTimer.Stop();
            _stopHttpListener = true;
            _httpListener?.Stop();

            await _mqttClient?.StopAsync();
        }

        private static IPAddress LocalIpAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return null;

            var host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}

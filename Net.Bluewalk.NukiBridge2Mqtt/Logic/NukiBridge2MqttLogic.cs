using log4net;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using Net.Bluewalk.NukiBridge2Mqtt.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MQTTnet.Client.Options;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Config;

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

        private List<Lock> _locks;

        /// <summary>
        /// Constructor
        /// </summary>
        public NukiBridge2MqttLogic()
        {
            var config = Configuration.Instance.Config;

            if (config == null) throw new Exception("Config has not yet been read, use Configuration.Instance.FromYaml() or FromEnvironment()");

            Initialize(config.Mqtt.Host, config.Mqtt.Port, config.Mqtt.RootTopic, config.Bridge.Callback.Address,
                config.Bridge.Callback.Port, config.Bridge.Url, config.Bridge.Token, config.Bridge.HashToken);
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
        public NukiBridge2MqttLogic(string mqttHost, int? mqttPort, string mqttRootTopic, string callbackAddress,
            int? callbackPort, string bridgeUrl, string token, bool hashToken)
        {
            Initialize(mqttHost, mqttPort, mqttRootTopic, callbackAddress, callbackPort, bridgeUrl, token, hashToken);
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
        private void Initialize(string mqttHost, int? mqttPort, string mqttRootTopic, string callbackAddress,
            int? callbackPort, string bridgeUrl, string token, bool hashToken)
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

            _callbackAddress = callbackAddress ?? LocalIpAddress().ToString();
            _callbackPort = callbackPort ?? 8080;

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

            _httpListener = new HttpListener
            {
                Prefixes = { $"http://+:{_callbackPort}/" }
            };

            _locks = new List<Lock>();
        }

        /// <summary>
        /// Starts the callback listener
        /// </summary>
        public async void HttpListenAsync()
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
                    if (ex.ErrorCode == 995) return;
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
                    var callback = SimpleJson.DeserializeObject<CallbackBody>(body);

                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    ctx.Response.Close();

                    var @lock = _locks.FirstOrDefault(l => l.NukiId.Equals(callback.nukiId));
                    if (@lock == null) return;

                    @lock.LastKnownState.BatteryCritical = callback.batteryCritical;
                    @lock.LastKnownState.State = (LockStateEnum)callback.state;
                    @lock.LastKnownState.StateName = callback.stateName;
                    await PublishLockStatus(@lock);
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
        private void DiscoverLocks()
        {
            _log.Info("Discovering locks on bridge");

            try
            {
                _locks = _nukiBridgeClient.List();
                _locks?.ForEach(async l => await PrepareLock(l));
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
        /// Prepare lock (MQTT subscriptions etc)
        /// </summary>
        /// <param name="lock"></param>
        /// <returns></returns>
        private async Task PrepareLock(Lock @lock)
        {
            _log.Info($"Processing lock {@lock.NukiId}");

            SubscribeTopic(
                $"{@lock.NukiId}/lock-action",
                $"{@lock.NameMqtt}/lock-action");

            await PublishLockStatus(@lock);
        }

        /// <summary>
        /// Publishes the lock status to the appropriate topics
        /// </summary>
        /// <param name="lock"></param>
        /// <returns></returns>
        public async Task PublishLockStatus(Lock @lock)
        {
            await Publish($"{@lock.NukiId}/lock-state", @lock.LastKnownState.StateName);
            await Publish($"{@lock.NameMqtt}/lock-state", @lock.LastKnownState.StateName);

            await Publish($"{@lock.NukiId}/battery-critical", @lock.LastKnownState.BatteryCritical.ToString());
            await Publish($"{@lock.NameMqtt}/battery-critical", @lock.LastKnownState.BatteryCritical.ToString());
        }

        #region MQTT

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
             * Topic[2] = Lock-Action, Reset, Reboot, Fw-Upgrade, Callbacks
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
                        var @lock = _locks.FirstOrDefault(l =>
                            l.NukiId.ToString().Equals(topic[1]) || l.NameMqtt.Equals(topic[1],
                                StringComparison.InvariantCultureIgnoreCase));
                        if (@lock == null) return;

                        switch (topic[2])
                        {
                            case "LOCK-ACTION":
                                Enum.TryParse(message, true, out LockActionEnum action);
                                if (action == LockActionEnum.Unspecified) return;

                                _nukiBridgeClient.LockAction(@lock.NukiId, action);
                                break;

                            default:
                                _log.Warn($"MQTT: {topic[2]} is not a valid lock topic");
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
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId($"BluewalkNukiBridge2Mqtt-{Environment.MachineName}-{Environment.UserName}")
                    .WithTcpServer(_mqttHost, _mqttPort))
                .Build();

            _log.Info($"MQTT: Connecting to {_mqttHost}:{_mqttPort}");
            await _mqttClient.StartAsync(options);

            _log.Info($"Starting callback listener on {_httpListener.Prefixes.First()}");
            _stopHttpListener = false;
            HttpListenAsync();

            InitializeCallback();
            DiscoverLocks();
        }

        /// <summary>
        /// Stop logic
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
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

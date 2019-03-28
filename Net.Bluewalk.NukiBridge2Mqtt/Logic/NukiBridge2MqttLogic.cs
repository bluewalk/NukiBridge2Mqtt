﻿using log4net;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Net.Bluewalk.NukiBridge2Mqtt.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Net.Bluewalk.NukiBridge2Mqtt.Logic
{
    public class NukiBridge2MqttLogic
    {
        private readonly ILog _log = LogManager.GetLogger("NukiBridge2Mqtt");

        private readonly IManagedMqttClient _mqttClient;
        private readonly string _mqttHost;
        private readonly int _mqttPort;
        private readonly string _mqttRootTopic;

        private readonly NukiBridgeClient _nukiBridgeClient;
        private readonly IPAddress _callbackAddress;
        private readonly int _callbackPort;
        private readonly HttpListener _httpListener;
        private bool _stopHttpListener;

        private List<Lock> _locks;

        public NukiBridge2MqttLogic(string mqttHost, int mqttPort, string mqttRootTopic, IPAddress callbackAddress, int callbackPort, string bridgeUrl, string token)
        {
            _mqttRootTopic = !string.IsNullOrEmpty(mqttRootTopic) ? mqttRootTopic : "nukibridge";
            _mqttHost = mqttHost;
            _mqttPort = mqttPort;

            _mqttClient = new MqttFactory().CreateManagedMqttClient();
            _mqttClient.ApplicationMessageReceived += MqttClientOnApplicationMessageReceived;
            _mqttClient.Connected += (sender, args) =>
            {
                _log.Info("MQTT: Connected");

                SubscribeTopic("discover");
                SubscribeTopic("reset");
                SubscribeTopic("reboot");
                SubscribeTopic("fw-upgrade");
            };
            _mqttClient.ConnectingFailed += (sender, args) =>
                _log.Error($"MQTT: Unable to connect ({args.Exception.Message})");
            _mqttClient.Disconnected += (sender, args) => _log.Warn("MQTT: Disconnected");

            _nukiBridgeClient = new NukiBridgeClient(bridgeUrl, token);

            _callbackAddress = callbackAddress;
            _callbackPort = callbackPort;
            _httpListener = new HttpListener
            {
                Prefixes = { $"http://+:{callbackPort}/" }
            };

            _locks = new List<Lock>();
        }

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

                var request = ctx.Request;
                string body;
                using (var reader = new StreamReader(request.InputStream,
                    request.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }

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

        private void InitializeCallback()
        {
            _log.Info("Requesting registered callbacks");
            try
            {
                var callbacks = _nukiBridgeClient.ListCallbacks().Callbacks;
                var callback = new Uri($"http://{_callbackAddress}:{_callbackPort}/");

                _log.Info("Checking if callback is registered");

                if (!callbacks.Any(c => c.Url.Equals(callback)))
                {
                    _log.Info($"Not registered, registering {callback}");
                    _nukiBridgeClient.AddCallback(callback);
                }
            }
            catch (ApplicationException e)
            {
                _log.Error(e.Message, e);
            }
        }

        private async Task PrepareLock(Lock @lock)
        {
            _log.Info($"Processing lock {@lock.NukiId}");

            SubscribeTopic($"{@lock.NukiId}/lock-action");

            await PublishLockStatus(@lock);
        }

        public async Task PublishLockStatus(Lock @lock)
        {
            await Publish($"{@lock.NukiId}/lock-state", @lock.LastKnownState.StateName);
            await Publish($"{@lock.NukiId}/name", @lock.Name);
            await Publish($"{@lock.NukiId}/battery-critical", @lock.LastKnownState.BatteryCritical.ToString());
        }

        #region MQTT

        private async Task Publish(string topic, string message, bool retain = true)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected) return;
            topic = $"{_mqttRootTopic}/{topic}";
#if DEBUG
            topic = $"dev/{topic}";
#endif
            _log.Info($"MQTT: Publishing message to {topic}");
            _log.Debug($"Message: {message}");

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(msg);
        }

        private async void SubscribeTopic(string topic)
        {
            topic = $"{_mqttRootTopic}/{topic}";

#if DEBUG
            topic = $"dev/{topic}";
#endif
            _log.Info($"MQTT: Subscribing to {topic}");

            await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        private void MqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
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
                        _nukiBridgeClient.ListCallbacks()?.Callbacks.ForEach(c => _log.Info($"Callback: #{c.Id} {c.Url}"));
                        break;

                    default:
                        var @lock = _locks.FirstOrDefault(l => l.NukiId.ToString().Equals(topic[1]));
                        if (@lock == null) return;

                        switch (topic[2])
                        {
                            case "LOCK-ACTION":
                                Enum.TryParse(message, true, out LockActionEnum action);
                                if (action == LockActionEnum.Unspecified) return;

                                _nukiBridgeClient.LockAction(@lock.NukiId, action);
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

        public async Task Start()
        {
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId($"BluewalkNukiBridge2Mqtt-{Environment.MachineName}")
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

        public async Task Stop()
        {
            _stopHttpListener = true;
            _httpListener.Stop();
            await _mqttClient?.StopAsync();
        }
    }
}
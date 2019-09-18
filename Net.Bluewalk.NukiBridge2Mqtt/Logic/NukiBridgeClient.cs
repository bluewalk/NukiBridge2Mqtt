using log4net;
using Net.Bluewalk.NukiBridge2Mqtt.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Polly;
using Polly.Retry;

namespace Net.Bluewalk.NukiBridge2Mqtt.Logic
{
    /// <summary>
    /// Implementation of https://developer.nuki.io/page/nuki-bridge-http-api-180/4/
    /// </summary>
    public class NukiBridgeClient
    {
        private readonly string _baseUrl;
        private readonly string _token;
        private readonly bool _hashToken;
        private readonly Random _random;
        private readonly ILog _log = LogManager.GetLogger(typeof(NukiBridgeClient));
        private readonly RetryPolicy _retryPolicy;

        public WebProxy Proxy { get; set; }

        public NukiBridgeClient(string baseUrl, string token, bool hashToken)
        {
            _baseUrl = baseUrl;
            _token = token;
            _hashToken = hashToken;
            _random = new Random();

            _retryPolicy = Policy.Handle<ApplicationException>()
                .WaitAndRetry(5,
                    retryAttempt => TimeSpan.FromSeconds(2 * retryAttempt),
                    (exception, timeSpan, retryCount, context) =>
                {
                    _log.Error("Request failed,", exception);
                    _log.Info($"Retrying (count {retryCount}) ...");
                });
        }

        public static string DiscoverBridge(WebProxy proxy = null)
        {
            var client = new RestClient("https://api.nuki.io/");
            if (proxy != null)
                client.Proxy = proxy;

            var request = new RestRequest("discover/bridges")
            {
                RequestFormat = DataFormat.Json
            };

            var response = client.Execute<DiscoverResult>(request);
            if (response.ErrorException != null)
                throw new ApplicationException("Error retrieving response. Check inner details for more info.",
                    response.ErrorException);

            var bridge = response.Data.Bridges.FirstOrDefault();
            if (bridge == null || bridge.Ip.Equals("0.0.0.0")) return null;

            return $"http://{bridge.Ip}:{bridge.Port}";
        }

        public T Execute<T>(RestRequest request) where T : new()
        {
            return _retryPolicy.Execute(() =>
            {
                var client = new RestClient(_baseUrl)
                    .UseSerializer(() => new JsonNetSerializer());

                if (Proxy != null)
                    client.Proxy = Proxy;

                request.RequestFormat = DataFormat.Json;

                if (_hashToken)
                {
                    var tokenRnr = _random.Next(1000, 9999);
                    var tokenTimestamp = DateTime.UtcNow.ToString("s") + "Z";
                    var tokenHash = $"{tokenTimestamp},{tokenRnr},{_token}".ToSha256();

                    request.AddQueryParameter("ts", tokenTimestamp, false);
                    request.AddQueryParameter("rnr", tokenRnr.ToString());
                    request.AddQueryParameter("hash", tokenHash);
                }
                else
                    request.AddQueryParameter("token", _token);

                _log.Debug($"Performing {request.Method} request to {client.BuildUri(request)}");

                var response = client.Execute<T>(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new ApplicationException($"Unauthorized", new Exception(response.Content));

                if (response.ErrorException == null) return response.Data;

                throw new ApplicationException("Error retrieving response. Check inner details for more info.",
                    response.ErrorException);
            });
        }

        /// <summary>
        /// Returns a list of all paired Smart Locks
        /// </summary>
        /// <returns>List of Device</returns>
        public List<Device> List()
        {
            var request = new RestRequest("list");

            return Execute<List<Device>>(request);
        }

        /// <summary>
        /// Retrieves and returns the current lock state of a given Smart Device
        /// </summary>
        /// <param name="nukiId"></param>
        /// <returns>LockState</returns>
        public LockState GetLockState(int nukiId)
        {
            var request = new RestRequest("lockState");
            request.AddQueryParameter("nukiId", nukiId.ToString());

            return Execute<LockState>(request);
        }

        /// <summary>
        /// Performs a lock operation on the given Smart Device
        /// </summary>
        /// <param name="nukiId"></param>
        /// <param name="action"></param>
        /// <param name="noWait"></param>
        /// <returns>LockActionResult</returns>
        public LockActionResult LockAction(int nukiId, LockActionEnum action, bool noWait = false)
        {
            var request = new RestRequest("lockAction");
            request.AddQueryParameter("nukiId", nukiId.ToString());
            request.AddQueryParameter("action", ((int)action).ToString());
            request.AddQueryParameter("nowait", noWait ? "1" : "0");

            return Execute<LockActionResult>(request);
        }

        /// <summary>
        /// Removes the pairing with a given Smart Device
        /// </summary>
        /// <param name="nukiId"></param>
        /// <returns>RequestResult</returns>
        public RequestResult UnPair(int nukiId, DeviceTypeEnum deviceType)
        {
            var request = new RestRequest("unpair");
            request.AddQueryParameter("nukiId", nukiId.ToString());
            request.AddQueryParameter("deviceType", deviceType.ToString("d"));

            return Execute<RequestResult>(request);
        }

        /// <summary>
        /// Returns all Smart Locks in range and some device information of the bridge itself
        /// </summary>
        /// <returns>BridgeInfo</returns>
        public BridgeInfo Info()
        {
            var request = new RestRequest("info");

            return Execute<BridgeInfo>(request);
        }

        /// <summary>
        /// The following endpoints provides methods to register up to 3 http (no https) url callbacks, which will be triggered once the lock state of one of the known Smart Locks changes.
        /// 
        /// The new lock state will be sent to the callback url by executing a POST request and posting a JSON list in the following format:
        /// 
        /// {"nukiId": 11, "state": 1, "stateName": "locked", "batteryCritical": false}
        /// </summary>
        /// <param name="url"></param>
        /// <returns>RequestResult</returns>
        public RequestResult AddCallback(Uri url)
        {
            var request = new RestRequest("callback/add");
            request.AddQueryParameter("url", url.ToString());

            return Execute<RequestResult>(request);
        }

        /// <summary>
        /// Returns all registered url callbacks
        /// </summary>
        /// <returns>CallbackList</returns>
        public CallbackList ListCallbacks()
        {
            var request = new RestRequest("callback/list");

            return Execute<CallbackList>(request);
        }

        /// <summary>
        /// Removes a previously added callback
        /// </summary>
        /// <param name="id"></param>
        /// <returns>RequestResult;</returns>
        public RequestResult RemoveCallback(int id)
        {
            var request = new RestRequest("callback/remove");
            request.AddQueryParameter("id", id.ToString());

            return Execute<RequestResult>(request);
        }

        /// <summary>
        /// Immediately checks for a new firmware update and installs it
        /// </summary>
        public void FwUpdate()
        {
            var request = new RestRequest("fwupdate");

            Execute<RequestResult>(request);
        }

        /// <summary>
        /// Reboots the bridge
        /// </summary>
        public void Reboot()
        {
            var request = new RestRequest("reboot");

            Execute<RequestResult>(request);
        }

        /// <summary>
        /// Performs a factory reset
        /// </summary>
        public void FactoryReset()
        {
            var request = new RestRequest("factoryReset");

            Execute<RequestResult>(request);
        }
    }
}

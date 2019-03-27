using Net.Bluewalk.NukiBridge2Mqtt.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace Net.Bluewalk.NukiBridge2Mqtt
{
    /// <summary>
    /// Implementation of https://developer.nuki.io/page/nuki-bridge-http-api-180/4/
    /// </summary>
    public class NukiBridgeClient
    {
        private readonly string _baseUrl;
        private readonly string _token;
        private readonly Random _random;

        public WebProxy Proxy { get; set; }

        public NukiBridgeClient(string baseUrl, string token)
        {
            _baseUrl = baseUrl;
            _token = token;
            _random = new Random();
        }

        public T Execute<T>(RestRequest request) where T : new()
        {
            var client = new RestClient(_baseUrl);

            if (Proxy != null)
                client.Proxy = Proxy;

            request.RequestFormat = DataFormat.Json;

            // Generate token Hash
            //var tokenRnr = _random.Next(1000, 9999);
            //var tokenTimestamp = DateTime.UtcNow.ToString("s") + "Z";
            //var tokenHash = $"{tokenTimestamp},{tokenRnr},{_token}".ToSHA256();

            //request.AddQueryParameter("ts", tokenTimestamp, false);
            //request.AddQueryParameter("rnr", tokenRnr.ToString());
            //request.AddQueryParameter("hash", tokenHash);
            request.AddQueryParameter("token", _token);

            var response = client.Execute<T>(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new ApplicationException($"Unauthorized", new Exception(response.Content));

            if (response.ErrorException == null) return response.Data;

            throw new ApplicationException("Error retrieving response. Check inner details for more info.", response.ErrorException);
        }

        /// <summary>
        /// Returns a list of all paired Smart Locks
        /// </summary>
        /// <returns>List of Lock</returns>
        public List<Lock> List()
        {
            var request = new RestRequest("list");
            return Execute<List<Lock>>(request);
        }

        /// <summary>
        /// Retrieves and returns the current lock state of a given Smart Lock
        /// </summary>
        /// <param name="nukiId"></param>
        /// <returns>LockState</returns>
        public LockState GetLockstate(int nukiId)
        {
            var request = new RestRequest("lockState");
            request.AddQueryParameter("nukiId", nukiId.ToString());

            return Execute<LockState>(request);
        }

        /// <summary>
        /// Performs a lock operation on the given Smart Lock
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
        /// Removes the pairing with a given Smart Lock
        /// </summary>
        /// <param name="nukiId"></param>
        /// <returns>RequestResult</returns>
        public RequestResult UnPair(int nukiId)
        {
            var request = new RestRequest("unpair");
            request.AddQueryParameter("nukiId", nukiId.ToString());

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
        public RequestResult AddCallback(string url)
        {
            var request = new RestRequest("callback/add");
            request.AddQueryParameter("url", HttpUtility.UrlEncode(url));

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

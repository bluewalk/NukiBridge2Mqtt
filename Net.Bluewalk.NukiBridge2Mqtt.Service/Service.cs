using log4net;
using Net.Bluewalk.NukiBridge2Mqtt.Logic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;

namespace Net.Bluewalk.NukiBridge2Mqtt.Service
{
    public partial class Service : ServiceBase
    {
        private readonly ILog _log = LogManager.GetLogger("NukiBridge2Mqtt");
        private NukiBridge2MqttLogic _logic;

        public Service()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(params string[] args)
        {

#if (!DEBUG)
            if (System.Environment.UserInteractive)
            {
                var parameter = string.Concat(args);

                var svc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "BluewalkNukiBridge2Mqtt");

                switch (parameter)
                {
                    case "--install":
                        if (svc == null)
                            ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        if (svc != null)
                            ManagedInstallerClass.InstallHelper(new[] { "/u", "/LogFile=", Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--start":
                        if (svc != null)
                            if (svc.Status == ServiceControllerStatus.Stopped)
                                svc.Start();
                        break;
                    case "--stop":
                        if (svc?.Status == ServiceControllerStatus.Running)
                            svc.Stop();
                        break;
                    case "--pause":
                        if (svc?.Status == ServiceControllerStatus.Running)
                            svc.Pause();
                        break;
                    case "--continue":
                        if (svc?.Status == ServiceControllerStatus.Paused)
                            svc.Continue();
                        break;
                }
            }
            else
                Run(new Service());
#else
            var service = new Service();
            service.OnStart(args);

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#endif
        }

        protected override async void OnStart(string[] args)
        {
            _log.Info("Starting service");

            if (!int.TryParse(ConfigurationManager.AppSettings["MQTT_Port"], out var mqttPort))
                mqttPort = 1833;

            if (!int.TryParse(ConfigurationManager.AppSettings["Bridge_Callback_Port"], out var callbackPort))
                callbackPort = 8080;

            if (!IPAddress.TryParse(ConfigurationManager.AppSettings["Bridge_Callback_Address"],
                out var callbackAddress))
                callbackAddress = LocalIPAddress();

            _logic = new NukiBridge2MqttLogic(
                ConfigurationManager.AppSettings["MQTT_Host"],
                mqttPort,
                ConfigurationManager.AppSettings["MQTT_RootTopic"],
                callbackAddress,
                callbackPort,
                ConfigurationManager.AppSettings["Bridge_URL"],
                ConfigurationManager.AppSettings["Bridge_Token"]
            );

            await _logic.Start();
        }

        protected override async void OnStop()
        {
            _log.Info("Stopping service");
            await _logic.Stop();
        }

        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}

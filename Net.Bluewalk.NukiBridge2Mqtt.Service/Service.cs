using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Net.Bluewalk.NukiBridge2Mqtt.Service
{
    public partial class Service : ServiceBase
    {
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
            if (!int.TryParse(ConfigurationManager.AppSettings["MQTT_Port"], out var mqttPort))
                mqttPort = 1833;

            if (!int.TryParse(ConfigurationManager.AppSettings["Bridge_Callback_Port"], out var callbackPort))
                callbackPort = 8080;

            _logic = new NukiBridge2MqttLogic(
                ConfigurationManager.AppSettings["MQTT_Host"],
                mqttPort,
                ConfigurationManager.AppSettings["MQTT_RootTopic"],
                callbackPort,
                ConfigurationManager.AppSettings["Bridge_URL"],
                ConfigurationManager.AppSettings["Bridge_Token"]
            );

            await _logic.Start();
        }

        protected override async void OnStop()
        {
            await _logic.Stop();
        }
    }
}

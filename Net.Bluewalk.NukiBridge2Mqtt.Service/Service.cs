using System;
using System.Configuration;
using Net.Bluewalk.NukiBridge2Mqtt.Logic;
using System.ServiceProcess;
using System.IO;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Configuration = Net.Bluewalk.NukiBridge2Mqtt.Logic.Configuration;

namespace Net.Bluewalk.NukiBridge2Mqtt.Service
{
    public partial class Service : ServiceBase
    {
        private NukiBridge2MqttLogic _logic;

        public Service()
        {
            InitializeComponent();

            if (!Enum.TryParse(ConfigurationManager.AppSettings["LOG_LEVEL"], true, out LogEventLevel logLevel))
                logLevel = LogEventLevel.Information;

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Debug()
                .WriteTo.File(
                    "Net.Bluewalk.NukiBridge2Mqtt.Service.log", 
                    logLevel, 
                    "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                ).CreateLogger();
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
            Log.Information("Starting service");

            try
            {
                Configuration.Instance.FromYaml(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.yml"));

                _logic = new NukiBridge2MqttLogic();
                await _logic.Start();
            }
            catch (Exception e)
            {
                Log.Fatal(e.Message, e);
            }
        }

        protected override async void OnStop()
        {
            Log.Information("Stopping service");
            await _logic?.Stop();
        }
    }
}

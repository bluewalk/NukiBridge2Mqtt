using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using Net.Bluewalk.NukiBridge2Mqtt.Logic;

namespace Net.Bluewalk.NukiBridge2Mqtt.Console
{
    class Program
    {
        // AutoResetEvent to signal when to exit the application.
        private static readonly AutoResetEvent waitHandle = new AutoResetEvent(false);
        
        static void Main(string[] args)
        {
            var program = new ConsoleProgram();

            // Fire and forget
            Task.Run(() =>
            {
                program.Start(args.FirstOrDefault()?.Equals("docker") == true);
                waitHandle.WaitOne();
            });

            // Handle Control+C or Control+Break
            System.Console.CancelKeyPress += (o, e) =>
            {
                System.Console.WriteLine("Exit");

                program.Stop();

                // Allow the manin thread to continue and exit...
                waitHandle.Set();
            };

            // Wait
            waitHandle.WaitOne();
        }
    }

    public class ConsoleProgram
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(ConsoleProgram));
        private NukiBridge2MqttLogic _logic;

        public async void Start(bool isDocker = false)
        {
            _log.Info("Starting logic");
            try
            {
                if (isDocker)
                {
                    Configuration.Instance.FromEnvironment();

                    _log.Info("Running in docker container, changing some settings");

                    var repo = LogManager.GetRepository(Assembly.GetEntryAssembly()) as Hierarchy;
                    var appenders = repo.GetAppenders();

                    appenders.ToList().ForEach(a =>
                    {
                        if (a is FileAppender)
                        {
                            _log.Info($"Changing filepath for appender '{a.Name}' to /tmp");

                            ((FileAppender) a).File = Path.Combine("/tmp", Path.GetFileName(((FileAppender) a).File));
                        }
                    });
                }
                else
                    Configuration.Instance.FromYaml(
                        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.yml"));

                _log.Debug("Used configuration:");
                _log.Debug(Configuration.Instance.ToYaml());

                _logic = new NukiBridge2MqttLogic();
                await _logic.Start();
            }
            catch (Exception e)
            {
                _log.Fatal(e.Message, e);
            }
        }

        public async void Stop()
        {
            _log.Info("Stopping logic");

            await _logic?.Stop();
        }
    }

}

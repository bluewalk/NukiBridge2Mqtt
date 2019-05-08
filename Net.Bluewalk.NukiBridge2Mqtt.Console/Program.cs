using System;
using System.IO;
using System.Reflection;
using log4net;
using Net.Bluewalk.NukiBridge2Mqtt.Logic;

namespace Net.Bluewalk.NukiBridge2Mqtt.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var program = new ConsoleProgram();
            program.Start();

            System.Console.ReadLine();

            program.Stop();
        }
    }

    public class ConsoleProgram
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(ConsoleProgram));
        private NukiBridge2MqttLogic _logic;

        public async void Start()
        {
            _log.Info("Starting logic");
            try
            {
                Configuration.Instance.Read(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.yml"));

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

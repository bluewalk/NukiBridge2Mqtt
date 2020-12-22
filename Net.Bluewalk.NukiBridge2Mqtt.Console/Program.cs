using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Net.Bluewalk.DotNetEnvironmentExtensions;
using Net.Bluewalk.NukiBridge2Mqtt.Logic;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Net.Bluewalk.NukiBridge2Mqtt.Console
{
    class Program
    {
        // AutoResetEvent to signal when to exit the application.
        private static readonly AutoResetEvent waitHandle = new AutoResetEvent(false);
        
        static void Main(string[] args)
        {
            var program = new ConsoleProgram();
            
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.Console(
                    EnvironmentExtensions.GetEnvironmentVariable("LOG_LEVEL", LogEventLevel.Information),
                    "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code
                ).CreateLogger();

            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
            System.Console.WriteLine($"NukiBridge2Mqtt version {version}");
            System.Console.WriteLine("https://github.com/bluewalk/NukiBridge2Mqtt/\n");

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
        private NukiBridge2MqttLogic _logic;

        public async void Start(bool isDocker = false)
        {
            Log.Information("Starting logic");
            try
            {
                if (isDocker)
                    Configuration.Instance.FromEnvironment();
                else
                    Configuration.Instance.FromYaml(
                        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.yml"));

                Log.Debug("Used configuration:");
                Log.Debug(Configuration.Instance.ToYaml());

                _logic = new NukiBridge2MqttLogic();
                await _logic.Start();
            }
            catch (Exception e)
            {
                Log.Fatal(e.Message, e);
            }
        }

        public async void Stop()
        {
            Log.Information("Stopping logic");

            await _logic?.Stop();
        }
    }

}

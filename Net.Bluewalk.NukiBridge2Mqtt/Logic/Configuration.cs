using System;
using System.IO;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Config;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Net.Bluewalk.NukiBridge2Mqtt.Logic
{
    public class Configuration
    {
        private Config _config;

        public Config Config => _config;

        #region Singleton
        private static Configuration _instance;

        public static Configuration Instance => _instance ?? (_instance = new Configuration());
        #endregion

        public void FromYaml(string fileName)
        {
            Log.Information($"Reading configuration from {fileName}");

            if (!File.Exists(fileName))
                throw new FileNotFoundException("Config file not found", fileName);

            var yaml = File.ReadAllText(fileName);

            using (var input = new StringReader(yaml))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build();

                _config = deserializer.Deserialize<Config>(input);
            }

            Log.Information("Configuration read");
        }

        public void FromEnvironment()
        {
            Log.Information("Reading configuration from Environment variables");

            _config = new Config()
            {
                Bridge = new Bridge()
                {
                    Callback = new Callback()
                    {
                        Address = GetEnvironmentVariable<string>("BRIDGE_CALLBACK_ADDRESS"),
                        Port = GetEnvironmentVariable<int?>("BRIDGE_CALLBACK_PORT")
                    },
                    HashToken = GetEnvironmentVariable<bool>("BRIDGE_HASH_TOKEN"),
                    Token = GetEnvironmentVariable<string>("BRIDGE_TOKEN"),
                    Url = GetEnvironmentVariable<string>("BRIDGE_URL"),
                    InfoInterval = GetEnvironmentVariable<int?>("BRIDGE_INFO_INTERVAL")
                },
                Mqtt = new Mqtt()
                {
                    Host = GetEnvironmentVariable<string>("MQTT_HOST"),
                    Port = GetEnvironmentVariable<int?>("MQTT_PORT"),
                    RootTopic = GetEnvironmentVariable<string>("MQTT_ROOT_TOPIC"),
                    Username = GetEnvironmentVariable<string>("MQTT_USERNAME"),
                    Password = GetEnvironmentVariable<string>("MQTT_PASSWORD")
                }
            };

            Log.Information("Configuration read");
        }

        public string ToYaml()
        {
            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(_config);
        }

        private T GetEnvironmentVariable<T>(string name, T defaultValue = default(T))
        {
            var value = Environment.GetEnvironmentVariable(name);

            var t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) 
                    return defaultValue; 

                t = Nullable.GetUnderlyingType(t);
            }

            return string.IsNullOrEmpty(value) ? defaultValue : (T) Convert.ChangeType(value, t);
        }
    }
}

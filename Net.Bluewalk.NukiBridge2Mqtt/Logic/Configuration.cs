using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using log4net;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Net.Bluewalk.NukiBridge2Mqtt.Logic
{
    public class Configuration
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(Configuration));

        private Config _config;

        public Config Config => _config;

        #region Singleton
        private static Configuration _instance;

        public static Configuration Instance => _instance ?? (_instance = new Configuration());
        #endregion

        public void Read(string fileName)
        {
            _log.Info($"Reading configuration from {fileName}");

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

            _log.Info("Configuration read");
        }
    }
}

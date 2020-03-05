using GroupDocs.Editor.MVC.Products.Common.Util.Parser;
using Microsoft.Extensions.Configuration;
using System;

namespace GroupDocs.Editor.MVC.Products.Common.Config
{
    /// <summary>
    /// Server configuration
    /// </summary>
    public class ServerConfiguration
    {
        public int HttpPort { get; set; }
        public string HostAddress { get; set; }
        private IConfigurationSection ServerConfig { get; set; }

        /// <summary>
        /// Get server configuration section of the appsettings.json
        /// </summary>
        public ServerConfiguration(IConfiguration config) {
            ServerConfig = config.GetSection("ServerConfiguration");

            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("server");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);
            int defaultPort = Convert.ToInt32(ServerConfig["HttpPort"]);

            HttpPort = valuesGetter.GetIntegerPropertyValue("connector", defaultPort, "port");
            HostAddress = valuesGetter.GetStringPropertyValue("hostAddress", ServerConfig["HostAddress"]);
        }
    }
}
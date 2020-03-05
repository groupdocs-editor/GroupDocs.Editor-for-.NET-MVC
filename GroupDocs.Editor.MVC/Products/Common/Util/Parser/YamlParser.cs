using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using YamlDotNet.Serialization;

namespace GroupDocs.Editor.MVC.Products.Common.Util.Parser
{
    public class YamlParser
    {
        private static string appRoot = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.LastIndexOf("bin"));
        private static string YamlPath = Path.Combine(appRoot, "configuration.yml");
        private readonly dynamic ConfiguationData;

        public YamlParser()
        {           
            if (File.Exists(YamlPath))
            {
                using (var reader = new StringReader(File.ReadAllText(YamlPath)))
                {
                    var deserializer = new DeserializerBuilder().Build();
                    var yamlObject = deserializer.Deserialize(reader);

                    var serializer = new SerializerBuilder()
                        .JsonCompatible()
                        .Build();

                    ConfiguationData = serializer.Serialize(yamlObject);                     
                }
            }             
        }

        public dynamic GetConfiguration(string configurationSectionName) {
            dynamic productConfiguration = null;
            if (ConfiguationData != null)
            {
                productConfiguration = JsonConvert.DeserializeObject(ConfiguationData)[configurationSectionName];
            } 
            return productConfiguration;
        }
    }
}
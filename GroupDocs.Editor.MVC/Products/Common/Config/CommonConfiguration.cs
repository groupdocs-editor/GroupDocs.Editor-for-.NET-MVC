using GroupDocs.Editor.MVC.Products.Common.Util.Parser;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;

namespace GroupDocs.Editor.MVC.Products.Common.Config
{
    /// <summary>
    /// CommonConfiguration
    /// </summary>
    public class CommonConfiguration
    {
        [JsonProperty]
        public bool pageSelector { get; set; }

        [JsonProperty]
        public bool download { get; set; }

        [JsonProperty]
        public bool upload { get; set; }

        [JsonProperty]
        public bool print { get; set; }

        [JsonProperty]
        public bool browse { get; set; }

        [JsonProperty]
        public bool rewrite { get; set; }

        [JsonProperty]
        public bool enableRightClick { get; set; }

        private IConfigurationSection CommonConfig { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public CommonConfiguration(IConfiguration config)
        {
            CommonConfig = config.GetSection("CommonConfiguration");

            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("common");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);
            pageSelector = valuesGetter.GetBooleanPropertyValue("pageSelector", Convert.ToBoolean(CommonConfig["IsPageSelector"]));
            download = valuesGetter.GetBooleanPropertyValue("download", Convert.ToBoolean(CommonConfig["IsDownload"]));
            upload = valuesGetter.GetBooleanPropertyValue("upload", Convert.ToBoolean(CommonConfig["IsUpload"]));
            print = valuesGetter.GetBooleanPropertyValue("print", Convert.ToBoolean(CommonConfig["IsPrint"]));
            browse = valuesGetter.GetBooleanPropertyValue("browse", Convert.ToBoolean(CommonConfig["IsBrowse"]));
            rewrite = valuesGetter.GetBooleanPropertyValue("rewrite", Convert.ToBoolean(CommonConfig["IsRewrite"]));
            enableRightClick = valuesGetter.GetBooleanPropertyValue("enableRightClick", Convert.ToBoolean(CommonConfig["EnableRightClick"]));
        }
    }
}
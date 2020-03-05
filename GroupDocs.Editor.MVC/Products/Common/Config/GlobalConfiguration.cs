using GroupDocs.Editor.MVC.Products.Editor.Config;
using Microsoft.Extensions.Configuration;

namespace GroupDocs.Editor.MVC.Products.Common.Config
{
    /// <summary>
    /// Global configuration
    /// </summary>
    public class GlobalConfiguration
    {
        private readonly ServerConfiguration Server;
        private readonly ApplicationConfiguration Application;
        private readonly CommonConfiguration Common;        
        private readonly EditorConfiguration Editor;

        /// <summary>
        /// Get all configurations
        /// </summary>
        public GlobalConfiguration(IConfiguration config)
        {            
            Server = new ServerConfiguration(config);
            Application = new ApplicationConfiguration();         
            Common = new CommonConfiguration(config);        
            Editor = new EditorConfiguration(config);
        }       

        public EditorConfiguration GetEditorConfiguration()
        {
            return Editor;
        }

        public ServerConfiguration GetServerConfiguration()
        {
            return Server;
        }

        public ApplicationConfiguration GetApplicationConfiguration()
        {
            return Application;
        }

        public CommonConfiguration GetCommonConfiguration()
        {
            return Common;
        }
    }
}
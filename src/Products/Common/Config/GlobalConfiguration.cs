using GroupDocs.Editor.MVC.Products.Editor.Config;

namespace GroupDocs.Editor.MVC.Products.Common.Config
{
    /// <summary>
    /// Global configuration
    /// </summary>
    public class GlobalConfiguration
    {
        public ServerConfiguration Server;
        public ApplicationConfiguration Application;
        public CommonConfiguration Common;        
        private readonly EditorConfiguration Editor;

        /// <summary>
        /// Get all configurations
        /// </summary>
        public GlobalConfiguration()
        {            
            Server = new ServerConfiguration();
            Application = new ApplicationConfiguration();         
            Common = new CommonConfiguration();        
            Editor = new EditorConfiguration();
        }       

        public EditorConfiguration GetEditorConfiguration()
        {
            return Editor;
        }
    }
}
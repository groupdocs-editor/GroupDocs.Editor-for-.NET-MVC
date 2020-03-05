using Microsoft.Extensions.Configuration;

namespace GroupDocs.Editor.MVC.AppDomainGenerator
{
    /// <summary>
    /// DomainGenerator
    /// </summary>
    public class DomainGenerator
    {
        private readonly Products.Common.Config.GlobalConfiguration globalConfiguration;

        /// <summary>
        /// Constructor
        /// </summary>
        public DomainGenerator(string assemblyName, string className, IConfiguration config)
        {
            globalConfiguration = new Products.Common.Config.GlobalConfiguration(config);
        }

        /// <summary>
        /// Set GroupDocs.Editor license
        /// </summary>
        /// <param name="type">Type</param>
        public void SetEditorLicense()
        {
            // Initiate license class
            License license = new License();
            license.SetLicense(globalConfiguration.GetApplicationConfiguration().GetLicensePath());
        }
    }
}

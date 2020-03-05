using GroupDocs.Editor.MVC.Products.Common.Util.Parser;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GroupDocs.Editor.MVC.Products.Common.Config
{
    /// <summary>
    /// Application configuration
    /// </summary>
    public class ApplicationConfiguration
    {
        private string LicensePath = "Licenses";
        
        /// <summary>
        /// Get license path from the application configuration section of the web.config
        /// </summary>
        public ApplicationConfiguration()
        {
            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("application");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);
            string licPath = valuesGetter.GetStringPropertyValue("licensePath");
            var appRoot = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.LastIndexOf("bin"));

            if (String.IsNullOrEmpty(licPath))
            {
                // TODO: check for more cross-platform solution
                
                var licenseDirectory = Path.Combine(appRoot, licPath);
                string[] files = Directory.GetFiles(licenseDirectory, "*.lic");
                LicensePath = Path.Combine(LicensePath, files[0]);
            }
            else
            {
                if (!IsFullPath(licPath))
                {
                    licPath = Path.Combine(appRoot, licPath);
                    if (!Directory.Exists(Path.GetDirectoryName(licPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(licPath));
                    }
                }
                LicensePath = licPath;
                if (!File.Exists(LicensePath))
                {                    
                    Debug.WriteLine("License file path is incorrect, launched in trial mode");
                    LicensePath = "";
                }
            }
        }

        private static bool IsFullPath(string path)
        {
            return !String.IsNullOrWhiteSpace(path)
                && path.IndexOfAny(Path.GetInvalidPathChars().ToArray()) == -1
                && Path.IsPathRooted(path)
                && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        public string GetLicensePath()
        {
            return LicensePath;
        }
    }
}
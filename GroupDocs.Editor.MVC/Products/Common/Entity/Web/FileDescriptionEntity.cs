
using Newtonsoft.Json;

namespace GroupDocs.Editor.MVC.Products.Common.Entity.Web
{
    /// <summary>
    /// File description entity
    /// </summary>
    public class FileDescriptionEntity
    {
        [JsonProperty]
        public string guid{ get; set; }
        [JsonProperty]
        public string name{ get; set; }
        [JsonProperty]
        public string docType{ get; set; }
        [JsonProperty]
        public bool isDirectory{ get; set; }
        [JsonProperty]
        public long size{ get; set; }
    }
}
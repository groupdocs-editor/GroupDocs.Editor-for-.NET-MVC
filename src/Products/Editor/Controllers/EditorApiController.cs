using GroupDocs.Editor.Options;
using GroupDocs.Editor.MVC.Products.Common.Entity.Web;
using GroupDocs.Editor.MVC.Products.Common.Resources;
using GroupDocs.Editor.MVC.Products.Common.Util.Comparator;
using GroupDocs.Editor.MVC.Products.Editor.Config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace GroupDocs.Editor.MVC.Products.Editor.Controllers
{
    /// <summary>
    /// EditorApiController
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EditorApiController : ApiController
    {

        private static Common.Config.GlobalConfiguration globalConfiguration = new Common.Config.GlobalConfiguration();       

        /// <summary>
        /// Load Viewr configuration
        /// </summary>       
        /// <returns>Editor configuration</returns>
        [HttpGet]
        [Route("editor/loadConfig")]
        public EditorConfiguration LoadConfig()
        {
            return globalConfiguration.GetEditorConfiguration();
        }

        /// <summary>
        /// Get all files and directories from storage
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>List of files and directories</returns>
        [HttpPost]
        [Route("editor/loadFileTree")]
        public HttpResponseMessage loadFileTree(PostedDataEntity postedData)
        {
            // get request body       
            string relDirPath = postedData.path;
            // get file list from storage path
            try
            {
                // get all the files from a directory
                if (string.IsNullOrEmpty(relDirPath))
                {
                    relDirPath = globalConfiguration.GetEditorConfiguration().GetFilesDirectory();
                }
                else
                {
                    relDirPath = Path.Combine(globalConfiguration.GetEditorConfiguration().GetFilesDirectory(), relDirPath);
                }

                List<FileDescriptionEntity> fileList = new List<FileDescriptionEntity>();
                List<string> allFiles = new List<string>(Directory.GetFiles(relDirPath));
                allFiles.AddRange(Directory.GetDirectories(relDirPath));

                allFiles.Sort(new FileNameComparator());
                allFiles.Sort(new FileTypeComparator());

                foreach (string file in allFiles)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    // check if current file/folder is hidden
                    if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                        Path.GetFileName(file).Equals(Path.GetFileName(globalConfiguration.GetEditorConfiguration().GetFilesDirectory())) ||
                        Path.GetFileName(file).Equals(".gitkeep"))
                    {
                        // ignore current file and skip to next one
                        continue;
                    }
                    else
                    {
                        FileDescriptionEntity fileDescription = new FileDescriptionEntity();
                        fileDescription.guid = Path.GetFullPath(file);
                        fileDescription.name = Path.GetFileName(file);
                        // set is directory true/false
                        fileDescription.isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory);
                        // set file size
                        if (!fileDescription.isDirectory)
                        {
                            fileDescription.size = fileInfo.Length;
                        }
                        // add object to array list
                        fileList.Add(fileDescription);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, fileList);
            }
            catch (System.Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
            }
        }

        /// <summary>
        /// Load supported file types
        /// </summary>       
        /// <returns>Editor configuration</returns>
        [HttpGet]
        [Route("editor/loadFormats")]
        public List<string> LoadFormats()
        {
            return PrepareFormats();
        }

        /// <summary>
        /// Load document description
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Document info object</returns>
        [HttpPost]
        [Route("editor/loadDocumentDescription")]
        public HttpResponseMessage LoadDocumentDescription(PostedDataEntity postedData)
        {
            string password = "";
            try
            {
                dynamic options = null;
                //GroupDocs.Editor cannot detect text-based Cells documents formats (like CSV) automatically
                if (postedData.guid.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    options = new SpreadsheetToHtmlOptions();
                } else {
                    options = EditorHandler.DetectOptionsFromExtension(postedData.guid);
                }
              
                if (options is SpreadsheetToHtmlOptions)
                {
                    options.TextOptions = options.TextLoadOptions(",");
                }
                string bodyContent;

                using (System.IO.FileStream inputDoc = System.IO.File.OpenRead(postedData.guid))
                using (InputHtmlDocument htmlDoc = EditorHandler.ToHtml(inputDoc, options))
                {
                    bodyContent = htmlDoc.GetEmbeddedHtml();
                }
                LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();
                loadDocumentEntity.SetGuid(System.IO.Path.GetFileName(postedData.guid));
                PageDescriptionEntity page = new PageDescriptionEntity();
                page.SetData(bodyContent);
                loadDocumentEntity.SetPages(page);

                // return document description
                return Request.CreateResponse(HttpStatusCode.OK, loadDocumentEntity);
            }
            catch (System.Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.Forbidden, new Resources().GenerateException(ex, password));
            }
        }

        /// <summary>
        /// Download curerntly viewed document
        /// </summary>
        /// <param name="path">Path of the document to download</param>
        /// <returns>Document stream as attachement</returns>
        [HttpGet]
        [Route("editor/downloadDocument")]
        public HttpResponseMessage DownloadDocument(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    var fileStream = new FileStream(path, FileMode.Open);
                    response.Content = new StreamContent(fileStream);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                    response.Content.Headers.ContentDisposition.FileName = Path.GetFileName(path);
                    return response;
                }
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Upload document
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Uploaded document object</returns>
        [HttpPost]
        [Route("editor/uploadDocument")]
        public HttpResponseMessage UploadDocument()
        {
            try
            {
                string url = HttpContext.Current.Request.Form["url"];
                // get documents storage path
                string documentStoragePath = globalConfiguration.GetEditorConfiguration().GetFilesDirectory();
                bool rewrite = bool.Parse(HttpContext.Current.Request.Form["rewrite"]);
                string fileSavePath = "";
                if (string.IsNullOrEmpty(url))
                {
                    if (HttpContext.Current.Request.Files.AllKeys != null)
                    {
                        // Get the uploaded document from the Files collection
                        var httpPostedFile = HttpContext.Current.Request.Files["file"];
                        if (httpPostedFile != null)
                        {
                            if (rewrite)
                            {
                                // Get the complete file path
                                fileSavePath = Path.Combine(documentStoragePath, httpPostedFile.FileName);
                            }
                            else
                            {
                                fileSavePath = Resources.GetFreeFileName(documentStoragePath, httpPostedFile.FileName);
                            }

                            // Save the uploaded file to "UploadedFiles" folder
                            httpPostedFile.SaveAs(fileSavePath);
                        }
                    }
                }
                else
                {
                    using (WebClient client = new WebClient())
                    {
                        // get file name from the URL
                        Uri uri = new Uri(url);
                        string fileName = Path.GetFileName(uri.LocalPath);
                        if (rewrite)
                        {
                            // Get the complete file path
                            fileSavePath = Path.Combine(documentStoragePath, fileName);
                        }
                        else
                        {
                            fileSavePath = Resources.GetFreeFileName(documentStoragePath, fileName);
                        }
                        // Download the Web resource and save it into the current filesystem folder.
                        client.DownloadFile(url, fileSavePath);
                    }
                }
                UploadedDocumentEntity uploadedDocument = new UploadedDocumentEntity();
                uploadedDocument.guid = fileSavePath;
                return Request.CreateResponse(HttpStatusCode.OK, uploadedDocument);
            }
            catch (System.Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.OK, new Resources().GenerateException(ex));
            }
        }

        /// <summary>
        /// Load document description
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Document info object</returns>
        [HttpPost]
        [Route("editor/saveFile")]
        public HttpResponseMessage SaveFile(LoadDocumentEntity postedData)
        {
            string password ="";
            try
            {
                string htmlContent = postedData.GetPages()[0].GetData(); // Initialize with HTML markup of the edited document

                string saveFilePath = Path.Combine(globalConfiguration.GetEditorConfiguration().GetFilesDirectory(), postedData.GetGuid());
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                }
                using (OutputHtmlDocument editedHtmlDoc = new OutputHtmlDocument(htmlContent, null))
                {
                    dynamic options = GetSaveOptions(saveFilePath);
                    if (options.GetType().Equals(typeof(WordProcessingSaveOptions)))
                    {
                        options.EnablePagination = true;
                    }
                    options.Password = password;
                    options.OutputFormat = GetSaveFormat(saveFilePath);
                    using (System.IO.FileStream outputStream = System.IO.File.Create(saveFilePath))
                    {
                        EditorHandler.ToDocument(editedHtmlDoc, outputStream, options);
                    }
                }

                // return document description
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (System.Exception ex)
            {
                // set exception message
                return Request.CreateResponse(HttpStatusCode.Forbidden, new Resources().GenerateException(ex, password));
            }
        }

        private dynamic GetSaveFormat(string saveFilePath)
        {
            string extension = Path.GetExtension(saveFilePath).Replace(".", "");
            extension = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(extension);
            dynamic format = null;
            switch (extension)
            {

                case "Doc":
                    format = WordProcessingFormats.Doc;
                    break;
                case "Dot":
                    format = WordProcessingFormats.Dot;
                    break;             
                case "Docm":
                    format = WordProcessingFormats.Docm;
                    break;
                case "Dotx":
                    format = WordProcessingFormats.Dotx;
                    break;
                case "Dotm":
                    format = WordProcessingFormats.Dotm;
                    break;
                case "FlatOpc":
                    format = WordProcessingFormats.FlatOpc;
                    break;
                case "Rtf":
                    format = WordProcessingFormats.Rtf;
                    break;
                case "Odt":
                    format = WordProcessingFormats.Odt;
                    break;
                case "Ott":
                    format = WordProcessingFormats.Ott;
                    break;
                case "txt":
                    format = WordProcessingFormats.Text;
                    break;
                case "Html":
                    format = WordProcessingFormats.Html;
                    break;
                case "Mhtml":
                    format = WordProcessingFormats.Mhtml;
                    break;
                case "WordML":
                    format = WordProcessingFormats.WordML;
                    break;           
                case "Csv":
                    format = SpreadsheetFormats.Csv;
                    break;
                case "Ods":
                    format = SpreadsheetFormats.Ods;
                    break;
                case "SpreadsheetML":
                    format = SpreadsheetFormats.SpreadsheetML;
                    break;
                case "TabDelimited":
                    format = SpreadsheetFormats.TabDelimited;
                    break;
                case "Xls":
                    format = SpreadsheetFormats.Xls;
                    break;
                case "Xlsb":
                    format = SpreadsheetFormats.Xlsb;
                    break;
                case "Xlsm":
                    format = SpreadsheetFormats.Xlsm;
                    break;
                case "Xlsx":
                    format = SpreadsheetFormats.Xlsx;
                    break;
                case "Xltm":
                    format = SpreadsheetFormats.Xltm;
                    break;
                case "Xltx":
                    format = SpreadsheetFormats.Xltx;
                    break;
                default:
                    format = WordProcessingFormats.Docx;
                    break;

            }
            return format;
        }

        private dynamic GetSaveOptions(string saveFilePath)
        {
            string extension = Path.GetExtension(saveFilePath).Replace(".", "");
            extension = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(extension);
            if (extension.Equals("Txt"))
            {
                extension = "Text";
            }
            dynamic options = null;
            foreach (var item in Enum.GetNames(typeof(WordProcessingFormats)))
            {
                if (item.Equals("Auto"))
                {
                    continue;
                }
                if (item.Equals(extension))
                {
                    options = new WordProcessingSaveOptions();
                    break;
                }              
            }
            if (options == null)
            {
                options = new SpreadsheetSaveOptions();
            }
            return options;
        }

        private static List<string> PrepareFormats()
        {
            List<string> outputListItems = new List<string>();
                       
            foreach (var item in Enum.GetNames(typeof(WordProcessingFormats)))
            {
                if (item.Equals("Auto"))
                {
                    continue;
                }
                if (item.Equals("Text"))
                {
                    outputListItems.Add("Txt");
                }
                else
                {
                    outputListItems.Add(item);
                }

            }

            foreach (var item in Enum.GetNames(typeof(SpreadsheetFormats)))
            {
                if (item.Equals("Auto"))
                {
                    continue;
                }
                outputListItems.Add(item);
            }

            return outputListItems;
        }
    }
}
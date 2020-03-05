using GroupDocs.Editor.Options;
using GroupDocs.Editor.MVC.Products.Common.Entity.Web;
using GroupDocs.Editor.MVC.Products.Common.Resources;
using GroupDocs.Editor.MVC.Products.Common.Util.Comparator;
using GroupDocs.Editor.MVC.Products.Editor.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using GroupDocs.Editor.MVC.Products.Editor.Entity.Web.Request;
using GroupDocs.Editor.Formats;
using System.Globalization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace GroupDocs.Editor.MVC.Products.Editor.Controllers
{
    /// <summary>
    /// EditorApiController
    /// </summary>
    [EnableCors]
    public class EditorApiController : ControllerBase
    {
        private readonly Common.Config.GlobalConfiguration globalConfiguration;

        public EditorApiController(IConfiguration _config) 
        {
            globalConfiguration = new Common.Config.GlobalConfiguration(_config);
        }

        /// <summary>
        /// Load Viewr configuration
        /// </summary>       
        /// <returns>Editor configuration</returns>
        [HttpGet]
        [Route("loadConfig")]
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("loadFileTree")]
        public IActionResult loadFileTree(PostedDataEntity postedData)
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

                return Ok(JsonConvert.SerializeObject(fileList));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Resources().GenerateException(ex));
            }
        }

        /// <summary>
        /// Load supported file types
        /// </summary>       
        /// <returns>Editor configuration</returns>
        [HttpGet]
        [Route("loadFormats")]
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Route("loadDocumentDescription")]
        public IActionResult LoadDocumentDescription([FromBody] PostedDataEntity postedData)
        {
            try
            {
                LoadDocumentEntity loadDocumentEntity = LoadDocument(postedData.guid, postedData.password);
                // return document description
                return Ok(JsonConvert.SerializeObject(loadDocumentEntity));
            }
            catch (PasswordRequiredException ex)
            {
                // set exception message
                return StatusCode(403, new Resources().GenerateException(ex, postedData.password));
            }
            catch (Exception ex)
            {
                // set exception message
                return StatusCode(500, new Resources().GenerateException(ex, postedData.password));
            }
        }

        /// <summary>
        /// Download curerntly viewed document
        /// </summary>
        /// <param name="path">Path of the document to download</param>
        /// <returns>Document stream as attachement</returns>
        [HttpGet]
        [Route("downloadDocument")]
        public HttpResponseMessage DownloadDocument(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (System.IO.File.Exists(path))
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("uploadDocument")]
        public IActionResult UploadDocument()
        {
            try
            {
                string url = HttpContext.Request.Form["url"];
                // get documents storage path
                string documentStoragePath = globalConfiguration.GetEditorConfiguration().GetFilesDirectory();
                bool rewrite = bool.Parse(HttpContext.Request.Form["rewrite"]);
                string fileSavePath = "";
                if (string.IsNullOrEmpty(url))
                {
                    if (HttpContext.Request.Form.Files.Count > 0)
                    {
                        // Get the uploaded document from the Files collection
                        var httpPostedFile = HttpContext.Request.Form.Files["file"];
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
                            using (var fileStream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                httpPostedFile.CopyToAsync(fileStream);
                            }
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

                return Ok(JsonConvert.SerializeObject(uploadedDocument));
            }
            catch (Exception ex)
            {
                // set exception message
                return StatusCode(500, new Resources().GenerateException(ex));
            }
        }

        /// <summary>
        /// Load document description
        /// </summary>
        /// <param name="postedData">Post data</param>
        /// <returns>Document info object</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("saveFile")]
        public IActionResult SaveFile([FromBody] EditDocumentRequest postedData)
        {
            try
            {
                string htmlContent = postedData.getContent(); // Initialize with HTML markup of the edited document
                string saveFilePath = Path.Combine(globalConfiguration.GetEditorConfiguration().GetFilesDirectory(), postedData.GetGuid());
                string tempFilename = Path.GetFileNameWithoutExtension(saveFilePath) + "_tmp";
                string tempPath = Path.Combine(Path.GetDirectoryName(saveFilePath), tempFilename + Path.GetExtension(saveFilePath));

                ILoadOptions loadOptions = GetLoadOptions(postedData.GetGuid());
                loadOptions.Password = postedData.getPassword();

                // Instantiate Editor object by loading the input file
                using (GroupDocs.Editor.Editor editor = new GroupDocs.Editor.Editor(postedData.GetGuid(), delegate { return loadOptions; }))
                {
                    dynamic editOptions = GetEditOptions(postedData.GetGuid());

                    if (editOptions is WordProcessingEditOptions)
                    {
                        editOptions.EnablePagination = true;
                        editOptions.FontExtraction = FontExtractionOptions.ExtractEmbeddedWithoutSystem;
                        editOptions.EnableLanguageInformation = true;
                    }

                    using (EditableDocument beforeEdit = editor.Edit(editOptions))
                    {
                        EditableDocument htmlContentDoc = EditableDocument.FromMarkup(htmlContent, null);
                        
                        dynamic saveOptions = GetSaveOptions(postedData.GetGuid());
                        saveOptions.Password = postedData.getPassword();

                        if (saveOptions is WordProcessingSaveOptions)
                        {
                            saveOptions.EnablePagination = true;
                        }

                        using (FileStream outputStream = System.IO.File.Create(tempPath))
                        {
                            editor.Save(htmlContentDoc, outputStream, saveOptions);
                        }
                    }
                }

                if (System.IO.File.Exists(saveFilePath))
                {
                    System.IO.File.Delete(saveFilePath);
                }

                System.IO.File.Move(tempPath, saveFilePath);

                LoadDocumentEntity loadDocumentEntity = LoadDocument(saveFilePath, postedData.getPassword());
                // return document description
                return Ok(JsonConvert.SerializeObject(loadDocumentEntity));
            }
            catch (Exception ex)
            {
                // set exception message
                return StatusCode(500, new Resources().GenerateException(ex, postedData.getPassword()));
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
                case "WordML":
                    format = WordProcessingFormats.WordML;
                    break;
                case "Ods":
                    format = SpreadsheetFormats.Ods;
                    break;
                case "SpreadsheetML":
                    format = SpreadsheetFormats.SpreadsheetML;
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

        private ILoadOptions GetLoadOptions(string guid)
        {
            string extension = Path.GetExtension(guid).Replace(".", "");
            extension = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(extension);

            if (extension.Equals("Txt"))
            {
                extension = "Text";
            }

            ILoadOptions options = null;

            foreach (var item in typeof(WordProcessingFormats).GetFields())
            {
                if (item.Name.Equals("Auto"))
                {
                    continue;
                }

                if (item.Name.Equals(extension))
                {
                    options = new WordProcessingLoadOptions();
                    break;
                }
            }

            if (options == null)
            {
                options = new SpreadsheetLoadOptions();
            }

            return options;
        }

        private IEditOptions GetEditOptions(string guid)
        {
            string extension = Path.GetExtension(guid).Replace(".", "");
            extension = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(extension);

            if (extension.Equals("Txt"))
            {
                extension = "Text";
            }

            IEditOptions options = null;

            foreach (var item in typeof(WordProcessingFormats).GetFields())
            {
                if (item.Name.Equals("Auto"))
                {
                    continue;
                }

                if (item.Name.Equals(extension))
                {
                    options = new WordProcessingEditOptions();
                    break;
                }
            }

            if (options == null)
            {
                options = new SpreadsheetEditOptions();
            }

            return options;
        }

        private ISaveOptions GetSaveOptions(string saveFilePath)
        {
            string extension = Path.GetExtension(saveFilePath).Replace(".", "");
            extension = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(extension);

            if (extension.Equals("Txt"))
            {
                extension = "Text";
            }

            ISaveOptions options = null;

            foreach (var item in typeof(WordProcessingFormats).GetFields())
            {
                if (item.Name.Equals("Auto"))
                {
                    continue;
                }

                if (item.Name.Equals(extension))
                {
                    options = new WordProcessingSaveOptions(WordProcessingFormats.Docm);
                    break;
                }
            }

            if (options == null)
            {
                options = new SpreadsheetSaveOptions(SpreadsheetFormats.Xlsb);
            }

            return options;
        }

        private static List<string> PrepareFormats()
        {
            List<string> outputListItems = new List<string>();

            foreach (var item in typeof(WordProcessingFormats).GetFields())
            {
                if (item.Name.Equals("Auto"))
                {
                    continue;
                }

                if (item.Name.Equals("Text"))
                {
                    outputListItems.Add("Txt");
                }

                else
                {
                    outputListItems.Add(item.Name);
                }
            }

            foreach (var item in typeof(SpreadsheetFormats).GetFields())
            {
                if (item.Name.Equals("Auto"))
                {
                    continue;
                }

                outputListItems.Add(item.Name);
            }

            return outputListItems;
        }

        private LoadDocumentEntity LoadDocument(string guid, string password)
        {
            LoadDocumentEntity loadDocumentEntity = new LoadDocumentEntity();
            ILoadOptions loadOptions = GetLoadOptions(guid);
            loadOptions.Password = password;

            // Instantiate Editor object by loading the input file
            using (GroupDocs.Editor.Editor editor = new GroupDocs.Editor.Editor(guid, delegate { return loadOptions; }))
            {
                dynamic editOptions = GetEditOptions(guid);
                if (editOptions is WordProcessingEditOptions)
                {
                    editOptions.EnablePagination = true;
                }

                // Open input document for edit — obtain an intermediate document, that can be edited
                EditableDocument beforeEdit = editor.Edit(editOptions);

                // Get document as a single base64-encoded string, where all resources (images, fonts, etc) 
                // are embedded inside this string along with main textual content
                string allEmbeddedInsideString = beforeEdit.GetEmbeddedHtml();

                loadDocumentEntity.SetGuid(guid);
                PageDescriptionEntity page = new PageDescriptionEntity();
                page.SetData(allEmbeddedInsideString);
                loadDocumentEntity.SetPages(page);

                beforeEdit.Dispose();
            }

            return loadDocumentEntity;
        }
    }
}
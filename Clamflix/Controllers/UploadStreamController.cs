using System;
using System.Text;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Clam.Models;
using Clam.Filters;
using Clam.Utilities;
using ClamDataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using ClamDataLibrary.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Clam.Areas.Clamflix.Models;
using Clam.Utilities.Security;
using Microsoft.AspNetCore.Identity;

namespace Clam.Areas.Clamflix.Controllers
{
    //[Area("Clamflix")]
    //[Route("clamflix/film")]
    public class UploadStreamController : Controller
    {

        private readonly StreamFilmflixUpload _uploadFile;
        private readonly UserManager<ClamUserAccountRegister> _userManager;

        // Private readonly IFileProvider _fileProvider;
        private readonly ClamUserAccountContext _context;
        private readonly long _fileSizeLimit;
        private readonly ILogger<UploadStreamController> _logger;
        private readonly string[] _permittedExtentions = { ".mkv", ".flv", ".mp4", ".png", ".jpeg", ".jpg" };
        private readonly string _targetFilePath;
        private readonly string _targetFolderPath;
        private readonly IFileProvider _fileProvider;

        // Get the default form options so that we can use them to set the default
        // limits for request body data.
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public UploadStreamController(ClamUserAccountContext context, UserManager<ClamUserAccountRegister> userManager, IConfiguration config, StreamFilmflixUpload uploadFile,
            ILogger<UploadStreamController> logger, IFileProvider fileProvider)
        {

            _userManager = userManager;

            //_fileProvider = fileProvider;
            _uploadFile = uploadFile;
            _context = context;
            _logger = logger;
            _fileProvider = fileProvider;

            // Physical Store Provider
            _targetFilePath = config.GetValue<string>("AbsoluteRootFilePathStore");
            _targetFolderPath = config.GetValue<string>("AbsoluteFilePath-Flix");
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
        }

        #region StreamUploadToDatabase
        [HttpPost("upload")]
        [DisableFormValueModelBinding]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(4294967295)]
        public async Task<IActionResult> UploadDatabase()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            // User Profile
            var name = User.Identity.Name;
            var profile = await _userManager.FindByNameAsync(name);

            // Accumulate the form data key-value pairs in the request (formAccumulator).
            var formAccumulator = new KeyValueAccumulator();
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            var trustedFilePathStorage = string.Empty;
            var trustedFileNameForFileStorage = string.Empty;
            var streamedFileImageContent = new byte[0];
            var streamedFilePhysicalContent = new byte[0];


            // List Byte for file storage
            List<byte[]> filesByteStorage = new List<byte[]>();
            List<string> filesNameStorage = new List<string>();
            var fileStoredData = new Dictionary<string, byte[]>();
            List<string> storedPaths = new List<string>();
            List<string> storedPathDictionaryKeys = new List<string>();

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        // Don't trust the file name sent by the client. To display
                        // the file name, HTML-encode the value.
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);

                        if (!Directory.Exists(_targetFilePath))
                        {
                            string path = String.Format("{0}", _targetFilePath);
                            Directory.CreateDirectory(path);
                        }

                        //streamedFileContent =
                        //    await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                        //        ModelState, _permittedExtentions, _fileSizeLimit);

                        streamedFilePhysicalContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtentions, _fileSizeLimit);

                        filesNameStorage.Add(trustedFileNameForDisplay);
                        filesByteStorage.Add(streamedFilePhysicalContent);
                        fileStoredData.Add(trustedFileNameForDisplay, streamedFilePhysicalContent);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }
                    }
                    else if (MultipartRequestHelper
                        .HasFormDataContentDisposition(contentDisposition))
                    {
                        // Don't limit the key name length because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities
                            .RemoveQuotes(contentDisposition.Name).Value;
                        var encoding = GetEncoding(section);

                        if (encoding == null)
                        {
                            ModelState.AddModelError("File",
                                $"The request couldn't be processed (Error 2).");
                            // Log error

                            return BadRequest(ModelState);
                        }

                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by 
                            // MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();

                            if (string.Equals(value, "undefined",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }

                            formAccumulator.Append(key, value);

                            if (formAccumulator.ValueCount >
                                _defaultFormOptions.ValueCountLimit)
                            {
                                // Form key count limit of 
                                // _defaultFormOptions.ValueCountLimit 
                                // is exceeded.
                                ModelState.AddModelError("File",
                                    $"The request couldn't be processed (Error 3).");
                                // Log error

                                return BadRequest(ModelState);
                            }
                        }
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            // Bind form data to the model
            var formData = new StreamFormFilmflixData();
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);
            var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "",
                valueProvider: formValueProvider);
            var keyPathFolder = FilePathUrlHelper.GenerateKeyPath(profile.Id);

            trustedFilePathStorage = String.Format("{0}\\{1}\\{2}\\{3}",
                //_targetFilePath,
                _targetFolderPath,
                keyPathFolder,
                GenerateSecurity.Encode(profile.Id),
                Path.GetRandomFileName());

            if (!bindingSuccessful)
            {
                ModelState.AddModelError("File",
                    "The request couldn't be processed (Error 5).");
                // Log error

                return BadRequest(ModelState);
            }

            // **WARNING!**
            // In the following example, the file is saved without
            // scanning the file's contents. In most production
            // scenarios, an anti-virus/anti-malware scanner API
            // is used on the file before making the file available
            // for download or for use by other systems. 
            // For more information, see the topic that accompanies 
            // this sample app.

            Directory.CreateDirectory(trustedFilePathStorage);

            foreach (var item in fileStoredData)
            {
                using (var targetStream = System.IO.File.Create(
                            Path.Combine(trustedFilePathStorage, item.Key)))
                {
                    await targetStream.WriteAsync(item.Value);

                    _logger.LogInformation(
                        "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                        "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                        item.Key, trustedFilePathStorage,
                        item.Key);
                }
                storedPaths.Add(Path.Combine(trustedFilePathStorage, item.Key));
                storedPathDictionaryKeys.Add(item.Key);
            }

            var keyValue = storedPathDictionaryKeys[0];
            var keyConvert = fileStoredData[keyValue];
            var file = new ClamUserFilm()
            {
                ItemPath = storedPaths[0],
                ImagePath = storedPaths[1],
                WallpaperPath = storedPaths[2],
                FilmTitle = formData.FilmTitle,
                Size = keyConvert.Length,
                DateAdded = DateTime.Now,
                UrlEmbeddedVideo = FilePathUrlHelper.YoutubePathFilter(formData.UrlEmbeddedVideo),
                Year = formData.Year,
                Status = bool.Parse(formData.Status),
                UserId = profile.Id
            };

            _context.Add(file);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Film");
        }
        #endregion

        #region UploadModifiedVideo
        [HttpPost("modify")]
        [DisableFormValueModelBinding]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(4294967295)]
        public async Task<IActionResult> UploadModifiedVideo()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            // Accumulate the form data key-value pairs in the request (formAccumulator).
            var formAccumulator = new KeyValueAccumulator();
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            var trustedFilePathStorage = string.Empty;
            var trustedFileNameForFileStorage = string.Empty;
            var streamedFileImageContent = new byte[0];
            var streamedFilePhysicalContent = new byte[0];

            // Test Files
            List<byte[]> filesByteStorage = new List<byte[]>();
            List<string> filesNameStorage = new List<string>();
            var fileStoredData = new Dictionary<string, byte[]>();

            List<string> storedPaths = new List<string>();


            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        // Don't trust the file name sent by the client. To display
                        // the file name, HTML-encode the value.
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);

                        if (!Directory.Exists(_targetFilePath))
                        {
                            string path = String.Format("{0}", _targetFilePath);
                            Directory.CreateDirectory(path);
                        }

                        //streamedFileContent =
                        //    await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                        //        ModelState, _permittedExtentions, _fileSizeLimit);

                        streamedFilePhysicalContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtentions, _fileSizeLimit);

                        filesNameStorage.Add(trustedFileNameForDisplay);
                        filesByteStorage.Add(streamedFilePhysicalContent);
                        fileStoredData.Add(trustedFileNameForDisplay, streamedFilePhysicalContent);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }
                    }
                    else if (MultipartRequestHelper
                        .HasFormDataContentDisposition(contentDisposition))
                    {
                        // Don't limit the key name length because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities
                            .RemoveQuotes(contentDisposition.Name).Value;
                        var encoding = GetEncoding(section);

                        if (encoding == null)
                        {
                            ModelState.AddModelError("File",
                                $"The request couldn't be processed (Error 2).");
                            // Log error

                            return BadRequest(ModelState);
                        }

                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by 
                            // MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();

                            if (string.Equals(value, "undefined",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }

                            formAccumulator.Append(key, value);

                            if (formAccumulator.ValueCount >
                                _defaultFormOptions.ValueCountLimit)
                            {
                                // Form key count limit of 
                                // _defaultFormOptions.ValueCountLimit 
                                // is exceeded.
                                ModelState.AddModelError("File",
                                    $"The request couldn't be processed (Error 3).");
                                // Log error

                                return BadRequest(ModelState);
                            }
                        }
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            // Bind form data to the model
            var formData = new StreamFormFilmflixData();
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);
            var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "",
                valueProvider: formValueProvider);
            var keyPathFolder = FilePathUrlHelper.GenerateKeyPath(formData.UserId);

            trustedFilePathStorage = String.Format("{0}\\{1}\\{2}\\{3}",
                //_targetFilePath,
                _targetFolderPath,
                keyPathFolder,
                formData.UserId,
                Path.GetRandomFileName());

            if (!bindingSuccessful)
            {
                ModelState.AddModelError("File",
                    "The request couldn't be processed (Error 5).");
                // Log error

                return BadRequest(ModelState);
            }

            // **WARNING!**
            // In the following example, the file is saved without
            // scanning the file's contents. In most production
            // scenarios, an anti-virus/anti-malware scanner API
            // is used on the file before making the file available
            // for download or for use by other systems. 
            // For more information, see the topic that accompanies 
            // this sample app.

            Directory.CreateDirectory(trustedFilePathStorage);
            if (fileStoredData.Count == 2)
            {
                foreach (var item in fileStoredData)
                {
                    using (var targetStream = System.IO.File.Create(
                                Path.Combine(trustedFilePathStorage, item.Key)))
                    {
                        await targetStream.WriteAsync(item.Value);

                        _logger.LogInformation(
                            "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                            "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                            item.Key, trustedFilePathStorage,
                            item.Key);
                    }
                    storedPaths.Add(Path.Combine(trustedFilePathStorage, item.Key));
                }
                var file = new ClamUserFilm()
                {
                    ItemPath = storedPaths[0],
                    ImagePath = storedPaths[1],
                    WallpaperPath = storedPaths[3],
                    FilmTitle = formData.FilmTitle,
                    Size = streamedFilePhysicalContent.Length,
                    DateAdded = DateTime.Now,
                    UrlEmbeddedVideo = formData.UrlEmbeddedVideo,
                    Year = formData.Year,
                    Status = bool.Parse(formData.Status),
                    UserId = formData.UserId
                };

                var model = _context.ClamUserFilms.Find(formData.FilmId);
                _context.Entry(model).Entity.FilmTitle = formData.FilmTitle;
                _context.Entry(model).Entity.UrlEmbeddedVideo = formData.UrlEmbeddedVideo;
                _context.Entry(model).Entity.Year = formData.Year;
                _context.Entry(model).Entity.LastModified = DateTime.Now;
                _context.Entry(model).State = EntityState.Modified;
                _context.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Film");
            }
            else {
                var file = new ClamUserFilm()
                {
                    FilmTitle = formData.FilmTitle,
                    Size = streamedFilePhysicalContent.Length,
                    DateAdded = DateTime.Now,
                    UrlEmbeddedVideo = formData.UrlEmbeddedVideo,
                    Year = formData.Year,
                    Status = bool.Parse(formData.Status),
                    UserId = formData.UserId
                };
                var model = _context.ClamUserFilms.Find(formData.FilmId);
                _context.Entry(model).Entity.FilmTitle = formData.FilmTitle;
                _context.Entry(model).Entity.UrlEmbeddedVideo = formData.UrlEmbeddedVideo;
                _context.Entry(model).Entity.Year = formData.Year;
                _context.Entry(model).Entity.LastModified = DateTime.Now;
                _context.Entry(model).State = EntityState.Modified;
                _context.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Film");
            }
        }
        #endregion

        [HttpGet("download")]
        public ActionResult DownloadFile(Guid id)
        {
            var requestFile = _context.ClamUserFilms.SingleOrDefault(m => m.FilmId == id);
            if (requestFile == null)
            {
                return null;
            }
            return PhysicalFile(requestFile.ItemPath, MediaTypeNames.Application.Octet, WebUtility.HtmlEncode(FilePathUrlHelper.GetFileAtEndOfPath(requestFile.ItemPath)));
        }
        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader =
                MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);

            // UTF-7 is insecure and shouldn't be honored. UTF-8 succeeds in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }

            return mediaType.Encoding;
        }

    }
}
using System;
using System.Data;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Clam.Areas.Clamflix.Models;
using Clam.Filters;
using Clam.Utilities;
using Clam.Utilities.Security;
using ClamDataLibrary.DataAccess;
using ClamDataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Clam.Repository;

namespace Clam.Areas.Clamflix.Controllers
{
    [Authorize(Policy = "Level-Two")]
    [Authorize(Policy = "Contributor-Access")]
    [Area("Clamflix")]
    [Route("clamflix/film")]
    public class FilmController : Controller
    {
        private readonly StreamFilmflixUpload _uploadFile;
        private readonly UserManager<ClamUserAccountRegister> _userManager;

        private readonly ClamUserAccountContext _context;
        private readonly IMapper _mapper;

        // Stream Path Host
        private readonly string _targetFolderPath;
        private readonly string _targetFilePath;
        private readonly string _targetImagePath;
        private readonly long _fileSizeLimit;
        private readonly ILogger<FilmController> _logger;
        private readonly string[] _permittedExtentions = { ".mkv", ".flv", ".mp4", ".png", ".jpeg", ".jpg" };
 
        // WebHosting Enviroment
        private readonly IWebHostEnvironment _environment;
        // Get the default form options so that we can use them to set the default
        // limits for request body data.
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly UnitOfWork _unitOfWork;

        public FilmController(UserManager<ClamUserAccountRegister> userManager,
            ClamUserAccountContext context, IMapper mapper, IConfiguration config, IWebHostEnvironment environment, 
            ILogger<FilmController> logger, StreamFilmflixUpload uploadFile, UnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;

            _uploadFile = uploadFile;
            _environment = environment;
            _targetFilePath = config.GetValue<string>("AbsoluteRootFilePathStore");
            _targetFolderPath = config.GetValue<string>("AbsoluteFilePath-Flix");
            _targetImagePath = config.GetValue<string>("AbsoluteFilePath-Image");
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
        }


        // GET: Film
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var getUserName = User.Identity.Name;
            var model = await _unitOfWork.FilmControl.GetAllUserFilms(getUserName);
            return View(model);
        }

        // GET: Home/Create
        [Authorize(Policy = "Contributor-Create")]
        [HttpGet("upload")]
        public IActionResult PostUploadFilm()
        {
            List<bool> decisions = new List<bool>() { true, false };
            var dictionary = new Dictionary<string, bool>()
            {
                { "Public", true },
                { "Private", false }
            };
            List<SelectListItem> viewingStatus = new List<SelectListItem>();
            foreach (var item in dictionary)
            {
                viewingStatus.Add(new SelectListItem()
                {
                    Text = item.Key,
                    Value = item.Value.ToString()
                });
            }
            ViewBag.ViewStatus = viewingStatus;
            return View();
        }

        // GET: Home/Edit/5
        [Authorize(Policy = "Contributor-Update")]
        [HttpGet("update/{id}")]
        public IActionResult GetEditFilm(Guid id)
        {
            List<bool> decisions = new List<bool>() { true, false };
            var dictionary = new Dictionary<string, bool>()
            {
                { "Public", true },
                { "Private", false }
            };
            List<SelectListItem> viewingStatus = new List<SelectListItem>();
            foreach (var item in dictionary)
            {
                viewingStatus.Add(new SelectListItem()
                {
                    Text = item.Key,
                    Value = item.Value.ToString()
                });
            }
            ViewBag.ViewStatus = viewingStatus;
            ViewBag.FilmId = id;

            //Unit of Test
            var model = _unitOfWork.FilmControl.GetFilm(id);
            return View(_mapper.Map<StreamEditModelFilmflixDisplay>(model));
        }

        [Authorize(Policy = "Contributor-Update")]
        [Authorize(Policy = "Permission-Update")]
        [HttpPost("update/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetEditFilm(Guid id, StreamEditModelFilmflixDisplay formData)
        {
            await _unitOfWork.FilmControl.UpdateFilm(id, formData);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("details/{id}")]
        public IActionResult GetFilmDetails(Guid id)
        {
            var model = _unitOfWork.FilmControl.GetFilm(id);
            var result = _mapper.Map<StreamEditModelFilmflixDisplay>(model);
            ViewBag.FilmId = id;
            return View(result);
        }

        [Authorize(Policy = "Contributor-Remove")]
        [Authorize(Policy = "Permission-Remove")]
        [HttpPost("delete")]
        public async Task<IActionResult> RemoveFilm(Guid id)
        {
            await _unitOfWork.FilmControl.RemoveFilm(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Category
        [HttpGet("category")]
        public IActionResult Category()
        {
            var model = _unitOfWork.FilmControl.GetAllGenres();
            return View(model);
        }

        // GET: Home/Details/5
        [Authorize(Policy = "Contributor-Create")]
        [HttpGet("category/create")]
        public IActionResult PostUploadCategory()
        {
            return View();
        }

        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpPost("category/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostUploadCategory(AreaFilmflixCategory model)
        {
            try
            {
                await _unitOfWork.FilmControl.AddAsyncGenre(model);
                _unitOfWork.Complete();
                return RedirectToAction(nameof(Category));
            }
            catch
            {
                return View();
            }
        }

        [Authorize(Policy = "Contributor-Remove")]
        [Authorize(Policy = "Permission-Remove")]
        [HttpPost("category/delete")]
        public async Task<IActionResult> RemoveCategory(Guid id)
        {
            await _unitOfWork.FilmControl.RemoveGenre(id);
            _unitOfWork.Complete();
            return RedirectToAction(nameof(Category));
        }

        [Authorize(Policy = "Contributor-Create")]
        [HttpGet("category/select/{id}")]
        public async Task<IActionResult> CategorySelect(Guid id)
        {
            var getUser = User.Identity.Name;
            var category = _unitOfWork.FilmControl.GetGenre(id);
            var model = await _unitOfWork.FilmControl.GetAllFilmsForGenreSelection(id, getUser);
            ViewBag.CategoryName = category.CategoryName;
            ViewBag.CategoryId = category.CategoryId;
            return View(model);
        }

        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpPost("category/select")]
        public async Task<IActionResult> CategorySelectUpload(Guid id, List<FilmCategorySelection> model)
        {
            if (!ModelState.IsValid)
            {
                return View("Error");
            }
            await _unitOfWork.FilmControl.UpdateAllFilmsGenreSelection(id, model);
            _unitOfWork.Complete();
            return RedirectToAction(nameof(Category));
        }

        [HttpGet("category/details/{id}")]
        public IActionResult CategoryDetails(Guid id)
        {
            var category = _unitOfWork.FilmControl.GetGenre(id);
            var test = _unitOfWork.FilmControl.GetAsyncGenre(id);
            ViewBag.CategoryName = category.CategoryName;
            ViewBag.CategoryId = category.CategoryId;
            //return View(newModel.OrderBy(x => x.FilmTitle));
            return View(test);
        }

        // ####################################################################################################
        // ####################################################################################################
        // ####################################################################################################

        #region StreamUploadToDatabase
        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpPost("stream")]
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
            List<string> storedPaths = new List<string>();
            List<string> storedPathDictionaryKeys = new List<string>();
            var fileStoredData = new Dictionary<string, byte[]>();

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
            return RedirectToAction(nameof(Index));
        }
        #endregion

        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpGet("download")]
        public ActionResult DownloadFile(Guid id)
        {
            //var requestFile = _context.ClamUserFilms.SingleOrDefault(m => m.FilmId == id);
            var requestFile = _unitOfWork.FilmControl.GetFilm(id);
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
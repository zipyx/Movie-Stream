using System;
using System.Threading.Tasks;
using AutoMapper;
using Clam.Repository;
using Clam.Utilities;
using ClamDataLibrary.DataAccess;
using ClamDataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clam.Areas.Clamflix.Controllers
{
    [Authorize(Policy = "Level-One")]
    [Area("Clamflix")]
    [Route("clamflix")]
    public class HomeController : Controller
    {

        private readonly UserManager<ClamUserAccountRegister> _userManager;
        private readonly RoleManager<ClamRoles> _roleManager;
        private readonly UnitOfWork _unitOfWork;

        public HomeController(UserManager<ClamUserAccountRegister> userManager, RoleManager<ClamRoles> roleManager, 
            UnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        // GET: Home
        [HttpGet]
        public async Task<IActionResult> Index(string search)
        {
            // First initial check -> If user not authenticated, deny access
            if (!User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            var model = await _unitOfWork.FilmControl.GetHomeDisplayContent(search);
            return View(model);
        }

        [HttpGet("watch/{id}")]
        public async Task<IActionResult> Watch(Guid id)
        {
            var model = await _unitOfWork.FilmControl.GetAsyncFilm(id);
            var displayVideo = await _unitOfWork.FilmControl.GetHomeDisplayFilm(id);
            ViewBag.Wallpaper = FilePathUrlHelper.DataFilePathFilter(model.WallpaperPath, 3);
            ViewBag.VideoPath = FilePathUrlHelper.DataFilePathFilter(model.ItemPath, 3);
            return View(displayVideo);
        }

        [HttpGet("track/{id}")]
        public async Task<IActionResult> Show(Guid id)
        {
            var clientDisplay = await _unitOfWork.FilmControl.GetHomeDisplayTVShows(id);
            return View(clientDisplay);
        }

        [HttpGet("episode/{id}")]
        public async Task<IActionResult> Episode(Guid id)
        {
            var clientDisplay = await _unitOfWork.FilmControl.GetHomeDisplayEpisode(id);
            return View(clientDisplay);
        }

    }
}
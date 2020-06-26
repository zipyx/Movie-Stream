using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Clam.Areas.Clamflix.Models;
using Clam.Interface.Film;
using Clam.Areas.TVShows.Models;
using Clam.Utilities;
using Clam.Utilities.Security;
using ClamDataLibrary.DataAccess;
using ClamDataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clam.Repository.Film
{
    public class FilmRepository : Repository<ClamUserFilm>, IFilmRepository
    {
        private readonly UserManager<ClamUserAccountRegister> _userManager;
        private new readonly ClamUserAccountContext _context;
        private readonly IMapper _mapper;

        #region FailedBufferedFile_Dependencies
        //private readonly string _targetFolderPath;
        //private readonly string _targetImagePath;
        //private readonly long _fileSizeLimit;
        //private readonly string[] _permittedExtentions = { ".mkv", ".flv", ".mp4", ".png", ".jpeg", ".jpg" };
        #endregion

        public FilmRepository(ClamUserAccountContext context, UserManager<ClamUserAccountRegister> userManager, 
            IMapper mapper, IConfiguration config) : base(context)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;

            #region FailedBuffered_Dependency_Injections
            //_targetFolderPath = config.GetValue<string>("AbsoluteFilePath-Flix");
            //_targetImagePath = config.GetValue<string>("AbsoluteFilePath-Image");
            //_fileSizeLimit = config.GetValue<long>("ImageSizeLimit");
            #endregion
        }

        public async Task AddAsyncGenre(AreaFilmflixCategory model)
        {
            var result = _mapper.Map<ClamUserFilmCategory>(model);
            await _context.ClamUserFilmCategories.AddAsync(result);
            await _context.SaveChangesAsync();
        }

        public void AddGenre(AreaFilmflixCategory model)
        {
            var result = _mapper.Map<ClamUserFilmCategory>(model);
            _context.ClamUserFilmCategories.Add(result);
            _context.SaveChanges();
        }

        public async Task<AreaFilmflix> GetAsyncFilm(Guid id)
        {
            var model = await _context.ClamUserFilms.FindAsync(id);
            return _mapper.Map<AreaFilmflix>(model);
        }

        public async Task<AreaFilmflixCategory> GetAsyncGenre(Guid id)
        {
            var model = await _context.ClamUserFilmCategories.FindAsync(id);
            return _mapper.Map<AreaFilmflixCategory>(model);
        }

        public async Task<IEnumerable<FilmCategorySelection>> GetAsyncGenreWithFilms(Guid id)
        {
            var films = await _context.ClamUserFilms.ToListAsync();
            var category = _context.ClamUserFilmCategories.Find(id);
            var joinTable = await _context.ClamUserFilmJoinCategories.ToListAsync();

            List<FilmCategorySelection> newModel = new List<FilmCategorySelection>();
            foreach (var item in films)
            {
                if (joinTable.Any(val => val.CategoryId == category.CategoryId && val.FilmId == item.FilmId))
                {
                    newModel.Add(new FilmCategorySelection()
                    {
                        FilmTitle = item.FilmTitle,
                        Year = item.Year,
                        LastModified = item.LastModified,
                        FilmId = item.FilmId,
                        IsSelected = true
                    });
                }
                else
                {
                    newModel.Add(new FilmCategorySelection()
                    {
                        FilmTitle = item.FilmTitle,
                        Year = item.Year,
                        LastModified = item.LastModified,
                        FilmId = item.FilmId,
                        IsSelected = false
                    });
                }
            }
            return newModel;
        }

        public AreaFilmflix GetFilm(Guid id)
        {
            var model = _context.ClamUserFilms.Find(id);
            return _mapper.Map<AreaFilmflix>(model);
        }

        public AreaFilmflixCategory GetGenre(Guid id)
        {
            var model = _context.ClamUserFilmCategories.Find(id);
            return _mapper.Map<AreaFilmflixCategory>(model);
        }

        public async Task<IEnumerable<FilmCategorySelection>> GetAllFilmsForGenreSelection(Guid id, string userName)
        {
            var films = await _context.ClamUserFilms.ToListAsync();
            var category = await _context.ClamUserFilmCategories.FindAsync(id);
            var joinTable = await _context.ClamUserFilmJoinCategories.ToListAsync();

            var userProfile = await _userManager.FindByNameAsync(userName);

            List<FilmCategorySelection> newModel = new List<FilmCategorySelection>();
            foreach (var item in films)
            {
                if (joinTable.Any(val => val.CategoryId == category.CategoryId && val.FilmId == item.FilmId) && item.UserId.Equals(userProfile.Id))
                {
                    newModel.Add(new FilmCategorySelection()
                    {
                        FilmTitle = item.FilmTitle,
                        Year = item.Year,
                        LastModified = item.LastModified,
                        FilmId = item.FilmId,
                        IsSelected = true
                    });
                }
                else if (!joinTable.Any(val => val.CategoryId == category.CategoryId && val.FilmId == item.FilmId) && item.UserId.Equals(userProfile.Id))
                {
                    newModel.Add(new FilmCategorySelection()
                    {
                        FilmTitle = item.FilmTitle,
                        Year = item.Year,
                        LastModified = item.LastModified,
                        FilmId = item.FilmId,
                        IsSelected = false
                    });
                }
            }
            return newModel;
        }

        public IEnumerable<AreaFilmflixCategory> GetAllGenres()
        {
            var model = _context.ClamUserFilmCategories.ToList();
            List<AreaFilmflixCategory> result = new List<AreaFilmflixCategory>();
            foreach (var item in model)
            {
                result.Add(new AreaFilmflixCategory()
                {
                    CategoryId = item.CategoryId,
                    CategoryName = item.CategoryName,
                    LastModified = item.LastModified,
                    DateCreated = item.DateCreated
                });
            }
            return result;
        }

        public async Task<IEnumerable<AreaFilmflix>> GetAllUserFilms(string userName)
        {
            var model = _context.ClamUserFilms.ToList();
            var getUser = await _userManager.FindByNameAsync(userName);

            List<AreaFilmflix> result = new List<AreaFilmflix>();
            foreach (var item in model)
            {
                if ((item.UserId == getUser.Id) && !(await _userManager.IsInRoleAsync(getUser, "Contributer")))
                {
                    result.Add(new AreaFilmflix()
                    {
                        FilmId = item.FilmId,
                        FilmTitle = item.FilmTitle,
                        Year = item.Year,
                        Status = item.Status,
                        UserId = item.UserId
                    });
                }
            }
            return result;
        }

        public async Task<ClamflixHome> GetHomeDisplayContent(string search = null)
        {
            // Current Section retrieve database models
            var model = await _context.ClamUserFilms.ToListAsync();
            var categories = await _context.ClamUserFilmCategories.ToListAsync();
            var joinTable = await _context.ClamUserFilmJoinCategories.ToListAsync();

            // Current Section retrieve database models for TV Show category
            var showCategories = await _context.ClamSectionTVShowCategories.ToListAsync();
            var showCategoriesSections = await _context.ClamSectionTVShowSubCategories.ToListAsync();
            var showCategoriesSeasons = await _context.ClamSectionTVShowSubCategorySeasons.ToListAsync();
            var showCategoriesSeasonItems = await _context.ClamSectionTVShowSubCategorySeasonItems.ToListAsync();

            // Restriction Level Limits on List
            int restriction_Standard = 10;
            int restriction_General = 5;
            int restriction_RecentlyAdded = model.Count;

            // Main Model Display --> ClamflixHome
            var displayModel = new ClamflixHome();

            // Secondary Check -> If no available data, deny access
            if (model.Count == 0 || categories.Count == 0 || joinTable.Count == 0)
            {
                return displayModel;
            }

            // Random Number Generator
            var random = new Random();
            // Random Film list collector
            List<AreaFilmflix> result = new List<AreaFilmflix>();

            // Index Container register
            List<int> featuredIndexRegister = new List<int>();
            List<int> recommendedIndexRegister = new List<int>();
            List<int> latestIndexRegister = new List<int>();

            // Test Lists
            List<AreaFilmflix> testRecentlyAdded = new List<AreaFilmflix>();
            List<AreaFilmflix> testLatestFilms = new List<AreaFilmflix>();

            if (!String.IsNullOrEmpty(search))
            {
                foreach (var item in model)
                {
                    int randomIndex = random.Next(model.Count);
                    // Search Request Listing
                    if ((item.Status == true))
                    {
                        // Convert title to lower string
                        var filterTitle = item.FilmTitle.ToLower();
                        var getGenreId = item.ClamUserFilmJoinCategories;

                        // Search with keyword
                        if ((filterTitle.Contains(search.ToLower()) || (item.Year.Contains(search))
                            || (getGenreId.Any(x => x.ClamUserFilmCategory.CategoryName.ToLower().Contains(search.ToLower())))))
                        {
                            displayModel.Movies.Add(new AreaFilmflix()
                            {
                                FilmId = item.FilmId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(item.ItemPath, 3),
                                ImagePath = FilePathUrlHelper.DataFilePathFilter(item.ImagePath, 3),
                                WallpaperPath = FilePathUrlHelper.DataFilePathFilter(item.WallpaperPath, 3),
                                UrlEmbeddedVideo = item.UrlEmbeddedVideo,
                                Size = item.Size,
                                FilmTitle = item.FilmTitle,
                                Year = item.Year,
                                Status = item.Status,
                                UserId = item.UserId
                            });
                        }

                        // Featured Listing
                        if ((restriction_General != 0) && !(featuredIndexRegister.Contains(randomIndex)))
                        {
                            // Featured Listing
                            var featured = model[randomIndex];
                            displayModel.FeaturedList.Add(new AreaFilmflix()
                            {
                                FilmId = featured.FilmId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(featured.ItemPath, 3),
                                ImagePath = FilePathUrlHelper.DataFilePathFilter(featured.ImagePath, 3),
                                WallpaperPath = FilePathUrlHelper.DataFilePathFilter(featured.WallpaperPath, 3),
                                UrlEmbeddedVideo = featured.UrlEmbeddedVideo,
                                Size = featured.Size,
                                FilmTitle = featured.FilmTitle,
                                Year = featured.Year,
                                Status = featured.Status,
                                UserId = featured.UserId
                            });
                            featuredIndexRegister.Add(randomIndex);
                            restriction_General -= 0;
                        }
                    }

                }
                displayModel.SearchRequest = search;
                displayModel.SearchRequestResultsCount = displayModel.Movies.Count;
                return displayModel;

            }
            else
            {

                // For-loop iterates through every cateogry and retrieves each film and stores data in respective category/list-movies
                foreach (var selectedCategory in categories)
                {
                    List<AreaFilmflix> categoryFilms = new List<AreaFilmflix>();
                    foreach (var video in model)
                    {
                        if (video.Status == true
                            && joinTable.Any(x => x.FilmId == video.FilmId && x.CategoryId == selectedCategory.CategoryId)
                            && !categoryFilms.Any(x => x.FilmId.Equals(video.FilmId)))
                        {
                            categoryFilms.Add(new AreaFilmflix()
                            {
                                FilmId = video.FilmId,
                                FilmTitle = video.FilmTitle,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(video.ItemPath, 3),
                                ImagePath = FilePathUrlHelper.DataFilePathFilter(video.ImagePath, 3),
                                WallpaperPath = FilePathUrlHelper.DataFilePathFilter(video.WallpaperPath, 3),
                                UrlEmbeddedVideo = video.UrlEmbeddedVideo,
                                Size = video.Size,
                                Status = video.Status,
                                Year = video.Year,
                                LastModified = video.LastModified,
                                DateAdded = video.DateAdded,
                                UserId = video.UserId
                            });
                        }
                    }
                    // Store each item iteration into main model display listing
                    displayModel.AreaFilmflixCategories.Add(new AreaFilmflixCategory()
                    {
                        CategoryId = selectedCategory.CategoryId,
                        CategoryName = selectedCategory.CategoryName,
                        AreaFilmflixCategoryMovies = categoryFilms
                    });
                }

                // For-loop iterates through each film list and stores each list in their unique listing properties
                foreach (var item in model)
                {
                    int randomIndex = random.Next(model.Count);
                    int recommendedIndex = random.Next(model.Count);
                    if (item.Status == true)
                    {

                        // Latest Movie List
                        if ((int.Parse(item.Year) == DateTime.Now.Year) && (!displayModel.Latest.Any(x => x.FilmId == item.FilmId)) &&
                            !(latestIndexRegister.Contains(model.IndexOf(item))))
                        {
                            displayModel.Latest.Add(new AreaFilmflix()
                            {
                                FilmId = item.FilmId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(item.ItemPath, 3),
                                ImagePath = FilePathUrlHelper.DataFilePathFilter(item.ImagePath, 3),
                                WallpaperPath = FilePathUrlHelper.DataFilePathFilter(item.WallpaperPath, 3),
                                UrlEmbeddedVideo = item.UrlEmbeddedVideo,
                                Size = item.Size,
                                FilmTitle = item.FilmTitle,
                                Year = item.Year,
                                Status = item.Status,
                                UserId = item.UserId
                            });
                            latestIndexRegister.Add(model.IndexOf(item));
                        }

                        // General Movie List
                        if (!displayModel.Movies.Any(x => x.FilmId == item.FilmId))
                        {
                            displayModel.Movies.Add(new AreaFilmflix()
                            {
                                FilmId = item.FilmId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(item.ItemPath, 3),
                                ImagePath = FilePathUrlHelper.DataFilePathFilter(item.ImagePath, 3),
                                WallpaperPath = FilePathUrlHelper.DataFilePathFilter(item.WallpaperPath, 3),
                                UrlEmbeddedVideo = item.UrlEmbeddedVideo,
                                Size = item.Size,
                                FilmTitle = item.FilmTitle,
                                Year = item.Year,
                                Status = item.Status,
                                UserId = item.UserId
                            });
                        }

                        // Recently Added List (Gets sorted outside loop)
                        displayModel.RecentlyAdded.Add(new AreaFilmflix()
                        {
                            FilmId = item.FilmId,
                            ItemPath = FilePathUrlHelper.DataFilePathFilter(item.ItemPath, 3),
                            ImagePath = FilePathUrlHelper.DataFilePathFilter(item.ImagePath, 3),
                            WallpaperPath = FilePathUrlHelper.DataFilePathFilter(item.WallpaperPath, 3),
                            UrlEmbeddedVideo = item.UrlEmbeddedVideo,
                            Size = item.Size,
                            FilmTitle = item.FilmTitle,
                            Year = item.Year,
                            Status = item.Status,
                            UserId = item.UserId
                        });

                        // Recommended listing 
                        if ((restriction_Standard != 0) && !(recommendedIndexRegister.Contains(recommendedIndex)))
                        {
                            // Recommended Listing
                            var recommended = model[recommendedIndex];
                            displayModel.RecommendedList.Add(new AreaFilmflix()
                            {
                                FilmId = recommended.FilmId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(recommended.ItemPath, 3),
                                ImagePath = FilePathUrlHelper.DataFilePathFilter(recommended.ImagePath, 3),
                                WallpaperPath = FilePathUrlHelper.DataFilePathFilter(recommended.WallpaperPath, 3),
                                UrlEmbeddedVideo = item.UrlEmbeddedVideo,
                                Size = recommended.Size,
                                FilmTitle = recommended.FilmTitle,
                                Year = recommended.Year,
                                Status = recommended.Status,
                                UserId = recommended.UserId
                            });
                            recommendedIndexRegister.Add(recommendedIndex);
                            restriction_Standard -= 1;
                        }

                        // Featured Listing
                        if ((restriction_General != 0) && !(featuredIndexRegister.Contains(randomIndex)))
                        {
                            // Featured Listing
                            var featured = model[randomIndex];
                            displayModel.FeaturedList.Add(new AreaFilmflix()
                            {
                                FilmId = featured.FilmId,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(featured.ItemPath, 3),
                                ImagePath = FilePathUrlHelper.DataFilePathFilter(featured.ImagePath, 3),
                                WallpaperPath = FilePathUrlHelper.DataFilePathFilter(featured.WallpaperPath, 3),
                                UrlEmbeddedVideo = featured.UrlEmbeddedVideo,
                                Size = featured.Size,
                                FilmTitle = featured.FilmTitle,
                                Year = featured.Year,
                                Status = featured.Status,
                                UserId = featured.UserId
                            });
                            featuredIndexRegister.Add(randomIndex);
                            restriction_General -= 0;
                        }
                    }
                }

                // For-loop iterates through each episode in TV Shows category and retrieves listing
                foreach (var genre in showCategories)
                {
                    // Get list of particular subcategories belonging to particular genre
                    List<SectionTVShowSubCategory> sectionTV = new List<SectionTVShowSubCategory>();
                    foreach (var section in genre.ClamSectionTVShowSubCategories)
                    {
                        // Get List of seasons belonging to particular Sub-Category
                        List<SectionTVShowSubCategorySeason> seasonTV = new List<SectionTVShowSubCategorySeason>();
                        foreach (var season in section.ClamSectionTVShowSubCategorySeasons)
                        {
                            // Get List of Episodes belonging to particular season
                            List<SectionTVShowSubCategorySeasonItem> episodeTV = new List<SectionTVShowSubCategorySeasonItem>();
                            foreach (var episode in season.ClamSectionTVShowSubCategorySeasonItems)
                            {
                                episodeTV.Add(new SectionTVShowSubCategorySeasonItem()
                                {
                                    CategoryId = genre.CategoryId,
                                    TVShowId = section.TVShowId,
                                    SeasonId = season.SeasonId,
                                    TVShowSeasonNumber = season.TVShowSeasonNumber,
                                    ItemId = episode.ItemId,
                                    ItemPath = episode.ItemPath,
                                    ItemTitle = episode.ItemTitle,
                                });
                            }
                            seasonTV.Add(new SectionTVShowSubCategorySeason()
                            {
                                CategoryId = genre.CategoryId,
                                TVShowId = section.TVShowId,
                                SeasonId = season.SeasonId,
                                TVShowSeasonNumber = season.TVShowSeasonNumber,
                                ItemPath = season.ItemPath,
                                SubCategorySeasonItemCount = episodeTV.Count,
                                SubCategorySeasonItemList = episodeTV
                            });
                        }
                        sectionTV.Add(new SectionTVShowSubCategory()
                        {
                            CategoryId = genre.CategoryId,
                            TVShowId = section.TVShowId,
                            TVShowTitle = section.TVShowTitle,
                            ItemPath = FilePathUrlHelper.DataFilePathFilter(section.ItemPath, 3),
                            SubCategorySeasonCount = seasonTV.Count,
                            SubCategorySeasonList = seasonTV
                        });
                    }
                    displayModel.SectionTVShowCategories.Add(new SectionTVShowCategory()
                    {
                        CategoryId = genre.CategoryId,
                        Genre = genre.Genre,
                        ItemPath = FilePathUrlHelper.DataFilePathFilter(genre.ItemPath, 3),
                        SubCategoryCount = sectionTV.Count,
                        SubCategoryList = sectionTV
                    });
                    //displayModel.SectionTVShowCategories.Add(new Models.SectionTVShowCategory()
                    //{
                    //    CategoryId = genre.CategoryId,
                    //    Genre = genre.Genre,
                    //    ItemPath = FilePathUrlHelper.DataFilePathFilter(genre.ItemPath, 3),
                    //    SubCategoryCount = sectionTV.Count,
                    //    SubCategoryList = sectionTV
                    //});
                }

                // Recently Added Listing & Latest
                displayModel.FeaturedList = displayModel.FeaturedList.Take(restriction_Standard).ToList();
                displayModel.RecommendedList = displayModel.RecommendedList.Take(restriction_Standard).ToList();
                displayModel.RecentlyAdded = displayModel.RecentlyAdded.OrderByDescending(x => x.DateAdded.Date).Take(restriction_Standard + restriction_Standard).ToList();
                displayModel.Latest = displayModel.Latest.OrderByDescending(x => x.DateAdded.Ticks).Take(restriction_Standard).ToList();

                return displayModel;
            }
        }

        public async Task<ClamflixEpisode> GetHomeDisplayEpisode(Guid id)
        {
            var episodeList = await _context.ClamSectionTVShowSubCategorySeasonItems.ToListAsync();
            var video = await _context.ClamSectionTVShowSubCategorySeasonItems.FindAsync(id);
            var seasonNumber = await _context.ClamSectionTVShowSubCategorySeasons.FindAsync(video.SeasonId);

            // Instantiate Model Class for Display
            ClamflixEpisode clientDisplay = new ClamflixEpisode();
            SectionTVShowSubCategorySeasonItem convert = new SectionTVShowSubCategorySeasonItem()
            {
                CategoryId = video.CategoryId,
                TVShowId = video.TVShowId,
                SeasonId = video.SeasonId,
                ItemId = video.ItemId,
                ItemPath = FilePathUrlHelper.DataFilePathFilter(video.ItemPath, 3),
                ItemTitle = video.ItemTitle
            };
            clientDisplay.Episode = convert;
            clientDisplay.SeasonNumber = seasonNumber.TVShowSeasonNumber;

            // Get All season episodes belong to particular tv show, genre, and season
            foreach (var episode in episodeList)
            {
                if ((episode.SeasonId.Equals(video.SeasonId)) && (episode.TVShowId.Equals(video.TVShowId))
                    && (episode.CategoryId.Equals(video.CategoryId))
                    && (episode.ItemId != video.ItemId))
                {
                    clientDisplay.SameSeasonEpisodes.Add(_mapper.Map<SectionTVShowSubCategorySeasonItem>(episode));
                }
            }
            return clientDisplay;
        }

        public async Task<StreamEditModelFilmflixDisplay> GetHomeDisplayFilm(Guid id)
        {
            var model = await _context.ClamUserFilms.FindAsync(id);
            var jointCheck = await _context.ClamUserFilmJoinCategories.ToListAsync();
            var displayVideo = _mapper.Map<StreamEditModelFilmflixDisplay>(model);

            // List of Objects [View Display]
            List<ClamUserFilmCategory> collectedCategories = new List<ClamUserFilmCategory>();
            List<AreaFilmflix> recommended = new List<AreaFilmflix>();
            List<Guid> categoryList = new List<Guid>();

            // Check if there are objects listed in Joint Table
            if (jointCheck == null)
            {
                return null;
            }

            // If they pass initial check, iterate through each table to find Current Film Category
            foreach (var item in jointCheck)
            {
                if (item.FilmId.Equals(model.FilmId))
                {
                    categoryList.Add(item.CategoryId);
                }
            }

            // Iterate through each category list collected from current video and select same category videos to recommend
            foreach (var category in categoryList)
            {
                foreach (var selection in jointCheck)
                {
                    if (category.Equals(selection.CategoryId) && !selection.FilmId.Equals(model.FilmId) &&
                        !recommended.Any(x => x.FilmId.Equals(selection.FilmId)))
                    {
                        recommended.Add(_mapper.Map<AreaFilmflix>(await _context.ClamUserFilms.FindAsync(selection.FilmId)));
                    }
                }
            }

            // Adjust Image Path to Display in Recommendations
            // We don't need to modify path for Wallpaper as we are only displaying cover images for recommendations
            foreach (var video in recommended)
            {
                video.ImagePath = FilePathUrlHelper.DataFilePathFilter(video.ImagePath, 3);
            }

            // Append newly created recommended List
            displayVideo.Recommended = recommended;
            return displayVideo;
        }

        public async Task<ClamflixTVShow> GetHomeDisplayTVShows(Guid id)
        {
            // use id parameter and retrieve SubCategory from TVShowSection
            var model = await _context.ClamSectionTVShowSubCategories.FindAsync(id);

            // Client Model Display
            ClamflixTVShow clientDisplay = new ClamflixTVShow();
            var showCategoriesSections = await _context.ClamSectionTVShowSubCategories.ToListAsync();
            var showCategoriesSeasons = await _context.ClamSectionTVShowSubCategorySeasons.ToListAsync();
            var showCategoriesSeasonItems = await _context.ClamSectionTVShowSubCategorySeasonItems.ToListAsync();
            clientDisplay.TVShowDisplay = _mapper.Map<SectionTVShowSubCategory>(model);

            // Client Display, collect seasons and suggested shows for recommendation to client
            foreach (var show in showCategoriesSections)
            {
                if ((show.TVShowId.Equals(model.TVShowId) && (show.CategoryId.Equals(model.CategoryId))))
                {
                    foreach (var season in showCategoriesSeasons)
                    {
                        if ((season.CategoryId.Equals(show.CategoryId)) && (season.TVShowId.Equals(show.TVShowId)))
                        {
                            List<SectionTVShowSubCategorySeasonItem> seasonEpisodes = new List<SectionTVShowSubCategorySeasonItem>();
                            foreach (var episode in showCategoriesSeasonItems)
                            {
                                if ((episode.TVShowId.Equals(show.TVShowId)) && (episode.SeasonId.Equals(season.SeasonId)))
                                {
                                    seasonEpisodes.Add(new SectionTVShowSubCategorySeasonItem()
                                    {
                                        CategoryId = episode.CategoryId,
                                        TVShowId = episode.TVShowId,
                                        SeasonId = episode.SeasonId,
                                        ItemId = episode.ItemId,
                                        ItemTitle = FilePathUrlHelper.GetFileName(episode.ItemTitle),
                                        ItemPath = FilePathUrlHelper.DataFilePathFilter(episode.ItemPath, 3)
                                    });
                                }
                            }
                            clientDisplay.TVShowSeasons.Add(new SectionTVShowSubCategorySeason()
                            {
                                CategoryId = season.CategoryId,
                                TVShowId = season.TVShowId,
                                SeasonId = season.SeasonId,
                                TVShowSeasonNumber = season.TVShowSeasonNumber,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(season.ItemPath, 3),
                                SubCategorySeasonItemCount = seasonEpisodes.Count,
                                SubCategorySeasonItemList = seasonEpisodes
                            });
                        }
                    }
                }
                if ((show.CategoryId.Equals(model.CategoryId)) && (show.TVShowId != model.TVShowId))
                {
                    List<SectionTVShowSubCategorySeason> seasonsList = new List<SectionTVShowSubCategorySeason>();
                    foreach (var season in showCategoriesSeasons)
                    {
                        if ((season.TVShowId != show.TVShowId) && (season.CategoryId != show.CategoryId))
                        {
                            List<SectionTVShowSubCategorySeasonItem> seasonEpisodes = new List<SectionTVShowSubCategorySeasonItem>();
                            foreach (var episode in showCategoriesSeasonItems)
                            {
                                if ((episode.TVShowId != show.TVShowId) && (episode.SeasonId != season.SeasonId))
                                {
                                    seasonEpisodes.Add(new SectionTVShowSubCategorySeasonItem()
                                    {
                                        CategoryId = episode.CategoryId,
                                        TVShowId = episode.TVShowId,
                                        SeasonId = episode.SeasonId,
                                        ItemId = episode.ItemId,
                                        ItemTitle = FilePathUrlHelper.GetFileName(episode.ItemTitle),
                                        ItemPath = FilePathUrlHelper.DataFilePathFilter(episode.ItemPath, 3)
                                    });
                                }
                            }
                            seasonsList.Add(new SectionTVShowSubCategorySeason()
                            {
                                CategoryId = season.CategoryId,
                                TVShowId = season.TVShowId,
                                SeasonId = season.SeasonId,
                                TVShowSeasonNumber = season.TVShowSeasonNumber,
                                ItemPath = FilePathUrlHelper.DataFilePathFilter(season.ItemPath, 3),
                                SubCategorySeasonItemCount = seasonEpisodes.Count,
                                SubCategorySeasonItemList = seasonEpisodes
                            });
                        }
                    }
                    clientDisplay.CategoryTVShows.Add(new SectionTVShowSubCategory()
                    {
                        CategoryId = show.CategoryId,
                        TVShowId = show.TVShowId,
                        TVShowTitle = show.TVShowTitle,
                        ItemPath = FilePathUrlHelper.DataFilePathFilter(show.ItemPath, 6),
                        TVShowSeasonNumberTotal = seasonsList.Count,
                        SubCategorySeasonList = seasonsList
                    });
                }
            };

            return clientDisplay;
        }

        public async Task RemoveFilm(Guid id)
        {
            var model = _context.ClamUserFilms.Find(id);
            var result = FilePathUrlHelper.DataFilePathFilterIndex(model.ItemPath, 4);
            var path = model.ItemPath.Substring(0, result);
            Directory.Delete(path, true);
            _context.ClamUserFilms.Remove(model);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveGenre(Guid id)
        {
            var model = _context.ClamUserFilmCategories.Find(id);
            _context.ClamUserFilmCategories.Remove(model);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRangeFilms(List<AreaFilmflix> model)
        {
            foreach (var item in model)
            {
                var result = _context.ClamUserFilms.Find(item.FilmId);
                var filterIndex = FilePathUrlHelper.DataFilePathFilterIndex(result.ItemPath, 4);
                var filterPath = result.ItemPath.Substring(0, filterIndex);
                Directory.Delete(filterPath, true);
                _context.ClamUserFilms.Remove(result);
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAllFilmsGenreSelection(Guid id, List<FilmCategorySelection> model)
        {
            var category = await _context.ClamUserFilmCategories.FindAsync(id);
            var queryTable = _context.ClamUserFilmJoinCategories.AsNoTracking().ToList();
            List<ClamUserFilmJoinCategory> joinTables = new List<ClamUserFilmJoinCategory>();
            foreach (var item in model)
            {
                if (item.IsSelected == true && (queryTable.Any(val => val.CategoryId == category.CategoryId && val.FilmId == item.FilmId)))
                {
                    continue;
                }
                if (item.IsSelected == true && !(queryTable.Any(val => val.CategoryId == category.CategoryId && val.FilmId == item.FilmId)))
                {
                    joinTables.Add(new ClamUserFilmJoinCategory()
                    {
                        CategoryId = category.CategoryId,
                        FilmId = item.FilmId
                    });
                }
                if (item.IsSelected == false && (queryTable.Any(val => val.CategoryId == category.CategoryId && val.FilmId == item.FilmId)))
                {
                    _context.Remove(new ClamUserFilmJoinCategory() { FilmId = item.FilmId, CategoryId = category.CategoryId });
                }
            }
            await _context.AddRangeAsync(joinTables);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateFilm(Guid id, StreamEditModelFilmflixDisplay formData)
        {
            var model = _context.ClamUserFilms.Find(formData.FilmId);
            _context.Entry(model).Entity.FilmTitle = formData.FilmTitle;
            _context.Entry(model).Entity.UrlEmbeddedVideo = FilePathUrlHelper.YoutubePathFilter(formData.UrlEmbeddedVideo);
            _context.Entry(model).Entity.Status = bool.Parse(formData.Status);
            _context.Entry(model).Entity.Year = formData.Year;
            _context.Entry(model).Entity.LastModified = DateTime.Now;
            _context.Entry(model).State = EntityState.Modified;
            _context.Update(model);
            await _context.SaveChangesAsync();
        }

        #region BufferedFileTest_Failed
        //public async Task UploadBufferedFilm(StreamFilmflixUpload model)
        //{
        //    // User Details
        //    if (!User.Identity.IsAuthenticated)
        //    {
        //        //_logger.LogInformation("User is not Authenticated");
        //        Console.WriteLine("User is not Authenticated");
        //    }
        //    var profile = await _userManager.GetUserAsync(User);

        //    // byte cache for each file submitted for processing
        //    var bufferedPhysicalFile = new byte[0];
        //    var bufferedPhysicalCover = new byte[0];
        //    var bufferedPhysicalWallpaper = new byte[0];

        //    // encode file names for display 
        //    var trustedFileNameForDisplay = string.Empty;
        //    var trustedCoverNameForDisplay = string.Empty;
        //    var trustedWallpaperNameForDisplay = string.Empty;

        //    // path for storage
        //    var trustedFilePathStorage = string.Empty;

        //    // file dictionary store file name and byte information
        //    var fileStoredData = new Dictionary<string, byte[]>();
        //    List<string> storedPaths = new List<string>();
        //    List<string> storedPathDictionaryKeys = new List<string>();

        //    // Film Content
        //    bufferedPhysicalFile = await FileHelpers.ProcessFormFile<StreamFileUploadDatabase>(
        //           model.ItemPath, ModelState, _permittedExtentions,
        //           _fileSizeLimit);
        //    trustedFileNameForDisplay = WebUtility.HtmlEncode(model.ItemPath.FileName);

        //    // Cover Content
        //    bufferedPhysicalCover = await FileHelpers.ProcessFormFile<StreamFileUploadDatabase>(
        //           model.ItemPath, ModelState, _permittedExtentions,
        //           _fileSizeLimit);
        //    trustedCoverNameForDisplay = WebUtility.HtmlEncode(model.ItemPath.FileName);

        //    // Image/Wallpaper Content
        //    bufferedPhysicalWallpaper = await FileHelpers.ProcessFormFile<StreamFileUploadDatabase>(
        //           model.ItemPath, ModelState, _permittedExtentions,
        //           _fileSizeLimit);
        //    trustedWallpaperNameForDisplay = WebUtility.HtmlEncode(model.ItemPath.FileName);

        //    if (!ModelState.IsValid)
        //    {
        //        //_logger.LogInformation("Model State is invalid, check modelstate errors.");
        //        Console.WriteLine("Model state is invalid, check modelstate error.");
        //        ModelState.AddModelError("File Content",
        //            $"The request couldn't be processed (Error 1).");
        //    }

        //    fileStoredData.Add(trustedFileNameForDisplay, bufferedPhysicalFile);
        //    fileStoredData.Add(trustedCoverNameForDisplay, bufferedPhysicalCover);
        //    fileStoredData.Add(trustedWallpaperNameForDisplay, bufferedPhysicalWallpaper);

        //    var keyPathFolder = FilePathUrlHelper.GenerateKeyPath(profile.Id);
        //    trustedFilePathStorage = String.Format("{0}\\{1}\\{2}\\{3}",
        //        //_targetFilePath,
        //        _targetFolderPath,
        //        keyPathFolder,
        //        GenerateSecurity.Encode(profile.Id),
        //        Path.GetRandomFileName());

        //    Directory.CreateDirectory(trustedFilePathStorage);

        //    foreach (var item in fileStoredData)
        //    {
        //        using (var targetStream = System.IO.File.Create(
        //                    Path.Combine(trustedFilePathStorage, item.Key)))
        //        {
        //            await targetStream.WriteAsync(item.Value);

        //            //_logger.LogInformation(
        //            //    "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
        //            //    "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
        //            //    item.Key, trustedFilePathStorage,
        //            //    item.Key);
        //        }
        //        storedPaths.Add(Path.Combine(trustedFilePathStorage, item.Key));
        //        storedPathDictionaryKeys.Add(item.Key);
        //    }

        //    var keyValue = storedPathDictionaryKeys[0];
        //    var keyConvert = fileStoredData[keyValue];
        //    var file = new ClamUserFilm()
        //    {
        //        ItemPath = storedPaths[0],
        //        ImagePath = storedPaths[1],
        //        WallpaperPath = storedPaths[2],
        //        FilmTitle = model.FilmTitle,
        //        Size = keyConvert.Length,
        //        DateAdded = DateTime.Now,
        //        UrlEmbeddedVideo = FilePathUrlHelper.YoutubePathFilter(model.UrlEmbeddedVideo),
        //        Year = model.Year,
        //        Status = model.Status,
        //        UserId = profile.Id
        //    };

        //    _context.Add(file);
        //    await _context.SaveChangesAsync();

        //}
        #endregion
    }
}
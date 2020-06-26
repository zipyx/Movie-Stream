using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Clam.Areas.TVShows.Models;

namespace Clam.Areas.Clamflix.Models
{
    public class AreaFilmflix
    {
        public AreaFilmflix()
        {
            AreaFilmflixJoinCategories = new HashSet<AreaFilmflixJoinCategory>();
        }

        [Key]
        [Required]
        [Display(Name = "Film ID")]
        public Guid FilmId { get; set; }

        [Required]
        [MaxLength(300)]
        [DataType(DataType.Text)]
        [Display(Name = "Film (Path)")]
        public string ItemPath { get; set; }

        [MaxLength(300)]
        [DataType(DataType.Text)]
        [Display(Name = "Cover (Path)")]
        public string ImagePath { get; set; }

        [MaxLength(300)]
        [DataType(DataType.Text)]
        [Display(Name = "WallpaperPath (Path)")]
        public string WallpaperPath { get; set; }

        [MaxLength(200)]
        [DataType(DataType.Text)]
        [Display(Name = "Preview (Embed Video)")]
        public string UrlEmbeddedVideo { get; set; }

        [Display(Name = "Size (bytes)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public long Size { get; set; }

        [Required]
        [MaxLength(60)]
        [Display(Name = "Film Title")]
        public string FilmTitle { get; set; }

        [MaxLength(20)]
        [DataType(DataType.Text)]
        public string Year { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public bool Status { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateAdded { get; set; }

        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        public virtual Clam.Models.UserAccountRegister User { get; set; }

        public ICollection<AreaFilmflixJoinCategory> AreaFilmflixJoinCategories { get; set; }

    }

    public class AreaFilmflixCategory
    {
        public AreaFilmflixCategory()
        {
            AreaFilmflixCategoryMovies = new List<AreaFilmflix>();
        }

        [Key]
        [Required]
        [Display(Name = "Category Code")]
        public Guid CategoryId { get; set; }

        [Required]
        [MaxLength(30)]
        [DataType(DataType.Text)]
        [Display(Name = "Genre")]
        public string CategoryName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateCreated { get; set; }

        public List<AreaFilmflix> AreaFilmflixCategoryMovies { get; set; }
    }

    public class AreaFilmflixJoinCategory
    {
        public Guid FilmId { get; set; }
        public AreaFilmflix AreaFilmflix { get; set; }

        public Guid CategoryId { get; set; }
        public AreaFilmflixCategory AreaFilmflixCategory { get; set; }
    }

    public class StreamFilmflixUpload
    {

        [Display(Name = "Film ID")]
        public Guid FilmId { get; set; }

        [Required]
        //[MaxLength(300)]
        [Display(Name = "Video (Path)")]
        public IFormFile ItemPath { get; set; }

        [Required]
        //[MaxLength(300)]
        [Display(Name = "Cover (Path)")]
        public IFormFile ImagePath { get; set; }

        [Required]
        //[MaxLength(300)]
        [Display(Name = "Wallpaper (Path)")]
        public IFormFile WallpaperPath { get; set; }

        [MaxLength(200)]
        [DataType(DataType.Url)]
        [Display(Name = "Preview (Embed Video)")]
        public string UrlEmbeddedVideo { get; set; }

        [Required]
        [MaxLength(60)]
        [Display(Name = "Film Title")]
        public string FilmTitle { get; set; }

        [MaxLength(20)]
        [DataType(DataType.Text)]
        public string Year { get; set; }

        [Required]
        //[MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public bool Status { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateAdded { get; set; }

        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        public virtual Clam.Models.UserAccountRegister User { get; set; }
    }

    public class StreamFormFilmflixData
    {
        [Display(Name = "User ID")]
        public Guid UserId { get; set; }

        [Display(Name = "Film ID")]
        public Guid FilmId { get; set; }

        [MaxLength(200)]
        [DataType(DataType.Text)]
        [Display(Name = "Preview (Embed Video)")]
        public string UrlEmbeddedVideo { get; set; }

        [Required]
        [MaxLength(60)]
        [Display(Name = "Film Title")]
        public string FilmTitle { get; set; }

        [MaxLength(20)]
        [DataType(DataType.Text)]
        [Display(Name = "Released")]
        public string Year { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public string Status { get; set; }
    }

    public class StreamEditModelFilmflixDisplay
    {
        public StreamEditModelFilmflixDisplay()
        {
            AreaFilmflixJoinCategories = new HashSet<AreaFilmflixJoinCategory>();
            Recommended = new List<AreaFilmflix>();
        }

        [Key]
        [Required]
        [Display(Name = "Film ID")]
        public Guid FilmId { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Video (Path)")]
        public string ItemPath { get; set; }

        [MaxLength(300)]
        [DataType(DataType.Text)]
        [Display(Name = "Cover (Path)")]
        public string ImagePath { get; set; }

        [MaxLength(300)]
        [DataType(DataType.Text)]
        [Display(Name = "Wallpaper (Path)")]
        public string WallpaperPath { get; set; }

        [MaxLength(200)]
        [DataType(DataType.Text)]
        [Display(Name = "Preview (Embed Video)")]
        public string UrlEmbeddedVideo { get; set; }

        [Display(Name = "Size (bytes)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public long Size { get; set; }

        [Required]
        [MaxLength(60)]
        [Display(Name = "Film Title")]
        public string FilmTitle { get; set; }

        [MaxLength(20)]
        [DataType(DataType.Text)]
        [Display(Name = "Released")]
        public string Year { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Viewing Status")]
        public string Status { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateAdded { get; set; }

        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        public virtual Clam.Models.UserAccountRegister User { get; set; }

        public List<AreaFilmflix> Recommended { get; set; }

        public ICollection<AreaFilmflixJoinCategory> AreaFilmflixJoinCategories { get; set; }
    }

    public class FilmCategorySelection
    {


        [Display(Name = "Film ID")]
        public Guid FilmId { get; set; }


        [MaxLength(60)]
        [Display(Name = "Film Title")]
        public string FilmTitle { get; set; }

        [MaxLength(20)]
        [DataType(DataType.Text)]
        [Display(Name = "Released")]
        public string Year { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateAdded { get; set; }

        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        public virtual Clam.Models.UserAccountRegister User { get; set; }

        [Display(Name = "Select")]
        public bool IsSelected { get; set; }
    }

    public class ClamflixHome
    {

        public ClamflixHome()
        {
            AreaFilmflixDisplay = new HashSet<AreaFilmflix>();
            AreaFilmflixCategories = new List<AreaFilmflixCategory>();
            AreaFilmflixJoinCategories = new HashSet<AreaFilmflixJoinCategory>();
            RecentlyAdded = new List<AreaFilmflix>();
            FeaturedList = new HashSet<AreaFilmflix>();
            RecommendedList = new HashSet<AreaFilmflix>();
            Latest = new List<AreaFilmflix>();
            Movies = new List<AreaFilmflix>();

            //  Section TV Show Genres
            SectionTVShowCategories = new List<SectionTVShowCategory>();
        }

        public ICollection<AreaFilmflix> AreaFilmflixDisplay { get; set; }
        
        public List<AreaFilmflixCategory> AreaFilmflixCategories { get; set; } 

        public ICollection<AreaFilmflixJoinCategory> AreaFilmflixJoinCategories { get; set; }

        public List<AreaFilmflix> RecentlyAdded { get; set; }

        public ICollection<AreaFilmflix> FeaturedList { get; set; }

        public ICollection<AreaFilmflix> RecommendedList { get; set; }

        public List<AreaFilmflix> Latest { get; set; }

        public List<AreaFilmflix> Movies { get; set; }

        [Display(Name = "Searched For")]
        public string SearchRequest { get; set; }

        public int SearchRequestResultsCount { get; set; }

        // TV Shows
        public List<SectionTVShowCategory> SectionTVShowCategories { get; set; }
    }

    public class ClamflixTVShow
    {
        public ClamflixTVShow()
        {
            CategoryTVShows = new List<SectionTVShowSubCategory>();
            TVShowSeasons = new List<SectionTVShowSubCategorySeason>();
        }

        public List<SectionTVShowSubCategory> CategoryTVShows { get; set; }

        public List<SectionTVShowSubCategorySeason> TVShowSeasons { get; set; }

        public SectionTVShowSubCategory TVShowDisplay { get; set; }

    }

    public class ClamflixEpisode
    {
        public ClamflixEpisode()
        {
            SameSeasonEpisodes = new List<SectionTVShowSubCategorySeasonItem>();
        }
        public int SeasonNumber { get; set; }

        public SectionTVShowSubCategorySeasonItem Episode { get; set; }

        public List<SectionTVShowSubCategorySeasonItem> SameSeasonEpisodes { get; set; }
    }
}
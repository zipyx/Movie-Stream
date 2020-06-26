using Clam.Areas.Clamflix.Models;
using ClamDataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clam.Interface.Film
{
    public interface IFilmRepository : IRepository<ClamUserFilm>
    {
        // Films
        Task<IEnumerable<AreaFilmflix>> GetAllUserFilms(string userName);
        AreaFilmflix GetFilm(Guid id);
        Task<AreaFilmflix> GetAsyncFilm(Guid id);
        Task<IEnumerable<FilmCategorySelection>> GetAllFilmsForGenreSelection(Guid id, string userName);
        Task UpdateAllFilmsGenreSelection(Guid id, List<FilmCategorySelection> model);
        Task UpdateFilm(Guid id, StreamEditModelFilmflixDisplay formData);
        Task RemoveFilm(Guid id);
        Task RemoveRangeFilms(List<AreaFilmflix> model);

        // Film Genre
        IEnumerable<AreaFilmflixCategory> GetAllGenres();
        void AddGenre(AreaFilmflixCategory model);
        Task AddAsyncGenre(AreaFilmflixCategory model);
        Task RemoveGenre(Guid id);
        AreaFilmflixCategory GetGenre(Guid id);
        Task<AreaFilmflixCategory> GetAsyncGenre(Guid id);
        Task<IEnumerable<FilmCategorySelection>> GetAsyncGenreWithFilms(Guid id);

        // Home View
        Task<ClamflixHome> GetHomeDisplayContent(string search = null);
        Task<StreamEditModelFilmflixDisplay> GetHomeDisplayFilm(Guid id);
        Task<ClamflixTVShow> GetHomeDisplayTVShows(Guid id);
        Task<ClamflixEpisode> GetHomeDisplayEpisode(Guid id);

        #region BufferedFileTest_Failed
        // Upload Film Control Unit
        //Task UploadBufferedFilm(StreamFilmflixUpload model);
        #endregion
    }
}

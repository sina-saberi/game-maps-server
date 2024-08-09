using AutoMapper;
using game_maps.Application.DTOs.Location;
using game_maps.Application.Interfaces;
using game_maps.Domain.Base;
using game_maps.Domain.Entities.Category;
using game_maps.Domain.Entities.Location;
using game_maps.Domain.Entities.Map;
using game_maps.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace game_maps.Application.Services
{
    public class LocationService(
        IRepository<Location, int> _locationRepository,
        IRepository<Category, int> _categoryRepository,
        IRepository<UserLocation> _userLocationRepository,
        IRepository<Map, int> _mapRepository,
        IMapper _mapper
        ) : ILocationService
    {

        public async Task<IList<LocationCateogryDto>> GetAllLocations(string slug, string? userId)
        {
            var query = from map in _mapRepository.AsQueryable()
                        join category in _categoryRepository.AsQueryable() on map.Id equals category.MapId
                        into mapCategories
                        from mapCategoriy in mapCategories.DefaultIfEmpty()
                        where map.Slug == slug
                        select mapCategoriy;

            var inludedQuery = query.Include(x => x.Locations)
            .ThenInclude(x => x.UserLocations.Where(c => c.UserId == userId));

            var result = _mapper.Map<IList<LocationCateogryDto>>(await inludedQuery.ToListAsync());
            return result;
        }
        public async Task<int> GetCheckedCount(string slug, string userId)
        {
            var query = from map in _mapRepository.AsQueryable()
                        join c in _categoryRepository.AsQueryable() on map.Id equals c.MapId
                        join l in _locationRepository.AsQueryable() on c.Id equals l.CateogryId
                        join ul in _userLocationRepository.AsQueryable() on
                        new { l1 = l.Id, l2 = userId } equals new { l1 = ul.LocationId, l2 = ul.UserId }
                        where map.Slug == slug
                        where ul.Checked
                        select ul;
            return await query.CountAsync();
        }
        public async Task<int> GetLocationCount(string slug)
        {
            var query = from map in _mapRepository.AsQueryable()
                        join c in _categoryRepository.AsQueryable() on map.Id equals c.MapId
                        join l in _locationRepository.AsQueryable() on c.Id equals l.CateogryId
                        into ml
                        from mlc in ml.DefaultIfEmpty()
                        where map.Slug == slug
                        select ml;
            return await query.CountAsync();
        }
        public async Task<LocationDetailDto> GetLocationDetailById(int id)
        {
            return _mapper.Map<LocationDetailDto>(
                await _locationRepository.AsQueryable()
                .Include(x => x.Medias)
                .FirstOrDefaultAsync(x => x.Id == id));
        }
        public async Task<bool> ToggleLocation(int id, string userId)
        {

            var userLocation =
          await _userLocationRepository.AddOrUpdateAsync(x => x.UserId == userId && x.LocationId == id, x =>
            {
                x.LocationId = id;
                x.UserId = userId;
                x.Checked = !x.Checked;
            });

            return userLocation.Checked;
        }
        public async Task<IList<LocationSearchDto>> SearchInLocation(string slug, string search)
        {
            var query = from m in _mapRepository.AsQueryable()
                        join c in _categoryRepository.AsQueryable() on m.Id equals c.MapId
                        join l in _locationRepository.AsQueryable() on c.Id equals l.CateogryId
                        into ml
                        from mapLocation in ml.DefaultIfEmpty()
                        where m.Slug == slug
                        &&
                        mapLocation.Title.ToUpper().Contains(search.ToUpper()) 
                        ||
                        mapLocation.Title.ToUpper().Contains(search.ToUpper())
                        select mapLocation;
            return _mapper.Map<IList<LocationSearchDto>>(await query.ToListAsync());
        }
    }
}
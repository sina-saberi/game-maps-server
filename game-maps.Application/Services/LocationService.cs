﻿using AutoMapper;
using game_maps.Application.DTOs.Location;
using game_maps.Application.Interfaces;
using game_maps.Domain.Base;
using game_maps.Domain.Entities.Category;
using game_maps.Domain.Entities.Game;
using game_maps.Domain.Entities.Location;
using game_maps.Domain.Entities.Map;
using game_maps.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace game_maps.Application.Services
{
    public class LocationService(
        IRepository<Location, int> locationRepository,
        IRepository<Category, int> categoryRepository,
        IRepository<UserLocation> userLocationRepository,
        IRepository<Game, int> gameRepository,
        IRepository<Map, int> mapRepository,
        IMapper mapper
    ) : ILocationService
    {
        public async Task<IList<LocationCateogryDto>> GetAllLocations(string gameSlug, string slug, string? userId)
        {
            var query =
                from games in gameRepository.AsQueryable()
                where games.Slug == gameSlug
                join maps in mapRepository.AsQueryable() on games.Id equals maps.GameId into maps
                from map in maps
                join category in categoryRepository.AsQueryable() on map.Id equals category.MapId into mapCategories
                from mapCategory in mapCategories.DefaultIfEmpty()
                where map.Slug == slug
                select mapCategory;

            var includedQuery = query.Include(x => x.Locations)
                .ThenInclude(x => x.UserLocations.Where(c => c.UserId == userId));

            var result = mapper.Map<IList<LocationCateogryDto>>(await includedQuery.ToListAsync());
            return result;
        }

        public async Task<int> GetCheckedCount(string gameSlug, string slug, string userId)
        {
            var query =
                from games in gameRepository.AsQueryable()
                where games.Slug == gameSlug
                join maps in mapRepository.AsQueryable() on games.Id equals maps.GameId into maps
                from map in maps
                join c in categoryRepository.AsQueryable() on map.Id equals c.MapId
                join l in locationRepository.AsQueryable() on c.Id equals l.CategoryId
                join ul in userLocationRepository.AsQueryable() on
                    new { l1 = l.Id, l2 = userId } equals new { l1 = ul.LocationId, l2 = ul.UserId }
                where map.Slug == slug
                where ul.Checked
                select ul;
            return await query.CountAsync();
        }

        public async Task<int> GetLocationCount(string gameSlug, string slug)
        {
            var query =
                from games in gameRepository.AsQueryable()
                where games.Slug == gameSlug
                join maps in mapRepository.AsQueryable() on games.Id equals maps.GameId into maps
                from map in maps
                join c in categoryRepository.AsQueryable() on map.Id equals c.MapId
                join l in locationRepository.AsQueryable() on c.Id equals l.CategoryId
                    into ml
                from mlc in ml.DefaultIfEmpty()
                where map.Slug == slug
                select ml;
            return await query.CountAsync();
        }

        public async Task<LocationDetailDto> GetLocationDetailById(int id)
        {
            return mapper.Map<LocationDetailDto>(
                await locationRepository.AsQueryable()
                    .Include(x => x.Medias)
                    .FirstOrDefaultAsync(x => x.Id == id));
        }

        public async Task<bool> ToggleLocation(int id, string userId)
        {
            var userLocation =
                await userLocationRepository.AddOrUpdateAsync(x => x.UserId == userId && x.LocationId == id, x =>
                {
                    x.LocationId = id;
                    x.UserId = userId;
                    x.Checked = !x.Checked;
                });

            return userLocation.Checked;
        }

        public async Task<IList<LocationSearchDto>> SearchInLocation(string gameSlug, string slug, string search)
        {
            var query =
                from games in gameRepository.AsQueryable()
                where games.Slug == gameSlug
                join maps in mapRepository.AsQueryable() on games.Id equals maps.GameId into maps
                from m in maps
                join c in categoryRepository.AsQueryable() on m.Id equals c.MapId
                join l in locationRepository.AsQueryable() on c.Id equals l.CategoryId
                    into ml
                from mapLocation in ml.DefaultIfEmpty()
                where m.Slug == slug
                      &&
                      mapLocation.Title.ToUpper().Contains(search.ToUpper())
                      ||
                      mapLocation.Title.ToUpper().Contains(search.ToUpper())
                select mapLocation;
            return mapper.Map<IList<LocationSearchDto>>(await query.ToListAsync());
        }
    }
}
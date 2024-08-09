using game_maps.Application.DTOs.MapGenie;
using game_maps.Domain.Entities.Game;
using Microsoft.AspNetCore.Http;

namespace game_maps.Application.Interfaces
{
    public interface IMapGenieService
    {
        public Task Scrap(MapGenieSetting setting, IFormFile mapDataFile);
    }
}

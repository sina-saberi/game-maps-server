using game_maps.Application.DTOs.MapGenie;
using game_maps.Domain.Entities.Game;
using Microsoft.AspNetCore.Http;

namespace game_maps.Application.Interfaces
{
    public interface IMapGenieService
    {
        public Task ToggleMapToGame(string slug, IList<IFormFile> files, string? userId);
        public Task Scrap(string slug, IList<IFormFile> files);
        public Task ScrapMarker(string slug);
    }
}
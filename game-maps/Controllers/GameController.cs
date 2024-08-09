using game_maps.Application.DTOs.Game;
using game_maps.Application.DTOs.MapGenie;
using game_maps.Application.Interfaces;
using game_maps.Domain.Entities.Game;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace game_maps.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController(IGameService _game) : ControllerBase
    {
        private readonly IGameService _game = _game;

        [HttpGet]
        public async Task<IEnumerable<GameDto>> Get()
        {
            return await _game.GetAllGames();
        }
    }
}

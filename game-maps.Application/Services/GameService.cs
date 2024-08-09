using AutoMapper;
using game_maps.Application.DTOs.Game;
using game_maps.Application.Interfaces;
using game_maps.Domain.Base;
using game_maps.Domain.Entities.Game;
using Microsoft.EntityFrameworkCore;
namespace game_maps.Application.Services
{
    public class GameService(IMapper mapper, IRepository<Game, int> _gameRepository) : IGameService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IRepository<Game, int> _gameRepository = _gameRepository;

        public async Task<IEnumerable<GameDto>> GetAllGames()
        {
            return _mapper.Map<List<GameDto>>(await _gameRepository.ToListAsync());
        }
    }
}

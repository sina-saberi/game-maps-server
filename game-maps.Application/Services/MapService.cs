using AutoMapper;
using game_maps.Application.DTOs.Map;
using game_maps.Application.Interfaces;
using game_maps.Domain.Base;
using game_maps.Domain.Entities.Game;
using game_maps.Domain.Entities.Map;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Application.Services
{
    public class MapService(IRepository<Map, int> mapRepository, IRepository<Game, int> gameRepository, IMapper mapper)
        : IMapService
    {
        private readonly IRepository<Map, int> _mapRepository = mapRepository;
        private readonly IRepository<Game, int> _gameRepository = gameRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<IList<MapDto>> GetAllAsync()
        {
            var result = await _mapRepository.ToListAsync();
            return _mapper.Map<List<MapDto>>(result);
        }

        public async Task<IList<MapDto>> GetAllAsync(string slug)
        {
            var game = await _gameRepository.AsQueryable()
                .Include(x => x.Maps)
                .FirstOrDefaultAsync(x => x.Slug == slug);

            return _mapper.Map<List<MapDto>>(game?.Maps);
        }

        public async Task<MapDetailDto> GetMapDetail(string slug)
        {
            var map = await _mapRepository.AsQueryable()
                .FirstOrDefaultAsync(x => x.Slug.Equals(slug));
            return _mapper.Map<MapDetailDto>(map);
        }
    }
}
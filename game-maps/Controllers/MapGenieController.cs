using game_maps.Application.DTOs.MapGenie;
using game_maps.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace game_maps.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapGenieController(IMapGenieService mapGenieService) : ControllerBase
    {
        [HttpPost]
        public async Task Post([FromForm] IList<IFormFile> mapDatas, [FromForm] MapGenieSetting setting)
        {
            var mapData = mapDatas.First();
            await mapGenieService.Scrap(setting, mapData);
        }
    }
}

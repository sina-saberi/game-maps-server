using game_maps.Application.DTOs.Category;
using game_maps.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace game_maps.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ICategoryService categoryService, ILocationService locationService) : ControllerBase
    {

        [HttpGet("{slug}")]
        public async Task<SideBarDto> Get(string slug)
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return new SideBarDto
            {
                LocationCount = await locationService.GetLocationCount(slug),
                CheckedCount = userId is not null ? await locationService.GetCheckedCount(slug, userId) : 0,
                Groups = await categoryService.GetGroupedCategories(slug, userId)
            };
        }
    }
}

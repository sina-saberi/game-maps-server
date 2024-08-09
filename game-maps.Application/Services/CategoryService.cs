using game_maps.Application.DTOs.Category;
using game_maps.Application.Interfaces;
using game_maps.Domain.Base;
using game_maps.Domain.Entities.Map;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Application.Services
{
    public class CategoryService(IRepository<Map, int> mapRepository) : ICategoryService
    {
        private readonly IRepository<Map, int> _mapRepository = mapRepository;
        public async Task<IList<GroupedCategoryDto>> GetGroupedCategories(string slug, string? userId)
        {
            var map = await _mapRepository.AsQueryable()
                .Include(x => x.Categories)
                .ThenInclude(x => x.Locations)
                .ThenInclude(x => x.UserLocations.Where(c => c.UserId == userId))
                .FirstOrDefaultAsync(x => x.Slug.Equals(slug));

            var groupedCategoryDtos = new List<GroupedCategoryDto>();
            if (map?.Categories != null)
            {
                var groupedCategories = map.Categories.GroupBy(c => c.Group);
                foreach (var group in groupedCategories)
                {
                    var groupedCategoryDto = new GroupedCategoryDto
                    {
                        Title = group.Key,
                        categories = group.Select(category => new CategoryDto
                        {
                            Id = category.Id,
                            Title = category.Title,
                            Icon = category.Icon,
                            Info = category.Info,
                            Template = category.Template,
                            Order = category.Order,
                            HasHeatmap = category.HasHeatmap,
                            FeaturesEnabled = category.FeaturesEnabled,
                            DisplayType = category.DisplayType,
                            IgnEnabled = category.IgnEnabled,
                            IgnVisible = category.IgnVisible,
                            Visible = category.Visible,
                            Description = category.Description,
                            LocationCount = category.Locations.Count,
                            CheckedCount = category.Locations.Where(x => x.UserLocations.Any(c => c.Checked)).Count()
                        }).ToList()
                    };
                    groupedCategoryDtos.Add(groupedCategoryDto);
                }
            }
            return groupedCategoryDtos;
        }
    }
}

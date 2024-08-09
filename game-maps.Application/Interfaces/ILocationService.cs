using game_maps.Application.DTOs.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Application.Interfaces
{
    public interface ILocationService
    {
        public Task<IList<LocationCateogryDto>> GetAllLocations(string slug, string? userId);
        public Task<LocationDetailDto> GetLocationDetailById(int id);
        public Task<bool> ToggleLocation(int id, string userId);
        public Task<int> GetCheckedCount(string slug, string userId);
        public Task<int> GetLocationCount(string slug);
        public Task<IList<LocationSearchDto>> SearchInLocation(string slug, string search);
    }
}

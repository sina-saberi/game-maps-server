using game_maps.Domain.Base;
using game_maps.Domain.Entities.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Domain.Entities.TileSet
{
    public class TileSet : Entity<int>
    {
        public int MapId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public int MinZoom { get; set; }
        public int MaxZoom { get; set; }
    }
}

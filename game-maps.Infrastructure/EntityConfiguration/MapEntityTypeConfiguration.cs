using game_maps.Domain.Entities.Map;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Infrastructure.EntityConfiguration
{
    internal class MapEntityTypeConfiguration : IEntityTypeConfiguration<Map>
    {
        public void Configure(EntityTypeBuilder<Map> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.GameId).IsRequired();
            builder.Property(m => m.Name).IsRequired().HasMaxLength(300);
            builder.Property(m => m.Description).HasMaxLength(1000);
            builder.Property(m => m.Slug).HasMaxLength(400);
            builder.OwnsOne(x => x.MapConfig);
            builder.HasMany(x => x.TileSets).WithOne().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Categories).WithOne().HasForeignKey(x => x.MapId);
        }
    }
}

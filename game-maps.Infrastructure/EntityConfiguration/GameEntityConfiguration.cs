using game_maps.Domain.Entities.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Infrastructure.EntityConfiguration
{
    internal class GameEntityConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(300);
            builder.Property(m => m.Description).HasMaxLength(1000);
            builder.Property(m => m.Slug).HasMaxLength(500);
            builder.HasMany(x => x.Maps)
                .WithOne()
                .HasForeignKey(x => x.GameId);
        }
    }
}

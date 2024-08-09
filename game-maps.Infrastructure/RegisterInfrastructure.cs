using game_maps.Domain.Base;
using game_maps.Domain.Entities.User;
using game_maps.Infrastructure.Base;
using game_maps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Infrastructure
{
    public static class Infrastructure
    {
        public static void RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var databasePath = Path.Combine(Directory.GetCurrentDirectory(), "app.db");
            services.AddDbContext<GameMapsDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            services.Configure<IdentityOptions>(options =>
            {
                
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            });



            services.AddIdentity<User, IdentityRole>()
              .AddEntityFrameworkStores<GameMapsDbContext>();


            services.AddScoped(typeof(IRepository<,>), typeof(BaseRepository<,>));
            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        }
    }
}

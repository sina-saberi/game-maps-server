using game_maps.Application.Interfaces;
using game_maps.Application.Mapper;
using game_maps.Application.Services;
using game_maps.Domain.Base;
using game_maps.Infrastructure.Base;
using game_maps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Test
{
    public class TestFixture : IDisposable
    {
        public ServiceProvider ServiceProvider { get; private set; }

        public TestFixture()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?
                .Parent?.Parent?.Parent?.FullName;
            if (solutionDirectory is null) throw new Exception("problem with finding direcoty");
            var databasePath = Path.Combine(solutionDirectory, "game-maps", "app.db");

            services.AddDbContext<GameMapsDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            services.AddScoped(typeof(IRepository<,>), typeof(BaseRepository<,>));
            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

            services.AddAutoMapper(typeof(MappingProfile).Assembly);

            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IMapGenieService, MapGenieService>();
            services.AddScoped<IMapService, MapService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ILocationService, LocationService>();
        }

        public void Dispose()
        {
            // Dispose the service provider when the fixture is disposed
            ServiceProvider?.Dispose();
        }
    }
}

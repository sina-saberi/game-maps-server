﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using game_maps.Application.Mapper;
using game_maps.Infrastructure;
using game_maps.Application.Interfaces;
using game_maps.Application.Services;
using Microsoft.AspNetCore.Builder;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace game_maps.Application
{
    public static class Application
    {
        public static void RegisterApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(typeof(MappingProfile).Assembly);
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IMapGenieService, MapGenieService>();
            services.AddScoped<IMapService, MapService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.RegisterInfrastructure(configuration);
        }

        public static void RegisterAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var secret = configuration["JwtConfig:Secret"]!;
            var key = Encoding.ASCII.GetBytes(secret);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        }
    }
}

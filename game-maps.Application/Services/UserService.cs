using game_maps.Application.DTOs.Auth;
using game_maps.Application.Interfaces;
using game_maps.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Application.Services
{
    public class UserService(ITokenService tokenService, UserManager<User> _userManager) : IUserService
    {
        public async Task<LoginResponseDto> Login(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = tokenService.GenerateAccessToken(user);
                var refreshToken = tokenService.GenerateRefreshToken(user);

                return new LoginResponseDto()
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(30),
                };
            }

            throw new Exception("could not found user");
        }

        public async Task Register(RegisterDto model)
        {
            var user = new User { UserName = model.Username, Email = model.Email };
            await _userManager.CreateAsync(user, model.Password);
        }
    }
}

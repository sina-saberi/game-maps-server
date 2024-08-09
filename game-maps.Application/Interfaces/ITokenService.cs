using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Application.Interfaces
{
    public interface ITokenService
    {
        public string GenerateAccessToken(IdentityUser user);
        public string GenerateRefreshToken(IdentityUser user);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiles.Core.DTO.UserDto
{
    public class TokenResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserResponseDto userData { get; set; } = new UserResponseDto();
    }
}

using Microsoft.AspNetCore.Mvc;
using Tiles.Core.DTO.UserDto;
using Tiles.Core.ServiceContracts.UserManagement.Application.Interfaces;

namespace TilesBackendApI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase // ✅ Match route with class name: "auth"
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

      [HttpPost("register")]
public async Task<IActionResult> RegisterUser([FromBody] UserRequestDto dto)
{
    // Validate the model
    if (!ModelState.IsValid)
    {
        // Collect validation error messages
        var errors = ModelState.Values.SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();
        
        // Return validation errors
        return BadRequest(new { msg = string.Join(", ", errors) });
    }

    var result = await _userService.RegisterUserAsync(dto);
    if (!result.Success)
        return BadRequest(new { msg = result.Message });

    return Created("", new { msg = result.Message });
}


        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string search = "", [FromQuery] int pageNo = 1, [FromQuery] int rowsPerPage = 10)
        {
            var result = await _userService.GetUsersAsync(search, pageNo, rowsPerPage);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { msg = "User not found" });

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditUser(Guid id, [FromBody] UserRequestDto dto)
        {
            var result = await _userService.UpdateUserAsync(id, dto);
            if (!result.Success)
                return NotFound(new { msg = result.Message });

            return Ok(new { msg = result.Message });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result.Success)
                return NotFound(new { msg = result.Message });

            return Ok(new { msg = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _userService.LoginAsync(dto);
            if (!result.Success)
                return Unauthorized(new { msg = result.Message });

            return Ok(result.Data);
        }

        [HttpPut("updatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var result = await _userService.UpdatePasswordAsync(dto);
            if (!result.Success)
                return BadRequest(new { msg = result.Message });

            return Ok(new { msg = result.Message });
        }

        [HttpPost("forgotPasswordEmail")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _userService.ForgotPasswordAsync(dto.Email);
            if (!result.Success)
                return NotFound(new { msg = result.Message });

            return Ok(new { msg = "OTP sent successfully" });
        }

        [HttpPost("verifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto dto)
        {
            var result = await _userService.VerifyOtpAsync(dto.Email, dto.Otp);
            if (!result.Success)
                return BadRequest(new { msg = result.Message });

            return Ok(new { msg = "OTP verified successfully" });
        }

        [HttpGet("parse-example")]
        public IActionResult ParseExample([FromQuery] string? input)
        {
            if (string.IsNullOrEmpty(input))
                return BadRequest(new { msg = "Input cannot be null or empty" });

            try
            {
                int parsedValue = int.Parse(input);
                return Ok(new { parsedValue });
            }
            catch (FormatException)
            {
                return BadRequest(new { msg = "Input is not a valid integer" });
            }
        }
    }
}

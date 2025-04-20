using Microsoft.AspNetCore.Mvc;
using Tiles.Core.DTO.UserDto;
using Tiles.Core.ServiceContracts.UserManagement.Application.Interfaces;

namespace TilesBackendApI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
  
    
        public class UserController : ControllerBase
        {
            private readonly IUserService _userService;

            // Constructor to inject the user service dependency
            public UserController(IUserService userService)
            {
                _userService = userService;
            }

            // Register a new user
            [HttpPost("/api/auth/register/")]
            public async Task<IActionResult> RegisterUser([FromBody] UserRequestDto dto)
            {
                var result = await _userService.RegisterUserAsync(dto);

                // Check if registration was successful, return appropriate response
                if (!result.Success)
                    return BadRequest(new { msg = result.Message });

                return Created("", new { msg = result.Message });
            }

            // Get a list of users with pagination and optional search filter
            [HttpGet("/api/auth/")]
            public async Task<IActionResult> GetUsers([FromQuery] string search = "", [FromQuery] int pageNo = 1, [FromQuery] int rowsPerPage = 10)
            {
                var result = await _userService.GetUsersAsync(search, pageNo, rowsPerPage);
                return Ok(result);
            }

            // Get user by ID
            [HttpGet("/api/auth/{id}")]
            public async Task<IActionResult> GetUserById(Guid id)
            {
                var user = await _userService.GetUserByIdAsync(id);

                // Return 404 if user not found
                if (user == null)
                    return NotFound(new { msg = "User not found" });

                return Ok(user);
            }

            // Edit an existing user by their ID
            [HttpPut("/api/auth/{id}")]
            public async Task<IActionResult> EditUser(Guid id, [FromBody] UserRequestDto dto)
            {
                var result = await _userService.UpdateUserAsync(id, dto);

                // Return 404 if user update failed
                if (!result.Success)
                    return NotFound(new { msg = result.Message });

                return Ok(new { msg = result.Message });
            }

            // Delete a user by their ID
            [HttpDelete("/api/auth/{id}")]
            public async Task<IActionResult> DeleteUser(Guid id)
            {
                var result = await _userService.DeleteUserAsync(id);

                // Return 404 if deletion failed
                if (!result.Success)
                    return NotFound(new { msg = result.Message });

                return Ok(new { msg = result.Message });
            }

            // User login with email and password
            [HttpPost("/api/auth/login")]
            public async Task<IActionResult> Login([FromBody] LoginDto dto)
            {
                var result = await _userService.LoginAsync(dto);

                // Return Unauthorized if login fails
                if (!result.Success)
                    return Unauthorized(new { msg = result.Message });

                return Ok(result.Data);
            }

            // Update user password
            [HttpPut("/api/auth/updatePassword")]
            public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
            {
                var result = await _userService.UpdatePasswordAsync(dto);

                // Return BadRequest if password update fails
                if (!result.Success)
                    return BadRequest(new { msg = result.Message });

                return Ok(new { msg = result.Message });
            }

            // Forgot password - sends an OTP to the user's email
            [HttpPost("/api/forgot/forgotPasswordEmail")]
            public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
            {
                var result = await _userService.ForgotPasswordAsync(dto.Email);

                // Return NotFound if OTP sending fails
                if (!result.Success)
                    return NotFound(new { msg = result.Message });

                return Ok(new { msg = "OTP sent successfully" });
            }

            // Verify OTP sent for password reset
            [HttpPost("/api/forgot/verifyOtp")]
            public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto dto)
            {
                var result = await _userService.VerifyOtpAsync(dto.Email, dto.Otp);

                // Return BadRequest if OTP verification fails
                if (!result.Success)
                    return BadRequest(new { msg = result.Message });

                return Ok(new { msg = "OTP verified successfully" });
            }

           // Fix for CS8604: Possible null reference argument for parameter 's' in 'int int.Parse(string s)'.
           // Ensure that the string being passed to int.Parse is not null by using null-coalescing operator or null check.

          [HttpGet("parse-example")]
           public IActionResult ParseExample([FromQuery] string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return BadRequest(new { msg = "Input cannot be null or empty" });
            }

            int parsedValue;
            try
            {
                parsedValue = int.Parse(input);
            }
            catch (FormatException)
            {
                return BadRequest(new { msg = "Input is not a valid integer" });
            }

            return Ok(new { parsedValue });
        }


        }



        //[HttpPost("forgot-password")]
        //public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        //{
        //    var result = await _userService.SendOtpAsync(dto.Email);
        //    if (!result.Success)
        //        return BadRequest(new { msg = result.Message });

        //    return Ok(new { msg = result.Message });
        //}

        //[HttpPost("verify-otp")]
        //public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto dto)
        //{
        //    var result = await _userService.VerifyOtpAsync(dto.Email, dto.Otp);
        //    if (!result.Success)
        //        return BadRequest(new { msg = result.Message });

        //    return Ok(new { msg = result.Message });
        //}

        // POST api/email/send
        //[HttpPost("send")]
        //public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        //{
        //    if (string.IsNullOrEmpty(request.To) || string.IsNullOrEmpty(request.Subject) || string.IsNullOrEmpty(request.Body))
        //    {
        //        return BadRequest("To, Subject, and Body are required.");
        //    }

        //    try
        //    {
        //        await _userService.SendEmailAsync(request.To, request.Subject, request.Body);
        //        return Ok("Email sent successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}


    }



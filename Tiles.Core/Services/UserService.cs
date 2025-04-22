using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Tiles.Core.Domain.Entites;
using Tiles.Core.Domain.RepositroyContracts;
using Tiles.Core.ServiceContracts.UserManagement.Application.Interfaces;
using Tiles.Core.Wrappers;
using MailKit.Net.Smtp;
using Tiles.Core.DTO.UserDto;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    // Constructor with DI for user repository and configuration
    public UserService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    // Registers a new user with a temporary password and sends a welcome email
   public async Task<ServiceResult> RegisterUserAsync(UserRequestDto dto)
{
    try
    {
        // Check if email is already registered
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            return ServiceResult.CreateFailure("User already exists");

        // Generate and hash temporary password
        var tempPassword = GenerateTemporaryPassword();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // Generate unique serial number
        var serialNumber = await _userRepository.GetNextSerialNumberAsync();

        // Create new user entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            Designation = dto.Designation,
            Phone = dto.Phone,
            IsActive = dto.IsActive,
            SerialNumber = serialNumber,
            PasswordHash = hashedPassword,
            IsFirst = true // Indicates first-time login
        };

        await _userRepository.AddAsync(user);

        // Compose welcome email with temp password
        var htmlContent = $@"
        <p>Hello {dto.Name},</p>
        <p>Your temporary password is: <b>{tempPassword}</b></p>
        <p>Click <a href=""https://localhost:5173"">here</a> to log in.</p>";

        await SendEmailAsync(dto.Email, dto.Name, "Welcome! Set up your password", htmlContent);

        return ServiceResult.CreateSuccess("User registered successfully");
    }
    catch (Exception ex)
    {
        //// Log the error details for debugging (ensure you have a logger injected into the service)
        //_logger.LogError(ex, "Error occurred while registering user.");
        
        return ServiceResult.CreateFailure("An unexpected error occurred. Please try again later.");
    }
}


    // Authenticates user and generates token (stubbed)
    public async Task<ServiceResult<TokenResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return ServiceResult<TokenResponseDto>.CreateFailure("Invalid email or password.");

        // Generate a JWT token (stubbed for now)
        var token = GenerateToken(user);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            SerialNumber = user.SerialNumber,
            Name = user.Name,
            Email = user.Email,
            Designation = user.Designation,
            Phone = user.Phone,
            IsActive = user.IsActive
        };

        return ServiceResult<TokenResponseDto>.CreateSuccess(new TokenResponseDto
        {
            Token = token,
            userData = userDto
        }, "Login successful.");
    }

    // Updates an existing user's profile
    public async Task<ServiceResult> UpdateUserAsync(Guid id, UserRequestDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return ServiceResult.CreateFailure("User not found.");

        // Update fields
        user.Name = dto.Name;
        user.Designation = dto.Designation;
        user.Phone = dto.Phone;
        user.IsActive = dto.IsActive;

        await _userRepository.UpdateAsync(user);
        return ServiceResult.CreateSuccess("User updated.");
    }

    // Gets user details by ID
    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : new UserResponseDto
        {
            Id = user.Id,
            SerialNumber = user.SerialNumber,
            Name = user.Name,
            Email = user.Email,
            Designation = user.Designation,
            Phone = user.Phone,
            IsActive = user.IsActive
        };
    }

    // Gets paginated and searchable list of users
    public async Task<PaginatedResult<UserResponseDto>> GetUsersAsync(string search, int pageNo, int rowsPerPage)
    {
        var users = await _userRepository.GetUsersAsync(search, pageNo, rowsPerPage);
        var total = await _userRepository.GetTotalCountAsync(search);

        var result = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            SerialNumber = u.SerialNumber,
            Name = u.Name,
            Designation = u.Designation,
            Email = u.Email,
            Phone = u.Phone,
            IsActive = u.IsActive
        }).ToList();

        return PaginatedResult<UserResponseDto>.CreateSuccess(result, pageNo, rowsPerPage, total);
    }

    // Deletes user by ID
    public async Task<ServiceResult> DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return ServiceResult.CreateFailure("User not found.");

        await _userRepository.DeleteAsync(user);
        return ServiceResult.CreateSuccess("User deleted.");
    }

    // Updates user's password and clears IsFirst flag
    public async Task<ServiceResult> UpdatePasswordAsync(UpdatePasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null) return ServiceResult.CreateFailure("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.IsFirst = false;
        await _userRepository.UpdateAsync(user);

        return ServiceResult.CreateSuccess("Password updated.");
    }

    // Sends OTP to user's email for password reset
    public async Task<ServiceResult> ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return ServiceResult.CreateFailure("User not found.");

        // Generate OTP and expiry
        var otp = new Random().Next(100000, 999999).ToString();
        var otpExpiry = DateTime.Now.AddMinutes(10);

        user.Otp = otp;
        user.OtpExpiry = otpExpiry;
        await _userRepository.UpdateAsync(user);

        // Compose and send OTP email
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
        var fromEmail = _configuration["EmailSettings:Email"];
        var fromPassword = _configuration["EmailSettings:Password"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Endless Enterprise", fromEmail));
        message.To.Add(new MailboxAddress(user.Name, user.Email));
        message.Subject = "Reset Your Password";
        message.Body = new TextPart("html")
        {
            Text = $"<p>Your OTP for password reset is: <b>{otp}</b></p><p>This OTP will expire in 10 minutes.</p>"
        };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(fromEmail, fromPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        return ServiceResult.CreateSuccess("OTP sent successfully");
    }

    // Sends email using SMTP configuration
    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPortString = _configuration["EmailSettings:Port"];
            var fromEmail = _configuration["EmailSettings:Email"];
            var fromPassword = _configuration["EmailSettings:Password"];

            if (string.IsNullOrEmpty(smtpPortString))
                throw new InvalidOperationException("SMTP port configuration is missing or invalid.");

            var smtpPort = int.Parse(smtpPortString);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Endless Enterprise", fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(fromEmail, fromPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email sending error: {ex.Message}");
            throw;
        }
    }

    // Verifies OTP for password reset
    public async Task<ServiceResult> VerifyOtpAsync(string email, string otp)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.Otp != otp)
            return ServiceResult.CreateFailure("Invalid or expired OTP");

        // Check OTP expiry
        if (DateTime.Now > user.OtpExpiry)
            return ServiceResult.CreateFailure("OTP expired");

        return ServiceResult.CreateSuccess("OTP verified successfully");
    }

    // Generates a random temporary password
    private string GenerateTemporaryPassword(int length = 10)
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$!";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[new Random().Next(s.Length)]).ToArray());
    }

    // Placeholder JWT generator (replace with real JWT logic)
    private string GenerateToken(User user)
    {
        return "jwt-token-placeholder";
    }

    // Unused method stub
    // public Task<ServiceResult> SendOtpAsync(string email)
    // {
    //     throw new NotImplementedException();
    // }
}

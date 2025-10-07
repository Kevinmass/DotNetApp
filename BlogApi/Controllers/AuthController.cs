using BlogData.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Message = "Invalid model" });

        // Multiple conditions for unit testing
        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new { Message = "Username cannot be null or empty" });

        if (request.UserName.Length < 2)
            return BadRequest(new { Message = "Username must be at least 2 characters long" });

        if (request.UserName.Length > 50)
            return BadRequest(new { Message = "Username cannot exceed 50 characters" });

        if (!request.UserName.All(char.IsLetterOrDigit))
            return BadRequest(new { Message = "Username can only contain letters and digits" });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Message = "Password cannot be null or empty" });

        if (request.Password.Length < 3)
            return BadRequest(new { Message = "Password must be at least 3 characters long" });

        if (request.Password.Length > 100)
            return BadRequest(new { Message = "Password cannot exceed 100 characters" });

        if (!request.Password.Any(char.IsUpper))
            return BadRequest(new { Message = "Password must contain at least one uppercase letter" });

        if (!request.Password.Any(char.IsLower))
            return BadRequest(new { Message = "Password must contain at least one lowercase letter" });

        if (!request.Password.Any(char.IsDigit))
            return BadRequest(new { Message = "Password must contain at least one digit" });

        if (!request.Password.Any(ch => !char.IsLetterOrDigit(ch)))
            return BadRequest(new { Message = "Password must contain at least one special character" });

        var user = new IdentityUser
        {
            UserName = request.UserName,
            Email = request.UserName + "@test.com", // Dummy email for testing
            EmailConfirmed = true // For simplicity, skip email confirmation
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { Message = "User creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        // Auto sign in after registration
        await _signInManager.SignInAsync(user, isPersistent: false);

        var token = await GenerateJwtToken(user);

        return Ok(new
        {
            Token = token,
            User = new
            {
                user.Id,
                user.Email,
                user.UserName
            }
        });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _signInManager.PasswordSignInAsync(request.UserName, request.Password, false, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid username or password" });
        }

        var user = await _userManager.FindByNameAsync(request.UserName);
        var token = await GenerateJwtToken(user!);

        return Ok(new
        {
            Token = token,
            User = new
            {
                user!.Id,
                user.Email,
                user.UserName
            }
        });
    }

    // POST: api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { Message = "Logged out successfully" });
    }

    // GET: api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.UserName
        });
    }

    private async Task<string> GenerateJwtToken(IdentityUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong";
        var issuer = jwtSettings["Issuer"] ?? "BlogApi";
        var audience = jwtSettings["Audience"] ?? "BlogApp";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class RegisterRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

using System.Security.Claims;
using CollabDocs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace CollabDocs.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Auth : ControllerBase
{
    
    private readonly AuthService _authService;
    
    public Auth(AuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        var result = await _authService.RegisterAsync(registerRequest.Email, registerRequest.Password);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }
        return Ok(new
        {
            message = result.Message,
            id = result.user!.Id,
            email = result.user.Email
        });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var result = await _authService.LoginAsync(loginRequest.Email, loginRequest.Password);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            token = result.Token,
            id = result.user!.Id,
            email = result.user.Email
        });
    }
    
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok($"Successfully authenticated as {userId}");
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
using CollabDocs.Services;
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
    public async Task<IActionResult> Login()
    {
        return Ok();
    }
}

public record RegisterRequest(string Email, string Password);
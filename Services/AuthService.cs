using CollabDocs.Domain.Models;
using CollabDocs.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CollabDocs.Services;

public class AuthService
{
    private readonly AppDbContext _dbContext;
    private readonly JwtService _jwtService;

    public AuthService(AppDbContext dbContext, JwtService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<(bool Success, string Message, User? user)> RegisterAsync(string email, string password)
    {
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            return (false, "Emails is already in use", null);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };
        
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        
        return (true, "User created successfully", user);   
    }
    
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return new LoginResult(false, "Invalid Email", null, null);
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!passwordValid)
        {
            return new LoginResult(false, "Invalid Password", null, null);
        }

        var token = _jwtService.GenerateToken(user.Id, user.Email);

        return new LoginResult(true, "Login successful", token, user);
    }
}

public record LoginResult(bool Success, string Message, string? Token, User? user);
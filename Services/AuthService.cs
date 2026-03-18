using CollabDocs.Domain.Models;
using CollabDocs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace CollabDocs.Services;

public class AuthService
{
    private readonly AppDbContext _dbContext;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
    
}
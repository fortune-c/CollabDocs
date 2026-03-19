using CollabDocs.Domain.Enums;
using CollabDocs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace CollabDocs.Services;

public class PermissionService
{
    private readonly AppDbContext _dbContext;
    
    public PermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentRole?> GetUserRoleAsync(Guid documentId, Guid userId)
    {
        var permission = await _dbContext.DocumentPermissions
            .FirstOrDefaultAsync(p => p.DocumentId == documentId && p.UserId == userId);

        return permission?.Role;
    }

    public async Task<bool> CanViewAsync(Guid documentId, Guid userId)
    {
        var role = await GetUserRoleAsync(documentId, userId);
        return role == DocumentRole.Owner || role == DocumentRole.Editor || role == DocumentRole.Viewer;
    }
    
    public async Task<bool> CanEditAsync(Guid documentId, Guid userId)
    {
        var role = await GetUserRoleAsync(documentId, userId);
        return role == DocumentRole.Owner || role == DocumentRole.Editor;
    }
    
    public async Task<bool> CanDeleteAsync(Guid documentId, Guid userId)
    {
        var role = await GetUserRoleAsync(documentId, userId);
        return role == DocumentRole.Owner;
    }
    
    public async Task<bool> CanShareAsync(Guid documentId, Guid userId)
    {
        var role = await GetUserRoleAsync(documentId, userId);
        return role == DocumentRole.Owner;
    }
}
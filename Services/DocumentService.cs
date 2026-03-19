using CollabDocs.Domain.Enums;
using CollabDocs.Domain.Models;
using CollabDocs.Infrastructure.Database;

namespace CollabDocs.Services;

public class DocumentService
{
    private readonly PermissionService _permissionService;
    private readonly AppDbContext _dbContext;
    
    public DocumentService(PermissionService permissionService, AppDbContext dbContext)
    {
        _permissionService = permissionService;
        _dbContext = dbContext;
    }
    
    // document creation
    public async Task<Document> CreateDocumentAsync(CreateDocumentRequest request, Guid userId)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            OwnerId = userId
        };

        _dbContext.Documents.Add(document);

        var ownerPermission = new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            UserId = userId,
            Role = DocumentRole.Owner
        };

        _dbContext.DocumentPermissions.Add(ownerPermission);

        await _dbContext.SaveChangesAsync();

        return document;
    }
    
    
    
    // document delete
    public async Task<string> DeleteDocumentAsync(Guid id)
    {
        var document = await _dbContext.Documents.FindAsync(id);
        
        if (document == null)
        {
            throw new Exception("Document not found");
        }

        var permissions = _dbContext.DocumentPermissions
            .Where(p => p.DocumentId == id);
        
        _dbContext.DocumentPermissions.RemoveRange(permissions);
        _dbContext.Documents.Remove(document);
        
        await _dbContext.SaveChangesAsync();
        
        return $"Document deleted: {id}";
    }
    
}

public record CreateDocumentRequest(string Title, string Content);
public record UpdateDocumentRequest(string Title, string Content);
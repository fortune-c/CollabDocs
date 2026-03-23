using CollabDocs.Domain.Enums;
using CollabDocs.Domain.Models;
using CollabDocs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

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
        
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Content = document.Content,
            VersionNumber = 1,
            CreatedAt = DateTime.UtcNow
        };
        
        _dbContext.DocumentVersions.Add(version);

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
    
    // document version history
    public async Task<(bool Success, string Message, List<DocumentVersionDto>? Versions)> GetHistoryAsync(Guid documentId)
        {
            var documentExists = await _dbContext.Documents
                .AnyAsync(d => d.Id == documentId);
    
            if (!documentExists)
            {
                return (false, "Document not found.", null);
            }
    
            var versions = await _dbContext.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderBy(v => v.VersionNumber)
                .Select(v => new DocumentVersionDto(
                    v.Id,
                    v.DocumentId,
                    v.VersionNumber,
                    v.Content,
                    v.CreatedAt
                ))
                .ToListAsync();
    
            return (true, "History loaded successfully.", versions);
        }
    
    // apply edit operation
    public async Task<(bool Success, string Message, string? UpdatedContent)> ApplyEditOperationAsync(ApplyEditOperationRequest request, Guid userId)
    {
        var document = await _dbContext.Documents.FindAsync(request.DocumentId);

        if (document == null)
        {
            return (false, "Document not found.", null);
        }

        var canEdit = await _permissionService.CanEditAsync(request.DocumentId, userId);
        if (!canEdit)
        {
            return (false, "User is not allowed to edit this document.", null);
        }

        var currentContent = document.Content;
        string updatedContent;

        switch (request.OperationType.Trim().ToLowerInvariant())
        {
            case "insert":
                if (request.Position < 0 || request.Position > currentContent.Length)
                    return (false, "Invalid insert position.", null);

                if (request.Text is null)
                    return (false, "Insert text is required.", null);

                updatedContent = currentContent.Insert(request.Position, request.Text);
                break;

            case "delete":
                if (request.Position < 0 || request.Position >= currentContent.Length)
                    return (false, "Invalid delete position.", null);

                if (request.Length is null || request.Length <= 0)
                    return (false, "Delete length is required.", null);

                if (request.Position + request.Length > currentContent.Length)
                    return (false, "Delete range exceeds document length.", null);

                updatedContent = currentContent.Remove(request.Position, request.Length.Value);
                break;

            case "replace":
                if (request.Position < 0 || request.Position > currentContent.Length)
                    return (false, "Invalid replace position.", null);

                if (request.Length is null || request.Length < 0)
                    return (false, "Replace length is required.", null);

                if (request.Position + request.Length > currentContent.Length)
                    return (false, "Replace range exceeds document length.", null);

                if (request.Text is null)
                    return (false, "Replace text is required.", null);

                updatedContent = currentContent.Remove(request.Position, request.Length.Value)
                                               .Insert(request.Position, request.Text);
                break;

            default:
                return (false, "Unsupported operation type.", null);
        }

        document.Content = updatedContent;

        var nextVersionNumber = await _dbContext.DocumentVersions
            .Where(v => v.DocumentId == request.DocumentId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync() ?? 0;

        _dbContext.DocumentVersions.Add(new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Content = updatedContent,
            VersionNumber = nextVersionNumber + 1,
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.DocumentEdits.Add(new DocumentEdit
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            UserId = userId,
            OperationType = request.OperationType,
            Position = request.Position,
            Length = request.Length,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        return (true, "Edit applied successfully.", updatedContent);
    }
    
    public async Task<bool> CanEditAsync(Guid documentId, Guid userId)
    {
        return await _dbContext.DocumentPermissions.AnyAsync(p =>
            p.DocumentId == documentId &&
            p.UserId == userId &&
            (p.Role == DocumentRole.Owner || p.Role == DocumentRole.Editor));
    }
}

public record CreateDocumentRequest(string Title, string Content);
public record UpdateDocumentRequest(string Title, string Content);
public record DocumentVersionDto(Guid Id, Guid DocumentId, int VersionNumber, string Content, DateTime CreatedAt);
public record ApplyEditOperationRequest(Guid DocumentId, string OperationType, int Position, int? Length, string? Text);
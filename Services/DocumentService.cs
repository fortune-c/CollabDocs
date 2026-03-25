using CollabDocs.Domain.Enums;
using CollabDocs.Domain.Models;
using CollabDocs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CollabDocs.Services;

public class DocumentService
{
    private readonly PermissionService _permissionService;
    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;
    
    public DocumentService(PermissionService permissionService, AppDbContext dbContext, IMemoryCache cache)
    {
        _permissionService = permissionService;
        _dbContext = dbContext;
        _cache = cache;
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
        var canEdit = await _permissionService.CanEditAsync(request.DocumentId, userId);
        if (!canEdit)
        {
            return (false, "User is not allowed to edit this document.", null);
        }

        var cacheKey = $"doc_state_{request.DocumentId}";
        if (!_cache.TryGetValue(cacheKey, out CachedDocumentState docState))
        {
            var document = await _dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DocumentId);
            if (document == null)
            {
                return (false, "Document not found.", null);
            }

            var version = await _dbContext.DocumentVersions
                .Where(v => v.DocumentId == request.DocumentId)
                .Select(v => (int?)v.VersionNumber)
                .MaxAsync() ?? 0;

            docState = new CachedDocumentState(document.Content, version);
            _cache.Set(cacheKey, docState, TimeSpan.FromHours(1));
        }

        var currentVersion = docState.VersionNumber;
        var transformedRequest = request;

        if (request.BaseVersion < currentVersion)
        {
            int editsToFetch = currentVersion - request.BaseVersion;
            var priorEdits = await _dbContext.DocumentEdits
                .Where(e => e.DocumentId == request.DocumentId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(editsToFetch)
                .ToListAsync();

            priorEdits.Reverse(); // Process oldest to newest
            transformedRequest = EditTransformService.Transform(request, priorEdits);
        }

        var currentContent = docState.Content;
        string updatedContent;

        switch (transformedRequest.OperationType.Trim().ToLowerInvariant())
        {
            case "insert":
                if (transformedRequest.Position < 0 || transformedRequest.Position > currentContent.Length)
                    return (false, "Invalid insert position.", null);

                if (transformedRequest.Text is null)
                    return (false, "Insert text is required.", null);

                updatedContent = currentContent.Insert(transformedRequest.Position, transformedRequest.Text);
                break;

            case "delete":
                if (transformedRequest.Position < 0 || transformedRequest.Position > currentContent.Length)
                    return (false, "Invalid delete position.", null);

                if (transformedRequest.Length is null || transformedRequest.Length < 0)
                    return (false, "Delete length is required.", null);

                if (transformedRequest.Position + transformedRequest.Length > currentContent.Length)
                    return (false, "Delete range exceeds document length.", null);

                // If length is 0 due to an overlapping delete transformation, content remains unchanged
                updatedContent = transformedRequest.Length == 0 
                    ? currentContent 
                    : currentContent.Remove(transformedRequest.Position, transformedRequest.Length.Value);
                break;

            case "replace":
                if (transformedRequest.Position < 0 || transformedRequest.Position > currentContent.Length)
                    return (false, "Invalid replace position.", null);

                if (transformedRequest.Length is null || transformedRequest.Length < 0)
                    return (false, "Replace length is required.", null);

                if (transformedRequest.Position + transformedRequest.Length > currentContent.Length)
                    return (false, "Replace range exceeds document length.", null);

                if (transformedRequest.Text is null)
                    return (false, "Replace text is required.", null);

                updatedContent = transformedRequest.Length == 0
                    ? currentContent.Insert(transformedRequest.Position, transformedRequest.Text)
                    : currentContent.Remove(transformedRequest.Position, transformedRequest.Length.Value)
                                    .Insert(transformedRequest.Position, transformedRequest.Text);
                break;

            default:
                return (false, "Unsupported operation type.", null);
        }

        docState = new CachedDocumentState(updatedContent, currentVersion + 1);
        _cache.Set(cacheKey, docState, TimeSpan.FromHours(1));

        var docToUpdate = new Document { Id = request.DocumentId, Content = updatedContent };
        _dbContext.Documents.Attach(docToUpdate);
        _dbContext.Entry(docToUpdate).Property(d => d.Content).IsModified = true;

        _dbContext.DocumentVersions.Add(new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = request.DocumentId,
            Content = updatedContent,
            VersionNumber = docState.VersionNumber,
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.DocumentEdits.Add(new DocumentEdit
        {
            Id = Guid.NewGuid(),
            DocumentId = request.DocumentId,
            UserId = userId,
            OperationType = transformedRequest.OperationType,
            Position = transformedRequest.Position,
            Length = transformedRequest.Length,
            Text = transformedRequest.Text,
            BaseVersion = request.BaseVersion,
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
public record ApplyEditOperationRequest(Guid DocumentId, string OperationType, int Position, int? Length, string? Text, int BaseVersion);
public record CachedDocumentState(string Content, int VersionNumber);
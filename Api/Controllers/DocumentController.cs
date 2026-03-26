using System.Security.Claims;
using CollabDocs.Domain.Models;
using CollabDocs.Infrastructure.Database;
using CollabDocs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollabDocs.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DocumentController : ControllerBase
{
    
    private readonly DocumentService _documentService;
    private readonly AppDbContext _dbContext;
    private readonly PermissionService _permissionService;

    public DocumentController(DocumentService documentService, AppDbContext dbContext, PermissionService permissionService)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _documentService = documentService;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return Guid.Parse(userIdClaim);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        var userId = GetUserIdFromToken();
        var document = await _documentService.CreateDocumentAsync(request, userId);
        return Ok(document);
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        var userId = GetUserIdFromToken();

        var documents = await _dbContext.Documents
            .Where(d => d.OwnerId == userId || _dbContext.DocumentPermissions.Any(p => p.DocumentId == d.Id && p.UserId == userId))
            .Select(document => new DocumentResponse(
                document.Id,
                document.Title,
                document.Content,
                document.CreatedAt,
                document.OwnerId
            ))
            .ToListAsync();

        return Ok(documents);
    }
    
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var userId = GetUserIdFromToken();

        var canView = await _dbContext.Documents.AnyAsync(d =>
            d.Id == id &&
            (d.OwnerId == userId || _dbContext.DocumentPermissions.Any(p => p.DocumentId == id && p.UserId == userId)));

        if (!canView)
        {
            return NotFound();
        }

        var document = await _dbContext.Documents.FindAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        var response = new DocumentResponse(
            document.Id,
            document.Title,
            document.Content,
            document.CreatedAt,
            document.OwnerId
        );

        return Ok(response);
    }
    
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentRequest request)
    {
        var userId = GetUserIdFromToken();
        var canEdit = await _permissionService.CanEditAsync(id, userId);

        if (!canEdit)
        {
            return Forbid();
        }

        var document = await _dbContext.Documents.FindAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        document.Title = request.Title;
        document.Content = request.Content;
        
        var latestVersionNumber = await _dbContext.DocumentVersions
            .Where(v => v.DocumentId == document.Id)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Content = document.Content,
            VersionNumber = latestVersionNumber + 1,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.DocumentVersions.Add(version);

        await _dbContext.SaveChangesAsync();
        
        var response = new DocumentResponse(
            document.Id,
            document.Title,
            document.Content,
            document.CreatedAt,
            document.OwnerId
        );

        return Ok(response);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var userId = GetUserIdFromToken();
        var canDelete = await _permissionService.CanDeleteAsync(id, userId);

        if (!canDelete)
        {
            return Forbid();
        }

        try
        {
            var result = await _documentService.DeleteDocumentAsync(id);
            return Ok(new { message = result });
        }
        catch (Exception)
        {
            return NotFound();
        }
    }
    
    [Authorize]
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var result = await _documentService.GetHistoryAsync(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            versions = result.Versions
        });
    }

    [Authorize]
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreVersion(Guid id, [FromBody] RestoreVersionRequest request)
    {
        var userId = GetUserIdFromToken();
        var canEdit = await _permissionService.CanEditAsync(id, userId);

        if (!canEdit)
        {
            return Forbid();
        }

        var result = await _documentService.RestoreVersionAsync(id, request.VersionNumber, userId);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            updatedContent = result.UpdatedContent
        });
    }
}

public record RestoreVersionRequest(int VersionNumber);

public record DocumentResponse(
    Guid Id,
    string Title,
    string Content,
    DateTime CreatedAt,
    Guid OwnerId
);
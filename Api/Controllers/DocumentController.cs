using System.Security.Claims;
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
    
    
    /*
    For sharing of document might implement this later if basic role checks works
    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareDocument(Guid id, [FromBody] ShareDocumentRequest request)
    {
        var ownerId = GetUserIdFromToken();

        var canShare = await _permissionService.CanDeleteAsync(id, ownerId);
        if (!canShare)
        {
            return Forbid();
        }

        var permission = new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = id,
            UserId = request.UserId,
            Role = request.Role
        };

        _dbContext.DocumentPermissions.Add(permission);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Document shared successfully." });
    }
    */
}

public record DocumentResponse(
    Guid Id,
    string Title,
    string Content,
    DateTime CreatedAt,
    Guid OwnerId
);
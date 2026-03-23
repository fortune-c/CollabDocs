using CollabDocs.Services;
using Microsoft.AspNetCore.SignalR;

namespace CollabDocs.Api.Hubs;

public class DocumentHub : Hub
{
    private readonly DocumentService _documentService;

    public DocumentHub(DocumentService documentService)
    {
        _documentService = documentService;
    }
    
    public async Task JoinDocument(Guid documentId)
    {
        var groupName = $"document-{documentId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedDocument", documentId);
    }
    
    public async Task LeaveDocument(Guid documentId)
    {
        var groupName = $"document-{documentId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftDocument", documentId);   
    }
    
    public async Task SendEditOperation(SendEditOperationRequest request)
    {
        var userId = Guid.Parse(Context.UserIdentifier!);
        var groupName = $"document-{request.DocumentId}";
        
        var result = await _documentService.ApplyEditOperationAsync(
            new ApplyEditOperationRequest(
                request.DocumentId,
                request.OperationType,
                request.Position,
                request.Length,
                request.Text
            ),
            userId
        );

        if (!result.Success)
        {
            await Clients.Group(groupName).SendAsync("EditRejected", result);
            return;
        }

        var operation = new EditOperationResponse(
            request.DocumentId,
            userId,
            request.OperationType,
            request.Position,
            request.Text,
            DateTime.UtcNow
        );

        await Clients.Group(groupName).SendAsync("ReceiveEditOperation", operation);
    }

    public async Task SendTestMesaage(Guid documentId, string message)
    {
        var groupName = $"document-{documentId}";
        await Clients.Group(groupName).SendAsync("ReceiveTestMessage", message);
    }
}

public record SendEditOperationRequest(Guid DocumentId, string OperationType, int Position, int? Length, string? Text);
public record EditOperationResponse(Guid DocumentId, Guid UserId, string OperationType, int Position, string? Text, DateTime Timestamp);
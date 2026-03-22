using Microsoft.AspNetCore.SignalR;

namespace CollabDocs.Api.Hubs;

public class DocumentHub : Hub
{
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

public record SendEditOperationRequest(Guid DocumentId, string OperationType, int Position, string? Text);
public record EditOperationResponse(Guid DocumentId, Guid UserId, string OperationType, int Position, string? Text, DateTime Timestamp);
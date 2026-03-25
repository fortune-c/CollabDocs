using CollabDocs.Services;
using Microsoft.AspNetCore.SignalR;

namespace CollabDocs.Api.Hubs;

public class DocumentHub : Hub
{
    private readonly DocumentEditQueue _documentEditQueue;

    public DocumentHub(DocumentEditQueue documentEditQueue)
    {
        _documentEditQueue = documentEditQueue;
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
        
        var operation = new QueuedEditOperation(
            new ApplyEditOperationRequest(
                request.DocumentId,
                request.OperationType,
                request.Position,
                request.Length,
                request.Text,
                request.BaseVersion
            ),
            userId,
            Context.ConnectionId
        );

        await _documentEditQueue.EnqueueEditAsync(operation);
    }

    public async Task SendTestMesaage(Guid documentId, string message)
    {
        var groupName = $"document-{documentId}";
        await Clients.Group(groupName).SendAsync("ReceiveTestMessage", message);
    }
}

public record SendEditOperationRequest(Guid DocumentId, string OperationType, int Position, int? Length, string? Text, int BaseVersion);
public record EditOperationResponse(Guid DocumentId, Guid UserId, string OperationType, int Position, string? Text, DateTime Timestamp);
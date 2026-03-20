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
}
using CollabDocs.Api.Hubs;
using CollabDocs.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CollabDocs.Infrastructure.BackgroundServices;

public class EditProcessingService : BackgroundService
{
    private readonly DocumentEditQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<DocumentHub> _hubContext;

    public EditProcessingService(
        DocumentEditQueue queue,
        IServiceProvider serviceProvider,
        IHubContext<DocumentHub> hubContext)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var operation in _queue.DequeueEditsAsync(stoppingToken))
        {
            var groupName = $"document-{operation.Request.DocumentId}";
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var documentService = scope.ServiceProvider.GetRequiredService<DocumentService>();

                var result = await documentService.ApplyEditOperationAsync(operation.Request, operation.UserId);

                if (!result.Success)
                {
                    // Send specific error message rejection directly to the caller
                    await _hubContext.Clients.Client(operation.ConnectionId).SendAsync("EditRejected", result, stoppingToken);
                    continue;
                }

                var successResponse = new EditOperationResponse(
                    operation.Request.DocumentId,
                    operation.UserId,
                    operation.Request.OperationType,
                    operation.Request.Position,
                    operation.Request.Text,
                    DateTime.UtcNow
                );

                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveEditOperation", successResponse, stoppingToken);
            }
            catch (Exception)
            {
                // In production, we log this properly. For now, fallback generic error
                await _hubContext.Clients.Client(operation.ConnectionId).SendAsync("EditRejected", new { Success = false, Message = "Internal server error applying edit." }, stoppingToken);
            }
        }
    }
}

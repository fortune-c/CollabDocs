namespace CollabDocs.Domain.Models;

public class DocumentEdit
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }

    public string OperationType { get; set; } = string.Empty;
    public int Position { get; set; }
    public int? Length { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
}
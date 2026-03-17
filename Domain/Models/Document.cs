using System.ComponentModel.DataAnnotations;

namespace CollabDocs.Domain.Models;

public class Document
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public ICollection<DocumentPermission> Permissions { get; set; } = new List<DocumentPermission>();
}
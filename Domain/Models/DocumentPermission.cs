using System.ComponentModel.DataAnnotations;
using CollabDocs.Domain.Enums;

namespace CollabDocs.Domain.Models;

public class DocumentPermission
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public DocumentRole Role { get; set; }

    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
}
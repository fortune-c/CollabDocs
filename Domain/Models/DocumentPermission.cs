using System.ComponentModel.DataAnnotations;
using CollabDocs.Domain.Enums;

namespace CollabDocs.Domain.Models;

public class DocumentPermission
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid DocumentId { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required]
    public DocumentRole Role { get; set; }

    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
}
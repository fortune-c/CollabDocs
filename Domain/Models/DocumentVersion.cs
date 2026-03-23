using System.ComponentModel.DataAnnotations;

namespace CollabDocs.Domain.Models;

public class DocumentVersion
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid DocumentId { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    public int VersionNumber { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }

    public Document Document { get; set; } = null;
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CollabDocs.Domain.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<Document> OwnedDocuments { get; set; } = new List<Document>();
    public ICollection<DocumentPermission> DocumentPermissions { get; set; } = new List<DocumentPermission>();
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public int? DocumentId { get; set; }
    [Required, MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    [MaxLength(2000)]
    public string? Details { get; set; }
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
    [ForeignKey(nameof(DocumentId))]
    public virtual Document? Document { get; set; }
}
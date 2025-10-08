using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoogleDriveClone.Api.Entities;

public class FileMetadata
{
    [Key]
    public Guid Id { get; set; }

    public string OriginalName { get; set; } = null!;

    public long Size { get; set; }

    public string ContentType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string OwnerId { get; set; } = null!;

    public string FileHash { get; set; } = null!;

    [ForeignKey("OwnerId")]
    public User? Owner { get; set; }
}

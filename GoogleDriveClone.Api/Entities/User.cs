using Microsoft.AspNetCore.Identity;

namespace GoogleDriveClone.Api.Entities;

public class User : IdentityUser
{
    public virtual ICollection<FileMetadata> Files { get; set; } = new List<FileMetadata>();
}
using Microsoft.AspNetCore.Identity;

namespace Identity.API.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
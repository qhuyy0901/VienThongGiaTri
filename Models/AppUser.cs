using Microsoft.AspNetCore.Identity;

namespace AspNetMvcApp.Models;

public class AppUser : IdentityUser
{
    public string? FullName { get; set; }
}

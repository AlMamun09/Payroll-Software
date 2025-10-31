using Microsoft.AspNetCore.Identity;

namespace PayrollSoftware.Infrastructure.Identity
{
 public class PayrollUser : IdentityUser
 {
 [PersonalData]
 public string? FirstName { get; set; }

 [PersonalData]
 public string? LastName { get; set; }
 }
}

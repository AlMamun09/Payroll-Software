using Microsoft.AspNetCore.Identity;
namespace PayrollSoftware.Web.Areas.Identity.Data
{
    public class PayrollUser: IdentityUser
    {
        [PersonalData]
        public string? FirstName { get; set; }
        [PersonalData]
        public string? LastName { get; set; }
    }
}

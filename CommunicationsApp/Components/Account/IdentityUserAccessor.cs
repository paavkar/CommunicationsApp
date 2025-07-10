using CommunicationsApp.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CommunicationsApp.Components.Account
{
    internal sealed class IdentityUserAccessor(
        UserManager<ApplicationUser> userManager,
        IdentityRedirectManager redirectManager)
    {
        public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null && context is not null)
            {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            }

            return user!;
        }

        public async Task<ApplicationUser> GetRequiredUserFromPrincipalAsync(ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            //if (user is null)
            //{
            //    redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(principal)}'.");
            //}
            return user!;
        }
    }
}

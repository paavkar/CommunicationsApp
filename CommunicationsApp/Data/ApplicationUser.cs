using CommunicationsApp.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationsApp.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public List<Server> Servers { get; set; } = [];
        public List<ServerProfile> ServerProfiles { get; set; } = [];
        [NotMapped]
        public AccountSettings AccountSettings { get; set; }
    }
}

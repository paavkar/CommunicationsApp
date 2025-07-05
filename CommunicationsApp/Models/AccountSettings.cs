using Microsoft.FluentUI.AspNetCore.Components;

namespace CommunicationsApp.Models
{
    public class AccountSettings
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PreferredLocale { get; set; }
        public bool DisplayServerMemberList { get; set; }
        public DesignThemeModes PreferredTheme { get; set; }
    }
}

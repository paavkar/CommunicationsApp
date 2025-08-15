using System;

namespace CommunicationsApp.Infrastructure.Auth
{
    public class RefreshTokenInfo
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public string Platform { get; set; } = string.Empty;
    }
}
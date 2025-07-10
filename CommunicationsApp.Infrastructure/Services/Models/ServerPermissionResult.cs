using CommunicationsApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationsApp.Infrastructure.Services
{
    public class ServerPermissionResult
    {
        public bool? Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

using CommunicationsApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationsApp.Infrastructure.Services
{
    public class ServerResult
    {
        public bool? Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
        public Server? Server { get; set; }
    }
}

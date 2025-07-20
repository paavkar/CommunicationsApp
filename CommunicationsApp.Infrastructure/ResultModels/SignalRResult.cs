using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationsApp.Infrastructure.Services.Models
{
    public class SignalRResult
    {
        public bool? Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

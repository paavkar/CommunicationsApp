namespace CommunicationsApp.Infrastructure.Services.ResultModels
{
    public class ResultBaseModel
    {
        public bool? Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

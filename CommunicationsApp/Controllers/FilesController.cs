using Asp.Versioning;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CommunicationsApp.Core.Models.Enums;

namespace CommunicationsApp.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FilesController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly IWebHostEnvironment _env;

        public FilesController(IMediaService mediaService, IWebHostEnvironment env)
        {
            _mediaService = mediaService;
            _env = env;
        }

        [HttpPost("upload/{messageId}")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> UploadFiles(string messageId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var tempDir = Path.Combine(_env.WebRootPath, "temp", messageId);
            Directory.CreateDirectory(tempDir);

            var fileDict = new Dictionary<string, MediaAttachment>();
            foreach (var file in files)
            {
                var filePath = Path.Combine(tempDir, file.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                fileDict[file.FileName] = new MediaAttachment
                {
                    FileName = file.FileName,
                    FileSize = file.Length,
                    Url = filePath
                };
            }

            var fileUpload = new FileUploadList
            {
                Origin = FileUploadOrigin.API,
                Files = fileDict
            };

            var result = await _mediaService.UploadPostMediaAsync(fileUpload, messageId);
            Directory.Delete(tempDir, true);
            return result.Succeeded ? Ok(result.Files) : BadRequest(result.ErrorMessage);
        }
    }
}

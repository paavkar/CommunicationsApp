using Asp.Versioning;
using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Core.Media;
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

        [HttpPost("user-profile-picture/{userId}")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> UploadUserProfilePicture(string userId, IFormFile file)
        {
            if (file == null)
                return BadRequest("No file uploaded.");

            var tempDir = Path.Combine(_env.WebRootPath, "temp", userId);
            Directory.CreateDirectory(tempDir);

            var filePath = Path.Combine(tempDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUpload = new FileUploadSingle
            {
                Origin = FileUploadOrigin.API,
                FilePath = filePath
            };

            var result = await _mediaService.UploadUserProfilePictureAsync(userId, fileUpload);
            Directory.Delete(tempDir, true);
            return result.Succeeded ? Ok(result.File) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("server-profile-picture/{serverProfileId}")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> UploadServerProfilePicture(string serverProfileId, IFormFile file)
        {
            if (file == null)
                return BadRequest("No file uploaded.");

            var tempDir = Path.Combine(_env.WebRootPath, "temp", serverProfileId);
            Directory.CreateDirectory(tempDir);

            var filePath = Path.Combine(tempDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUpload = new FileUploadSingle
            {
                Origin = FileUploadOrigin.API,
                FilePath = filePath
            };

            var result = await _mediaService.UploadServerProfilePictureAsync(serverProfileId, fileUpload);
            Directory.Delete(tempDir, true);
            return result.Succeeded ? Ok(result.File) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("server-banner/{serverId}")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> UploadServerBanner(string serverId, IFormFile file)
        {
            if (file == null)
                return BadRequest("No file uploaded.");

            var tempDir = Path.Combine(_env.WebRootPath, "temp", serverId);
            Directory.CreateDirectory(tempDir);

            var filePath = Path.Combine(tempDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUpload = new FileUploadSingle
            {
                Origin = FileUploadOrigin.API,
                FilePath = filePath
            };

            var result = await _mediaService.UploadServerBannerAsync(serverId, fileUpload);
            Directory.Delete(tempDir, true);
            return result.Succeeded ? Ok(result.File) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("server-icon/{serverId}")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> UploadServerIcon(string serverId, IFormFile file)
        {
            if (file == null)
                return BadRequest("No file uploaded.");

            var tempDir = Path.Combine(_env.WebRootPath, "temp", serverId);
            Directory.CreateDirectory(tempDir);

            var filePath = Path.Combine(tempDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUpload = new FileUploadSingle
            {
                Origin = FileUploadOrigin.API,
                FilePath = filePath
            };

            var result = await _mediaService.UploadServerIconAsync(serverId, fileUpload);
            Directory.Delete(tempDir, true);
            return result.Succeeded ? Ok(result.File) : BadRequest(result.ErrorMessage);
        }
    }
}

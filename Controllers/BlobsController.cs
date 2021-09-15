using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MimeTypes;
using System.IO;

namespace BlobStorage.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlobsController : ControllerBase
    {
        private readonly StorageOptions storageOptions;
        private readonly ILogger<BlobsController> _logger;

        public BlobsController(ILogger<BlobsController> logger, StorageOptions storageOptions)
        {
            this.storageOptions = storageOptions;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm(Name = "blob")] IFormFile blob, [FromHeader] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("Token is missing");

            if (!storageOptions.TryGetDirPath(token, out var dirPath))
                throw new Exception("Token is wrong");

            if (blob == null)
                throw new Exception("Blob is missing");

            string extension = "";

            if (!string.IsNullOrWhiteSpace(blob.FileName))
                extension = Path.GetExtension(blob.FileName);
            else if (!string.IsNullOrWhiteSpace(blob.ContentType))
                extension = MimeTypeMap.GetExtension(blob.ContentType, false);

            var fileName = $"{Guid.NewGuid():N}{extension}".ToLower();

            using (var reader = blob.OpenReadStream())
            using (var writer = System.IO.File.OpenWrite(Path.Combine(dirPath, fileName)))
                await reader.CopyToAsync(writer);


            return Ok(new { path = $"https://{storageOptions.Host}/blobs/{fileName}" });
        }

        [HttpGet("{fileName}")]
        public IActionResult Get(string fileName)
        {
            fileName = fileName?.ToLower() ?? "";

            if (!storageOptions.TryFindFile(fileName, out var path))
                throw new Exception("File not found");

            string mimeType = "";
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(extension))
                if (MimeTypeMap.TryGetMimeType(extension, out var _mimeType))
                    mimeType = _mimeType;

            return PhysicalFile(path, mimeType, fileName, true);
        }
    }
}

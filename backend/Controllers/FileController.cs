using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using backend.Data;

namespace backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded");

            try
            {
                AzureBlob blob = new AzureBlob();

                string uniqueBlobName = $"{Guid.NewGuid()}-{file.FileName}";

                await blob.UploadBlobAsync("knowledgebank", uniqueBlobName, file.OpenReadStream());

                return Ok(new { fileName = uniqueBlobName });
            }catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("exists")]
        public async Task<IResult> Exists(string name)
        {
            AzureBlob blob = new AzureBlob();

            return Results.Json(await blob.BlobExistsAsync("knowledgebank", name));
        }
    }

}

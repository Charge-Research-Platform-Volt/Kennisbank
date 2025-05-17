using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Responses;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class WebsiteUploadController(ResourceManager resourceManager) : ControllerBase
    {
        private readonly ResourceManager resourceManager = resourceManager;
        
        /// <summary>
        /// Checks if the URL is valid
        /// </summary>
        /// <param name="URL">URL of the website</param>
        /// <returns>whether or not the URL is valid</returns>
        private bool ValidURL(string URL)
        {
            // a.io is just about the shortest url there is
            // every URL needs at least 1 dot to be valid
            return URL.Length > 3 && URL.Contains('.');
        }

        [HttpGet("all-websites")]
        [SwaggerOperation(
            Summary = "lists all websites",
            Description = "HttpGet request that fetches all websites uploaded to the archive"
        )]
        [SwaggerResponse(200, "List of websites")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> Get()
        {

            try
            {
                return Ok(await resourceManager.GetAllResourcesAsync(predicate: r => r.FileType == "website", includeProperties: "WebsiteMetadata"));
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to retrieve uploaded websites.");
                return StatusCode(500, new {message = "Internal server error"});
            }
        }

        [HttpGet("get-website/{id}")]
        [SwaggerOperation(
            Summary = "gets website by id",
            Description = "HttpGet request that fetches the website url that corresponds with the id"
        )]
        [SwaggerResponse(200, "URL of website")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> Get_Website(string id)
        {
            try
            {
                return Ok(await resourceManager.GetWebsiteMetadataPropertyAsync(id, "new(Url as Url)"));
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to retrieve uploaded websites.");
                return StatusCode(500, new {message = "Internal server error"});
            }
        }

        /// <summary>
        /// Uploads a new website to the database.
        /// </summary>
        /// <param name="dto">The dto used for adding the website to the db.</param>
        /// <returns>
        /// Returns an OK response.
        /// </returns>
        [HttpPut("add-website")]
        [SwaggerOperation(
                Summary = "Uploads a new website.",
                Description = "Lets a user upload a new website to the archive."
            )]
        [SwaggerResponse(200, "New website uploaded")]
        [SwaggerResponse(400, "Bad request")]
        [SwaggerResponse(409, "Website already exists in database")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> AddWebsite([FromForm] WebsiteCreateDto dto)
        {
            Log.Information("Adding new website to database.");

            if(string.IsNullOrEmpty(dto.Url) || !ValidURL(dto.Url) )
            {
                return BadRequest("URL is not provided or invalid.");
            }
            if(string.IsNullOrEmpty(dto.Title))
            {
                return BadRequest("Title is not provided");
            }


            try
            {
                Log.Information("Adding website '{URL}' to database", dto.Url);
                Log.Information(dto.Url);
                Guid id = await resourceManager.CreateWebsiteAsync(dto);
                // Uploading website successful
                Log.Information("Website added to the database.");
                return Ok(new { message = "Website uploaded.", id });
            }
            catch(Exception e)
            {
                // Something else went wrong
                Log.Error(e, "Failed to add website.");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Deletes a website.
        /// </summary>
        /// <param name="id">The id of the website to delete.</param>
        /// <returns>
        /// Returns a 200 OK response.
        /// </returns>
        [HttpDelete("delete-website/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(
                Summary = "Delete website.",
                Description = "Lets an admin delete a website."
            )]
        [SwaggerResponse(200, "Website deleted")]
        [SwaggerResponse(400, "Bad request")]
        [SwaggerResponse(404, "Website not found")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> DeleteWebsite(string id)
        {
            Log.Information("Removing website from database");

            if(string.IsNullOrEmpty(id))
            {
                Log.Error("id is required");
                return BadRequest(new {message = "id is required"});
            }

            try
            {
                // Find and remove website
                if (!await resourceManager.DeleteResourceAsync(id))
                    return NotFound(new StorageResponse("ID was not found in database. Website was deleted succesfully."));

                return Ok(new { message = "website deleted" });
            }
            catch (Exception e)
            {
                Log.Information(e, "Error while detecting website with ID {id}", id);
                return StatusCode(500, new StorageResponse("Error while removing website. "));
            }
        }


        /// <summary>
        /// Changes website entry in the database based on what you want to change
        /// </summary>
        /// <param name="id">id of the website</param>
        /// <param name="newTitleOrURL">the new title or url to change to</param>
        /// <param name="column">the attribute to change</param>
        /// <returns>Ok if it succeeds</returns>
        [HttpPatch("change-website/{id}/{newTitleOrUrl}/{column}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(
                Summary = "Change website.",
                Description = "Lets an admin change a website."
            )]
        [SwaggerResponse(200, "Website changed")]
        [SwaggerResponse(400, "Bad request")]
        [SwaggerResponse(404, "Website not found")]
        [SwaggerResponse(409, "New URL already exists")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> ChangeWebsite(string id, string newTitleOrURL, WebsiteColumn column)
        {
            Log.Information("Changing website title or URL");

            if (string.IsNullOrEmpty(id))
            {
                Log.Error("Id is required");
                return BadRequest(new { message = "Id is required" });
            }

            if (string.IsNullOrEmpty(newTitleOrURL) || (column == WebsiteColumn.Url && !ValidURL(newTitleOrURL)))
            {
                Log.Error("invalid title or url");
                return BadRequest(new { message = "invalid title or url" });
            }

            // Parse GUID
            Guid guid;
            try
            {
                guid = Guid.Parse(id);
            }
            catch (FormatException)
            {
                Log.Error("Invalid id format.");
                return BadRequest(new { message = "Invalid id format." });
            }

            //Change website url or title
            try
            {
                if (column == WebsiteColumn.Url)
                {
                    if (!await resourceManager.UpdateWebsiteMetadataAsync(id, metadata => metadata.Url, newTitleOrURL))
                        return NotFound(new StorageResponse("ID was not found in database."));
                }
                else
                {
                    if (!await resourceManager.UpdateResourceAsync(id, r => r.Title, newTitleOrURL))
                        return NotFound(new StorageResponse("ID was not found in database."));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while renaming file with ID {Id}.", id);
                return StatusCode(500, new StorageResponse("Error while renaming file."));
            }
            return Ok(new { message = "Website URL or Title changed"});
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)



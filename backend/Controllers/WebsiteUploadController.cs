using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Docker.DotNet.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class WebsiteUploadController : ControllerBase
    {
        // Database context
        private readonly DatabaseContext _context;
        public WebsiteUploadController(DatabaseContext context)
        {
            _context = context;
        }
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
        [SwaggerResponse(200, "List of websites", typeof(List<Website>))]
        [SwaggerResponse(500, "Internal server error")]
        public IActionResult Get()
        {
            try
            {
                return Ok(_context.Websites.OrderBy(web => web.Name).ToList());
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
        [SwaggerResponse(200, "New website uploaded", typeof(Website))]
        [SwaggerResponse(400, "Bad request")]
        [SwaggerResponse(409, "Website already exists in database")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> AddWebsite([FromForm] WebUploadDto dto)
        {
            Log.Information("Adding new website to database.");

            if(string.IsNullOrEmpty(dto.URL) || !ValidURL(dto.URL) )
            {
                return BadRequest("URL is not provided or invalid.");
            }
            if(string.IsNullOrEmpty(dto.Name))
            {
                return BadRequest("Title is not provided");
            }
            if(string.IsNullOrEmpty(dto.Description))
            {
                return BadRequest("Description is not provided");
            }

            Guid id = Guid.NewGuid();
            //Log(id);

            try
            {
                Website website = new()
                {
                    Id = id,
                    URL = dto.URL,
                    Name = dto.Name,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.Websites.AddAsync(website);

                foreach (string tag in dto.Tags)
                {
                    FileTagLink tagEntry = new()
                    {
                        DocId = id,
                        TagId = Guid.Parse(tag),
                    };

                    await _context.FileTagLinks.AddAsync(tagEntry);
                }
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
            {
                // The website URL already is in the database
                Log.Error(e, "Website URL already exists in database.");
                return Conflict(new { message = "Website URL already exists." });
            }
            catch(Exception e)
            {
                // Something else went wrong
                Log.Error(e, "Failed to add website.");
                return StatusCode(500, new { message = "Internal server error" });
            }

            // Uploading website successful
            Log.Information("Website added to the database.");
            return Ok(new { message = "Website uploaded." });
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
        [SwaggerResponse(200, "Website deleted", typeof(Website))]
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
            Guid guid = Guid.Parse(id);
            Log.Error(id);

            // Find website
            Website? website = await _context.Websites.FirstOrDefaultAsync(web => web.Id == guid);

            if(website == null)
            {
                Log.Error("Website could not be found");
                return BadRequest(new {message = "website could not be found"});
            }
            // Remove website from database
            _context.Websites.Remove(website);
            await _context.SaveChangesAsync();

            return Ok(new { message = "website deleted"});
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
        [SwaggerResponse(200, "Website changed", typeof(Website))]
        [SwaggerResponse(400, "Bad request")]
        [SwaggerResponse(404, "Website not found")]
        [SwaggerResponse(409, "New URL already exists")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> ChangeWebsite(string id, string newTitleOrURL, string column)
        {
            Log.Information("Changing website title or URL");

            // column has to be either title or URL
            if(column != "Title" && column != "URL")
            {
                Log.Information("no such attribute for website");
                return BadRequest(new {message = "no such attribute for website"});
            }

            if (string.IsNullOrEmpty(id))
            {
                Log.Error("Id is required");
                return BadRequest(new { message = "Id is required" });
            }

            if (string.IsNullOrEmpty(newTitleOrURL) || (column == "URL" && !ValidURL(newTitleOrURL)))
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

            Website? website = await _context.Websites.FirstOrDefaultAsync(web => web.Id == guid);

            if(website == null)
            {
                Log.Error("no such website found");
                return NotFound(new {message = "so such website found"});
            }
            //Change website url or title
            try
            {
                if(column == "URL")
                    website.URL = newTitleOrURL;
                else
                    website.Name = newTitleOrURL;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
            {
                // The website url already exists
                Log.Error(e, "URL already exists.");
                return Conflict(new { message = "New URL already exists." });
            }
            return Ok(new { message = "Website URL or Title changed"});
        }
    }
}

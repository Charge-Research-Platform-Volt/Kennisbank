using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Data;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models.User;
using Requests = KnowledgeBank.Models.User.Requests;
using System.Data.SqlTypes;
using backend.Responses;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly DatabaseContext databaseContext;
    public UserController(DatabaseContext databaseContext)
    {
        this.databaseContext = databaseContext;
    }

    [HttpPost("add")]
    [SwaggerOperation(
        Summary = "Create a new user in the database.",
        Description = "Creates a new user in the database."
    )]
    public async Task<IActionResult> Add([FromBody] Requests.Add request)
    {
        // Check if our request body was valid
        if (!ModelState.IsValid) {
            return BadRequest(new { ModelState });
        }  

        User user = new()
        {
            Id = Guid.NewGuid(), 
            Email = request.Email,
            Name = request.Name,
            HashedPassword = request.Password
        };

        // Add the tag
        try
        {
            await databaseContext.Users.AddAsync(user);
            await databaseContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // Something else went wrong
            return StatusCode(500, new { message = e.Message, innerMessage = e.InnerException?.Message, innerType = e.InnerException?.ToString(), trace = e.StackTrace });
        }

        return Ok(new { message = "User added." });
    }
}
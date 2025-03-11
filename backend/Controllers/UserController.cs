using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Data;
using Swashbuckle.AspNetCore.Annotations;
using KnowledgeBank.Models;
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
    public async Task<IActionResult> Add([FromBody] AddUser request)
    {
        // Check if our request body was valid
        if (!ModelState.IsValid)
        {
            return BadRequest(new { ModelState });
        }

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
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
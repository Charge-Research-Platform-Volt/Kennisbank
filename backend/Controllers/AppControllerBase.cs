using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBank.Controllers;

[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    protected IActionResult OkOrNotFound<T>(T? value)
        => value is null ? NotFound() : Ok(value);
}
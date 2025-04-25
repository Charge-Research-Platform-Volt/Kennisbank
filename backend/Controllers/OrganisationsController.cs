// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Reflection;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// This controller is responsible for handing API calls to manage organisations and their metadata.
    /// 
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class OrganisationsController(ResourceManager resourceManager) : Controller
    {
        private readonly Serilog.ILogger logger = Log.ForContext<PersonsController>();
        

    }
}

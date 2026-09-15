using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SystemDesign.CloudStorage.Api.Controllers;

/// <summary>
/// Base class for authenticated controllers.
/// </summary>
[Authorize]
[ApiController]
public abstract class OwnedController : ControllerBase
{
    /// <summary>
    /// The current user identifier.
    /// </summary>
    protected Guid OwnerId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

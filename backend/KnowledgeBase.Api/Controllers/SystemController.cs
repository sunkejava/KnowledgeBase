using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN")]
[Route("api/system")]
public sealed class SystemController(KnowledgeDbContext dbContext) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
        => Ok(await dbContext.Users.AsNoTracking().OrderBy(x => x.UserName)
            .Select(x => new { x.Id, x.UserName, x.DisplayName, x.Enabled, x.CreatedAt })
            .ToListAsync(cancellationToken));

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken)
        => Ok(await dbContext.Roles.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, x.Enabled })
            .ToListAsync(cancellationToken));

    [HttpGet("departments")]
    public async Task<IActionResult> Departments(CancellationToken cancellationToken)
        => Ok(await dbContext.Departments.AsNoTracking().OrderBy(x => x.Sort)
            .Select(x => new { x.Id, x.ParentId, x.Name, x.Sort })
            .ToListAsync(cancellationToken));

    [HttpGet("organizations")]
    public async Task<IActionResult> Organizations(CancellationToken cancellationToken)
        => Ok(await dbContext.Organizations.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new { x.Id, x.ParentId, x.Name, x.Code })
            .ToListAsync(cancellationToken));

    [HttpGet("menus")]
    public async Task<IActionResult> Menus(CancellationToken cancellationToken)
        => Ok(await dbContext.Menus.AsNoTracking().OrderBy(x => x.Sort)
            .Select(x => new { x.Id, x.ParentId, x.Name, x.Path, x.Permission, x.Sort })
            .ToListAsync(cancellationToken));
}

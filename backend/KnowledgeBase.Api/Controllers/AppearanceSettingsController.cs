using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Route("api/settings/appearance")]
public sealed class AppearanceSettingsController(IAppearanceSettingsService service) : ControllerBase
{
    [HttpGet]
    public Task<AppearanceSettingsDto> Get([FromQuery] string userKey = "local", CancellationToken cancellationToken = default)
        => service.GetAsync(userKey, cancellationToken);

    [HttpPut]
    public Task<AppearanceSettingsDto> Save([FromBody] SaveAppearanceSettingsRequest request, [FromQuery] string userKey = "local", CancellationToken cancellationToken = default)
        => service.SaveAsync(userKey, request, cancellationToken);
}

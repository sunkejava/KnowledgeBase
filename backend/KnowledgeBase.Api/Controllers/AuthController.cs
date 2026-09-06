using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null ? Unauthorized(new { message = "用户名或密码错误" }) : Ok(result);
    }
}

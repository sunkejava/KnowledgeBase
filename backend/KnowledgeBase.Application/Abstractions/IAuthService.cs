using KnowledgeBase.Contracts.Auth;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>身份认证服务。</summary>
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task EnsureDefaultAdminAsync(CancellationToken cancellationToken);
}

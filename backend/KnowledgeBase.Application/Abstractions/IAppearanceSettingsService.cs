using KnowledgeBase.Contracts.Settings;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>用户界面偏好设置服务。</summary>
public interface IAppearanceSettingsService
{
    Task<AppearanceSettingsDto> GetAsync(string userKey, CancellationToken cancellationToken);
    Task<AppearanceSettingsDto> SaveAsync(string userKey, SaveAppearanceSettingsRequest request, CancellationToken cancellationToken);
}

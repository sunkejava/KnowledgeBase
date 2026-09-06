using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Settings;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>基于 EF Core 的界面设置持久化实现。</summary>
public sealed class AppearanceSettingsService(KnowledgeDbContext dbContext) : IAppearanceSettingsService
{
    public async Task<AppearanceSettingsDto> GetAsync(string userKey, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserAppearanceSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserKey == userKey, cancellationToken);
        return entity is null ? new("dark", "zh-CN", 14, false, "KnowledgeBase", false) : Map(entity);
    }

    public async Task<AppearanceSettingsDto> SaveAsync(string userKey, SaveAppearanceSettingsRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserAppearanceSettings.FirstOrDefaultAsync(x => x.UserKey == userKey, cancellationToken);
        if (entity is null)
        {
            entity = new UserAppearanceSetting(userKey);
            dbContext.UserAppearanceSettings.Add(entity);
        }
        entity.Update(request.Theme, request.Locale, request.FontSize, request.WatermarkEnabled, request.WatermarkText, request.CompactMode);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static AppearanceSettingsDto Map(UserAppearanceSetting x) => new(x.Theme, x.Locale, x.FontSize, x.WatermarkEnabled, x.WatermarkText, x.CompactMode);
}

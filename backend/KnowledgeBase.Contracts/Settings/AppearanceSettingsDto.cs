namespace KnowledgeBase.Contracts.Settings;

/// <summary>用户界面偏好设置。</summary>
public sealed record AppearanceSettingsDto(
    string Theme,
    string Locale,
    int FontSize,
    bool WatermarkEnabled,
    string WatermarkText,
    bool CompactMode);

public sealed record SaveAppearanceSettingsRequest(
    string Theme,
    string Locale,
    int FontSize,
    bool WatermarkEnabled,
    string WatermarkText,
    bool CompactMode);

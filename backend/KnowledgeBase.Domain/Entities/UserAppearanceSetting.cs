namespace KnowledgeBase.Domain.Entities;

/// <summary>账号级界面偏好。未启用登录模块前使用 userKey 作为临时身份键。</summary>
public sealed class UserAppearanceSetting
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserKey { get; private set; } = string.Empty;
    public string Theme { get; private set; } = "dark";
    public string Locale { get; private set; } = "zh-CN";
    public int FontSize { get; private set; } = 14;
    public bool WatermarkEnabled { get; private set; }
    public string WatermarkText { get; private set; } = "KnowledgeBase";
    public bool CompactMode { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private UserAppearanceSetting() { }

    public UserAppearanceSetting(string userKey) => UserKey = userKey.Trim();

    public void Update(string theme, string locale, int fontSize, bool watermarkEnabled, string watermarkText, bool compactMode)
    {
        Theme = theme is "light" or "dark" or "system" ? theme : "dark";
        Locale = locale is "zh-CN" or "en-US" ? locale : "zh-CN";
        FontSize = Math.Clamp(fontSize, 12, 18);
        WatermarkEnabled = watermarkEnabled;
        WatermarkText = string.IsNullOrWhiteSpace(watermarkText) ? "KnowledgeBase" : watermarkText.Trim()[..Math.Min(watermarkText.Trim().Length, 40)];
        CompactMode = compactMode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

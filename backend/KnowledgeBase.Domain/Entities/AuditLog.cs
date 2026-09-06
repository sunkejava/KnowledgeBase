namespace KnowledgeBase.Domain.Entities;

/// <summary>统一审计日志，覆盖登录、权限和知识资产关键操作。</summary>
public sealed class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Category { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string Target { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string? Message { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private AuditLog() { }
    public AuditLog(string category,string action,string userName,string target,string ipAddress,bool success,string? message=null)
    { Category=category;Action=action;UserName=userName;Target=target;IpAddress=ipAddress;Success=success;Message=message; }
}

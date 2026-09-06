namespace KnowledgeBase.Domain.Entities;

/// <summary>知识库聚合根，用于承载空间、目录与文档。</summary>
public sealed class KnowledgeBaseSpace
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private KnowledgeBaseSpace() { }

    public KnowledgeBaseSpace(string name, string description)
    {
        SetValues(name, description);
    }

    public void Update(string name, string description)
    {
        SetValues(name, description);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetValues(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("知识库名称不能为空", nameof(name));
        Name = name.Trim()[..Math.Min(name.Trim().Length, 120)];
        Description = (description ?? string.Empty).Trim()[..Math.Min((description ?? string.Empty).Trim().Length, 500)];
    }
}

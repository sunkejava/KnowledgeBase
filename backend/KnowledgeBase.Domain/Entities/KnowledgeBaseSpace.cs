namespace KnowledgeBase.Domain.Entities;

/// <summary>
/// 知识库，用于承载一组相互关联的空间、目录与文档。
/// </summary>
public sealed class KnowledgeBaseSpace
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private KnowledgeBaseSpace() { }

    public KnowledgeBaseSpace(string name, string description)
    {
        Name = name.Trim();
        Description = description.Trim();
    }
}

namespace KnowledgeBase.Contracts.KnowledgeBases;

public sealed record KnowledgeBaseDto(Guid Id, string Name, string Description, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record SaveKnowledgeBaseRequest(string Name, string Description);

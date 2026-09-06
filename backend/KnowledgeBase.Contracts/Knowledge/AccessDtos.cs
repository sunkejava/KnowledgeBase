namespace KnowledgeBase.Contracts.Knowledge;

/// <summary>知识库成员权限信息。</summary>
public sealed record KnowledgeBaseMemberDto(Guid KnowledgeBaseId, Guid UserId, string UserName, string DisplayName, string Role, DateTimeOffset CreatedAt);

/// <summary>新增或更新知识库成员请求。</summary>
public sealed record SetKnowledgeBaseMemberRequest(Guid UserId, string Role);

/// <summary>文档用户级权限信息。</summary>
public sealed record DocumentPermissionDto(Guid DocumentId, Guid UserId, string UserName, string DisplayName, bool CanView, bool CanEdit, bool CanManage);

/// <summary>新增或更新文档用户级权限请求。</summary>
public sealed record SetDocumentPermissionRequest(Guid UserId, bool CanView, bool CanEdit, bool CanManage);

/// <summary>权限面板使用的最小用户候选信息。</summary>
public sealed record PermissionUserLookupDto(Guid Id, string UserName, string DisplayName);

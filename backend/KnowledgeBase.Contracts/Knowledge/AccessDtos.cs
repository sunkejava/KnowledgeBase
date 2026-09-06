namespace KnowledgeBase.Contracts.Knowledge;
public sealed record KnowledgeBaseMemberDto(Guid KnowledgeBaseId,Guid UserId,string UserName,string DisplayName,string Role,DateTimeOffset CreatedAt);
public sealed record SetKnowledgeBaseMemberRequest(Guid UserId,string Role);
public sealed record DocumentPermissionDto(Guid DocumentId,Guid UserId,string UserName,string DisplayName,bool CanView,bool CanEdit,bool CanManage);
public sealed record SetDocumentPermissionRequest(Guid UserId,bool CanView,bool CanEdit,bool CanManage);

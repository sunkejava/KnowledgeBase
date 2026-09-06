namespace KnowledgeBase.Domain.Entities;

public sealed class KnowledgeBaseMember
{
    public Guid KnowledgeBaseId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = "Viewer";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private KnowledgeBaseMember() { }
    public KnowledgeBaseMember(Guid knowledgeBaseId,Guid userId,string role){KnowledgeBaseId=knowledgeBaseId;UserId=userId;SetRole(role);}
    public void SetRole(string role)=>Role=role is "Manager" or "Editor" or "Viewer"?role:"Viewer";
}

public sealed class DocumentUserPermission
{
    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
    public bool CanView { get; private set; } = true;
    public bool CanEdit { get; private set; }
    public bool CanManage { get; private set; }
    private DocumentUserPermission() { }
    public DocumentUserPermission(Guid documentId,Guid userId,bool canView,bool canEdit,bool canManage){DocumentId=documentId;UserId=userId;Update(canView,canEdit,canManage);}
    public void Update(bool canView,bool canEdit,bool canManage){CanManage=canManage;CanEdit=canManage||canEdit;CanView=CanEdit||canView;}
}

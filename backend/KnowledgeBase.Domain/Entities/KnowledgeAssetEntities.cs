namespace KnowledgeBase.Domain.Entities;

public sealed class DocumentTag
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#64748b";
    public DocumentTag(string name,string color){Name=name.Trim();Color=string.IsNullOrWhiteSpace(color)?"#64748b":color.Trim();}
    private DocumentTag(){}
    public void Update(string name,string color){Name=name.Trim();Color=string.IsNullOrWhiteSpace(color)?"#64748b":color.Trim();}
}

public sealed class DocumentTagLink
{
    public Guid DocumentId { get; private set; }
    public Guid TagId { get; private set; }
    private DocumentTagLink(){}
    public DocumentTagLink(Guid documentId,Guid tagId){DocumentId=documentId;TagId=tagId;}
}

public sealed class DocumentFavorite
{
    public Guid UserId { get; private set; }
    public Guid DocumentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private DocumentFavorite(){}
    public DocumentFavorite(Guid userId,Guid documentId){UserId=userId;DocumentId=documentId;}
}

public sealed class DocumentRecentView
{
    public Guid UserId { get; private set; }
    public Guid DocumentId { get; private set; }
    public DateTimeOffset LastViewedAt { get; private set; } = DateTimeOffset.UtcNow;
    public int ViewCount { get; private set; } = 1;
    private DocumentRecentView(){}
    public DocumentRecentView(Guid userId,Guid documentId){UserId=userId;DocumentId=documentId;}
    public void Touch(){LastViewedAt=DateTimeOffset.UtcNow;ViewCount++;}
}

public sealed class DocumentVersion
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Markdown { get; private set; } = string.Empty;
    public Guid? EditorId { get; private set; }
    public string ChangeNote { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private DocumentVersion(){}
    public DocumentVersion(Guid documentId,int versionNumber,string title,string slug,string markdown,Guid? editorId,string changeNote){DocumentId=documentId;VersionNumber=versionNumber;Title=title;Slug=slug;Markdown=markdown;EditorId=editorId;ChangeNote=changeNote?.Trim()??string.Empty;}
}

public sealed class DocumentAttachment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DocumentId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoredName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string RelativePath { get; private set; } = string.Empty;
    public Guid? UploaderId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private DocumentAttachment(){}
    public DocumentAttachment(Guid documentId,string fileName,string storedName,string contentType,long size,string relativePath,Guid? uploaderId){DocumentId=documentId;FileName=fileName;StoredName=storedName;ContentType=contentType;Size=size;RelativePath=relativePath;UploaderId=uploaderId;}
}

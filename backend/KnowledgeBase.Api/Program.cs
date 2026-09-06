using System.Text;
using KnowledgeBase.Api.Middleware;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<KnowledgeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=knowledgebase.db"));

builder.Services.AddScoped<IAppearanceSettingsService, AppearanceSettingsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ISystemManagementService, SystemManagementService>();
builder.Services.AddScoped<IKnowledgeAssetService, KnowledgeAssetService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IContentExchangeService, ContentExchangeService>();
builder.Services.AddScoped<IShareService, ShareService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key 未配置");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
    // v0.6 起正式由 EF Core Migration 管理数据库升级。基线迁移兼容历史 EnsureCreated 数据库。
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<IAuthService>().EnsureDefaultAdminAsync(CancellationToken.None);
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();
app.MapOpenApi();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "KnowledgeBase.Api",
    time = DateTimeOffset.UtcNow
}));
app.Run();

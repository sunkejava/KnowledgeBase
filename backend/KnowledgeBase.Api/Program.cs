using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<KnowledgeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=knowledgebase.db"));
builder.Services.AddScoped<IAppearanceSettingsService, AppearanceSettingsService>();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

var app = builder.Build();

// 开发阶段自动创建数据库结构；进入正式迁移管理后切换为 Database.MigrateAsync。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors("frontend");
app.MapOpenApi();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "KnowledgeBase.Api",
    time = DateTimeOffset.UtcNow
}));

app.Run();

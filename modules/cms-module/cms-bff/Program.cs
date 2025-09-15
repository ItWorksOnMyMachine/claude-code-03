using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Services;
using FastEndpoints;
using FastEndpoints.Swagger;
using PlatformShared.Extensions;
using PlatformShared.Services;
using PlatformShared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<CmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add shared platform services (Redis, Data Protection, Session, Tenant Context)
builder.Services.AddPlatformSharedServices(builder.Configuration);

// Add shared platform authentication
builder.Services.AddPlatformAuthentication(builder.Configuration);

// Add CMS services
builder.Services.AddScoped<CmsContentService>();
builder.Services.AddScoped<CmsAssetService>();
builder.Services.AddScoped<CmsTemplateService>();

// Note: ITenantContext is now provided by PlatformSharedServices

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("https://host-fe.platform.local:3002", "https://cms.platform.local:3003")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.UseSwaggerGen();
}

app.UseHttpsRedirection();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api/cms";
});

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    context.Database.EnsureCreated();
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }
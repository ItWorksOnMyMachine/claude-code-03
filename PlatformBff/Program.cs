using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Services;
using PlatformBff.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext with PostgreSQL and tenant context
builder.Services.AddDbContext<PlatformDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add Tenant Context
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Add Repositories
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantUserRepository, TenantUserRepository>();

// Add Redis for distributed caching
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
        ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "PlatformBff";
    });
}
else
{
    // Fallback to in-memory cache if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "platform.session";
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3002") // platform-host
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentPolicy");
    
    // Seed database in development
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        DbContextExtensions.HostingEnvironment = app.Environment;
        await DatabaseSeeder.SeedAsync(context);
    }
}

app.UseRouting();
app.UseSession(); // Add session middleware
app.UseTenantContext(); // Add tenant context middleware
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .WithName("HealthCheck");

// Redis connectivity test endpoint (development only)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test/redis", async (IConnectionMultiplexer? redis) =>
    {
        if (redis == null)
        {
            return Results.Ok(new { status = "not_configured", message = "Redis is not configured, using in-memory cache" });
        }

        try
        {
            var db = redis.GetDatabase();
            var key = "test:ping";
            var value = DateTime.UtcNow.ToString("O");
            
            // Set a test value
            await db.StringSetAsync(key, value, TimeSpan.FromSeconds(10));
            
            // Read it back
            var result = await db.StringGetAsync(key);
            
            // Check server info
            var endpoints = redis.GetEndPoints();
            var server = redis.GetServer(endpoints.First());
            var ping = await db.PingAsync();
            
            return Results.Ok(new 
            { 
                status = "connected",
                message = "Redis is operational",
                test_value = result.ToString(),
                ping_ms = ping.TotalMilliseconds,
                endpoint = endpoints.First().ToString()
            });
        }
        catch (Exception ex)
        {
            return Results.Ok(new 
            { 
                status = "error",
                message = "Redis connection failed",
                error = ex.Message
            });
        }
    })
    .WithName("RedisTest");
}

app.Run();
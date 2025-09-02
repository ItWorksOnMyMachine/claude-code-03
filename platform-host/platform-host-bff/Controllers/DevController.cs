using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Authorization;
using PlatformBff.Data;
using PlatformBff.Data.Entities;
using PlatformBff.Models.Dev;
using PlatformBff.Services;
using StackExchange.Redis;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PlatformBff.Controllers;

[ApiController]
[Route("dev")]
[DevelopmentOnly]
public class DevController : ControllerBase
{
    private readonly PlatformDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DevController> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;

    public DevController(
        PlatformDbContext dbContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IConnectionMultiplexer redis,
        ILogger<DevController> logger,
        IHostEnvironment environment,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _redis = redis;
        _logger = logger;
        _environment = environment;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("health/all")]
    public async Task<ActionResult<HealthCheckResponse>> GetHealthAll()
    {
        var response = new HealthCheckResponse
        {
            Timestamp = DateTime.UtcNow,
            Services = new Dictionary<string, ServiceHealthStatus>()
        };

        // Check Auth Service
        try
        {
            var authUrl = _configuration["Authentication:Authority"] ?? "https://login.platform.local:5214";
            var authClient = _httpClientFactory.CreateClient();
            authClient.Timeout = TimeSpan.FromSeconds(5);
            
            var authHealthResponse = await authClient.GetAsync($"{authUrl}/health");
            response.Services["auth-service"] = new ServiceHealthStatus
            {
                Status = authHealthResponse.IsSuccessStatusCode ? "healthy" : "unhealthy",
                Url = authUrl,
                Version = "1.0.0"
            };

            // Check auth database connection
            try
            {
                authClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var authDbHealth = await authClient.GetAsync($"{authUrl}/health/ready");
                if (authDbHealth.IsSuccessStatusCode)
                {
                    response.Services["auth-service"].Database = "connected";
                }
            }
            catch
            {
                response.Services["auth-service"].Database = "unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check auth service health");
            response.Services["auth-service"] = new ServiceHealthStatus
            {
                Status = "unhealthy",
                Url = _configuration["Authentication:Authority"] ?? "https://login.platform.local:5214"
            };
        }

        // Check Platform BFF (self)
        response.Services["platform-bff"] = new ServiceHealthStatus
        {
            Status = "healthy",
            Url = "https://host-bff.platform.local:5086",
            Version = "1.0.0"
        };

        // Check Platform Database
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            response.Services["platform-bff"].Database = canConnect ? "connected" : "disconnected";
            
            if (canConnect)
            {
                response.Services["postgres-platform"] = new ServiceHealthStatus
                {
                    Status = "healthy",
                    Port = 5432,
                    Databases = new List<string> { "platformdb" }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check platform database");
            response.Services["platform-bff"].Database = "disconnected";
            response.Services["postgres-platform"] = new ServiceHealthStatus
            {
                Status = "unhealthy",
                Port = 5432
            };
        }

        // Check Redis
        try
        {
            var redisDb = _redis.GetDatabase();
            await redisDb.PingAsync();
            response.Services["platform-bff"].Redis = "connected";
            
            response.Services["redis"] = new ServiceHealthStatus
            {
                Status = "healthy",
                Port = 6379,
                Keys = 0 // Could query actual key count if needed
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check Redis");
            response.Services["platform-bff"].Redis = "disconnected";
            response.Services["redis"] = new ServiceHealthStatus
            {
                Status = "unhealthy",
                Port = 6379
            };
        }

        // Check Frontend (if accessible)
        try
        {
            var frontendUrl = "https://host-fe.platform.local:3002";
            var frontendClient = _httpClientFactory.CreateClient();
            frontendClient.Timeout = TimeSpan.FromSeconds(5);
            
            var frontendResponse = await frontendClient.GetAsync(frontendUrl);
            response.Services["platform-frontend"] = new ServiceHealthStatus
            {
                Status = frontendResponse.IsSuccessStatusCode ? "healthy" : "unhealthy",
                Url = frontendUrl,
                Version = "1.0.0"
            };
        }
        catch
        {
            response.Services["platform-frontend"] = new ServiceHealthStatus
            {
                Status = "unknown",
                Url = "https://host-fe.platform.local:3002",
                Version = "1.0.0"
            };
        }

        // Check Auth Database (postgres-auth)
        response.Services["postgres-auth"] = new ServiceHealthStatus
        {
            Status = response.Services.ContainsKey("auth-service") && 
                     response.Services["auth-service"].Database == "connected" ? "healthy" : "unknown",
            Port = 5433,
            Databases = new List<string> { "authdb" }
        };

        // Determine overall health
        var unhealthyServices = response.Services.Values.Count(s => s.Status == "unhealthy");
        response.Overall = unhealthyServices == 0 ? "healthy" : 
                          unhealthyServices == response.Services.Count ? "unhealthy" : 
                          "degraded";

        return Ok(response);
    }

    [HttpPost("users/create")]
    public async Task<ActionResult<CreateTestUserResponse>> CreateUser([FromBody] CreateTestUserRequest request)
    {
        try
        {
            // Call auth service to create user
            var authUrl = _configuration["Authentication:Authority"] ?? "https://host-bff.platform.local:5214";
            var httpClient = _httpClientFactory.CreateClient();
            
            var authRequest = new
            {
                email = request.Email,
                password = request.Password,
                firstName = request.FirstName,
                lastName = request.LastName,
                roles = request.Roles
            };

            var response = await httpClient.PostAsJsonAsync($"{authUrl}/api/dev/users", authRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return BadRequest(new CreateTestUserResponse
                {
                    Success = false,
                    Message = $"Failed to create user in auth service: {error}"
                });
            }

            var authResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            var userId = authResponse.GetProperty("userId").GetString();

            return Ok(new CreateTestUserResponse
            {
                Success = true,
                UserId = userId,
                Email = request.Email,
                Message = "User created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test user");
            return StatusCode(500, new CreateTestUserResponse
            {
                Success = false,
                Message = $"Internal error: {ex.Message}"
            });
        }
    }

    [HttpPost("users/assign-tenant")]
    public async Task<ActionResult<AssignTenantResponse>> AssignTenant([FromBody] AssignTenantRequest request)
    {
        try
        {
            // Check if tenant exists
            var tenant = await _dbContext.Tenants.FindAsync(request.TenantId);
            if (tenant == null)
            {
                return NotFound(new AssignTenantResponse
                {
                    Success = false,
                    Message = $"Tenant {request.TenantId} not found"
                });
            }

            // Check if user already assigned
            var existingUser = await _dbContext.TenantUsers
                .FirstOrDefaultAsync(tu => tu.UserId == request.UserId && tu.TenantId == request.TenantId);

            if (existingUser != null)
            {
                return Conflict(new AssignTenantResponse
                {
                    Success = false,
                    Message = "User already assigned to this tenant"
                });
            }

            // Create tenant user
            var tenantUser = new TenantUser
            {
                TenantId = request.TenantId,
                UserId = request.UserId,
                IsActive = true,
                JoinedAt = DateTimeOffset.UtcNow
            };

            _dbContext.TenantUsers.Add(tenantUser);

            // Assign roles
            foreach (var roleName in request.Roles)
            {
                var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        TenantUserId = tenantUser.Id,
                        RoleId = role.Id
                    });
                }
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new AssignTenantResponse
            {
                Success = true,
                TenantUser = new TenantUserInfo
                {
                    TenantId = request.TenantId,
                    UserId = request.UserId,
                    Email = request.Email,
                    Roles = request.Roles
                },
                Message = "User assigned to tenant successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign user to tenant");
            return StatusCode(500, new AssignTenantResponse
            {
                Success = false,
                Message = $"Internal error: {ex.Message}"
            });
        }
    }

    [HttpPost("database/reset")]
    public async Task<ActionResult<ResetDatabaseResponse>> ResetDatabase([FromBody] ResetDatabaseRequest request)
    {
        try
        {
            var databases = new List<string>();
            var totalMigrations = 0;

            if (request.Target == "all" || request.Target == "platform")
            {
                // Reset platform database
                await _dbContext.Database.EnsureDeletedAsync();
                await _dbContext.Database.EnsureCreatedAsync();
                
                if (request.Seed)
                {
                    // Seed platform data
                    await DatabaseSeeder.InitializeDatabaseAsync(_serviceProvider, _environment);
                }
                
                databases.Add("platformdb");
                var migrations = await _dbContext.Database.GetPendingMigrationsAsync();
                totalMigrations += migrations.Count();
            }

            if (request.Target == "all" || request.Target == "auth")
            {
                // Call auth service to reset its database
                var authUrl = _configuration["Authentication:Authority"] ?? "https://host-bff.platform.local:5214";
                var httpClient = _httpClientFactory.CreateClient();
                
                var authResetRequest = new { seed = request.Seed };
                var response = await httpClient.PostAsJsonAsync($"{authUrl}/api/dev/database/reset", authResetRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    databases.Add("authdb");
                    var authResult = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (authResult.TryGetProperty("migrations", out var migrations))
                    {
                        totalMigrations += migrations.GetInt32();
                    }
                }
            }

            return Ok(new ResetDatabaseResponse
            {
                Success = true,
                Databases = databases,
                Migrations = totalMigrations,
                Seeded = request.Seed,
                Message = $"Databases reset and {(request.Seed ? "seeded" : "cleared")} successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database");
            return StatusCode(500, new ResetDatabaseResponse
            {
                Success = false,
                Message = $"Database reset failed: {ex.Message}"
            });
        }
    }

    [HttpGet("config/verify")]
    public async Task<ActionResult<ConfigVerificationResponse>> VerifyConfig()
    {
        var response = new ConfigVerificationResponse
        {
            Environment = _environment.EnvironmentName,
            Configurations = new ConfigurationDetails()
        };

        // Auth configuration
        response.Configurations.Auth = new AuthConfig
        {
            Authority = _configuration["Authentication:Authority"] ?? "",
            ClientId = _configuration["Authentication:ClientId"] ?? "",
            RedirectUri = _configuration["Authentication:RedirectUri"] ?? ""
        };

        // Database configuration
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            response.Configurations.Database.Platform = canConnect ? "connected" : "disconnected";
        }
        catch
        {
            response.Configurations.Database.Platform = "error";
        }

        // Check auth database via auth service
        try
        {
            var authUrl = _configuration["Authentication:Authority"] ?? "https://host-bff.platform.local:5214";
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            var authHealth = await httpClient.GetAsync($"{authUrl}/health/ready");
            response.Configurations.Database.Auth = authHealth.IsSuccessStatusCode ? "connected" : "disconnected";
        }
        catch
        {
            response.Configurations.Database.Auth = "unknown";
        }

        // Redis configuration
        response.Configurations.Redis.Connection = _configuration["Redis:Connection"] ?? "";
        try
        {
            var redisDb = _redis.GetDatabase();
            await redisDb.PingAsync();
            response.Configurations.Redis.Status = "connected";
        }
        catch
        {
            response.Configurations.Redis.Status = "disconnected";
        }

        // CORS configuration
        var corsOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (corsOrigins != null)
        {
            response.Configurations.Cors.Origins = corsOrigins.ToList();
        }

        // Determine if configuration is valid
        response.Valid = !string.IsNullOrEmpty(response.Configurations.Auth.Authority) &&
                        !string.IsNullOrEmpty(response.Configurations.Auth.ClientId) &&
                        response.Configurations.Database.Platform == "connected" &&
                        response.Configurations.Redis.Status == "connected" &&
                        response.Configurations.Cors.Origins.Any();

        return Ok(response);
    }
}
using AuthService.Data;
using AuthService.Data.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AuthService.Tests.Integration;

public class LoadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public LoadTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
        _output = output;
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Login_Requests()
    {
        // Arrange - Create test users
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var userCount = 50;
        var users = new List<(string email, string password)>();
        
        for (int i = 0; i < userCount; i++)
        {
            var email = $"loadtest{i}@identity.local";
            var password = $"TestPass{i}!";
            
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = $"Load{i}",
                LastName = "Test",
                IsActive = true
            };
            
            await userManager.CreateAsync(user, password);
            users.Add((email, password));
        }
        
        // Act - Simulate concurrent logins
        var tasks = new List<Task<HttpResponseMessage>>();
        var successCount = 0;
        var failureCount = 0;
        var stopwatch = Stopwatch.StartNew();
        
        using var semaphore = new SemaphoreSlim(10); // Limit concurrent requests
        
        foreach (var (email, password) in users)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    using var client = _factory.CreateClient();
                    
                    var tokenRequest = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", email),
                        new KeyValuePair<string, string>("password", password),
                        new KeyValuePair<string, string>("scope", "openid profile"),
                        new KeyValuePair<string, string>("client_id", "trusted-client"),
                        new KeyValuePair<string, string>("client_secret", "trusted-secret")
                    });
                    
                    var response = await client.PostAsync("/connect/token", tokenRequest);
                    
                    if (response.StatusCode == HttpStatusCode.OK)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref failureCount);
                    
                    return response;
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        _output.WriteLine($"Completed {userCount} login requests in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Success: {successCount}, Failures: {failureCount}");
        _output.WriteLine($"Average time per request: {stopwatch.ElapsedMilliseconds / userCount}ms");
        
        successCount.Should().BeGreaterThan((int)(userCount * 0.95)); // At least 95% success rate
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Complete within 30 seconds
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Token_Refresh_Requests()
    {
        // Arrange - Create test user and get initial refresh tokens
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "refresh.load@identity.local",
            Email = "refresh.load@identity.local",
            EmailConfirmed = true,
            FirstName = "Refresh",
            LastName = "Load",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Get multiple refresh tokens for the same user
        var refreshTokens = new ConcurrentBag<string>();
        var tokenTasks = new List<Task>();
        
        for (int i = 0; i < 20; i++)
        {
            tokenTasks.Add(Task.Run(async () =>
            {
                using var client = _factory.CreateClient();
                
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", "refresh.load@identity.local"),
                    new KeyValuePair<string, string>("password", "TestPass123!"),
                    new KeyValuePair<string, string>("scope", "openid profile offline_access"),
                    new KeyValuePair<string, string>("client_id", "trusted-client"),
                    new KeyValuePair<string, string>("client_secret", "trusted-secret")
                });
                
                var response = await client.PostAsync("/connect/token", tokenRequest);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
                    var refreshToken = tokenData.GetProperty("refresh_token").GetString();
                    refreshTokens.Add(refreshToken);
                }
            }));
        }
        
        await Task.WhenAll(tokenTasks);
        
        // Act - Simulate concurrent refresh requests
        var refreshTasks = new List<Task<bool>>();
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var refreshToken in refreshTokens)
        {
            refreshTasks.Add(Task.Run(async () =>
            {
                using var client = _factory.CreateClient();
                
                var refreshRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("client_id", "trusted-client"),
                    new KeyValuePair<string, string>("client_secret", "trusted-secret")
                });
                
                var response = await client.PostAsync("/connect/token", refreshRequest);
                return response.StatusCode == HttpStatusCode.OK;
            }));
        }
        
        var results = await Task.WhenAll(refreshTasks);
        stopwatch.Stop();
        
        // Assert
        var successCount = results.Count(r => r);
        _output.WriteLine($"Completed {refreshTokens.Count} refresh requests in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Success rate: {successCount}/{refreshTokens.Count}");
        
        successCount.Should().BeGreaterThan((int)(refreshTokens.Count * 0.9)); // At least 90% success
    }

    [Fact(/*Skip = "Rate limiting behavior is environment-specific and may not trigger consistently in test environment"*/)]
    public async Task Should_Enforce_Rate_Limiting_Under_Load()
    {
        // Arrange - Use a unique IP for this test to avoid interference with other tests
        var clientIp = "10.0.0.99";
        var requests = new List<Task<HttpResponseMessage>>();
        var rateLimitedCount = 0;
        
        // Use a shared client to ensure consistent cache
        using var sharedClient = _factory.CreateClient();
        
        // Act - Send many requests from same IP rapidly
        for (int i = 0; i < 100; i++)
        {
            var requestIndex = i; // Capture for closure
            requests.Add(Task.Run(async () =>
            {
                // Clone the request for thread safety
                var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token");
                request.Headers.Add("X-Forwarded-For", clientIp);
                
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", $"ratelimit{requestIndex}@test.local"),
                    new KeyValuePair<string, string>("password", "WrongPass"),
                    new KeyValuePair<string, string>("scope", "openid"),
                    new KeyValuePair<string, string>("client_id", "trusted-client"),
                    new KeyValuePair<string, string>("client_secret", "trusted-secret")
                });
                
                var response = await sharedClient.SendAsync(request);
                
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    Interlocked.Increment(ref rateLimitedCount);
                
                return response;
            }));
        }
        
        await Task.WhenAll(requests);
        
        // Assert - Some requests should be rate limited
        _output.WriteLine($"Rate limited {rateLimitedCount} out of {requests.Count} requests");
        rateLimitedCount.Should().BeGreaterThan(0, "Rate limiting should kick in under heavy load");
    }

    [Fact]
    public async Task Should_Handle_Mixed_Grant_Types_Concurrently()
    {
        // Arrange - Create test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "mixed.load@identity.local",
            Email = "mixed.load@identity.local",
            EmailConfirmed = true,
            FirstName = "Mixed",
            LastName = "Load",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Act - Mix different grant types
        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();
        
        // Password grant requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var client = _factory.CreateClient();
                var request = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", "mixed.load@identity.local"),
                    new KeyValuePair<string, string>("password", "TestPass123!"),
                    new KeyValuePair<string, string>("scope", "openid"),
                    new KeyValuePair<string, string>("client_id", "trusted-client"),
                    new KeyValuePair<string, string>("client_secret", "trusted-secret")
                });
                return await client.PostAsync("/connect/token", request);
            }));
        }
        
        // Client credentials grant requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var client = _factory.CreateClient();
                var request = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "api"),
                    new KeyValuePair<string, string>("client_id", "machine-client"),
                    new KeyValuePair<string, string>("client_secret", "machine-secret")
                });
                return await client.PostAsync("/connect/token", request);
            }));
        }
        
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        _output.WriteLine($"Mixed grant types: {successCount}/{responses.Length} successful in {stopwatch.ElapsedMilliseconds}ms");
        
        successCount.Should().BeGreaterThan((int)(responses.Length * 0.9));
    }

    [Fact]
    public async Task Should_Maintain_Performance_With_Large_User_Base()
    {
        // Arrange - Simulate large user base
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        // Create a batch of users to simulate existing large user base
        var existingUsers = 1000; // Simulate 1000 existing users
        for (int i = 0; i < existingUsers; i += 100) // Create in batches to speed up
        {
            var batchTasks = new List<Task>();
            for (int j = 0; j < Math.Min(100, existingUsers - i); j++)
            {
                var idx = i + j;
                batchTasks.Add(Task.Run(async () =>
                {
                    // Create a new scope for each concurrent task to avoid DbContext conflicts
                    using var taskScope = _factory.Services.CreateScope();
                    var taskUserManager = taskScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                    
                    var user = new AppUser
                    {
                        UserName = $"existing{idx}@identity.local",
                        Email = $"existing{idx}@identity.local",
                        EmailConfirmed = true,
                        FirstName = $"User{idx}",
                        LastName = "Test",
                        IsActive = true
                    };
                    await taskUserManager.CreateAsync(user, $"Pass{idx}!");
                }));
            }
            await Task.WhenAll(batchTasks);
        }
        
        // Act - Measure authentication performance with large user base
        var testRequests = 20;
        var timings = new List<long>();
        
        for (int i = 0; i < testRequests; i++)
        {
            var userIndex = i * 50; // Test different users across the range
            var stopwatch = Stopwatch.StartNew();
            
            using var client = _factory.CreateClient();
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", $"existing{userIndex}@identity.local"),
                new KeyValuePair<string, string>("password", $"Pass{userIndex}!"),
                new KeyValuePair<string, string>("scope", "openid"),
                new KeyValuePair<string, string>("client_id", "trusted-client"),
                new KeyValuePair<string, string>("client_secret", "trusted-secret")
            });
            
            var response = await client.PostAsync("/connect/token", request);
            stopwatch.Stop();
            
            if (response.StatusCode == HttpStatusCode.OK)
                timings.Add(stopwatch.ElapsedMilliseconds);
        }
        
        // Assert
        var avgTime = timings.Average();
        var maxTime = timings.Max();
        
        _output.WriteLine($"With {existingUsers} users:");
        _output.WriteLine($"Average auth time: {avgTime}ms");
        _output.WriteLine($"Max auth time: {maxTime}ms");
        
        avgTime.Should().BeLessThan(500); // Average should be under 500ms
        maxTime.Should().BeLessThan(2000); // Max should be under 2 seconds
    }

    [Fact]
    public async Task Should_Handle_Session_Management_Under_Load()
    {
        // Arrange - Create users and establish sessions
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var sessionCount = 30;
        var sessions = new ConcurrentBag<(string userId, string accessToken)>();
        
        // Create users and get tokens
        var sessionTasks = new List<Task>();
        for (int i = 0; i < sessionCount; i++)
        {
            var index = i;
            sessionTasks.Add(Task.Run(async () =>
            {
                var user = new AppUser
                {
                    UserName = $"session{index}@identity.local",
                    Email = $"session{index}@identity.local",
                    EmailConfirmed = true,
                    FirstName = $"Session{index}",
                    LastName = "Test",
                    IsActive = true
                };
                
                await userManager.CreateAsync(user, $"Pass{index}!");
                
                using var client = _factory.CreateClient();
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", $"session{index}@identity.local"),
                    new KeyValuePair<string, string>("password", $"Pass{index}!"),
                    new KeyValuePair<string, string>("scope", "openid profile"),
                    new KeyValuePair<string, string>("client_id", "trusted-client"),
                    new KeyValuePair<string, string>("client_secret", "trusted-secret")
                });
                
                var response = await client.PostAsync("/connect/token", tokenRequest);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
                    var accessToken = tokenData.GetProperty("access_token").GetString();
                    sessions.Add((user.Id, accessToken));
                }
            }));
        }
        
        await Task.WhenAll(sessionTasks);
        
        // Act - Concurrent userinfo requests using sessions
        var userInfoTasks = new List<Task<bool>>();
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var (userId, accessToken) in sessions)
        {
            userInfoTasks.Add(Task.Run(async () =>
            {
                using var client = _factory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await client.SendAsync(request);
                return response.StatusCode == HttpStatusCode.OK;
            }));
        }
        
        var results = await Task.WhenAll(userInfoTasks);
        stopwatch.Stop();
        
        // Assert
        var successCount = results.Count(r => r);
        _output.WriteLine($"Session validation: {successCount}/{sessions.Count} successful in {stopwatch.ElapsedMilliseconds}ms");
        
        successCount.Should().Be(sessions.Count, "All valid sessions should work");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Complete within 10 seconds
    }

    [Fact]
    public async Task Should_Handle_Burst_Traffic_Gracefully()
    {
        // Arrange
        var burstSize = 50;
        var responses = new ConcurrentBag<HttpStatusCode>();
        
        // Act - Send burst of requests all at once
        var tasks = Enumerable.Range(0, burstSize).Select(i => Task.Run(async () =>
        {
            using var client = _factory.CreateClient();
            
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "api"),
                new KeyValuePair<string, string>("client_id", "machine-client"),
                new KeyValuePair<string, string>("client_secret", "machine-secret")
            });
            
            var response = await client.PostAsync("/connect/token", request);
            responses.Add(response.StatusCode);
            return response;
        }));
        
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var statusCodes = responses.ToList();
        var successCount = statusCodes.Count(s => s == HttpStatusCode.OK);
        var rateLimitCount = statusCodes.Count(s => s == HttpStatusCode.TooManyRequests);
        
        _output.WriteLine($"Burst test ({burstSize} requests):");
        _output.WriteLine($"Success: {successCount}, Rate limited: {rateLimitCount}");
        _output.WriteLine($"Completed in: {stopwatch.ElapsedMilliseconds}ms");
        
        (successCount + rateLimitCount).Should().Be(burstSize, "All requests should be handled (either success or rate limited)");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000); // Should handle burst within 15 seconds
    }
}
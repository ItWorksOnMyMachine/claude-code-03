using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests.Configuration;

public class IdentityConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IdentityConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public void Should_Register_UserStore()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var userStore = scope.ServiceProvider.GetService<IUserStore<AppUser>>();

        // Assert
        userStore.Should().NotBeNull();
    }

    [Fact]
    public void Should_Register_RoleStore()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var roleStore = scope.ServiceProvider.GetService<IRoleStore<AppRole>>();

        // Assert
        roleStore.Should().NotBeNull();
    }

    [Fact]
    public void Should_Register_UserManager()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetService<UserManager<AppUser>>();

        // Assert
        userManager.Should().NotBeNull();
    }

    [Fact]
    public void Should_Register_Custom_PasswordValidator()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IPasswordValidator<AppUser>>();

        // Assert
        validators.Should().NotBeNull();
        validators.Should().Contain(v => v.GetType() == typeof(CustomPasswordValidator));
    }

    [Fact]
    public void Should_Configure_Identity_Options()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var identityOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();

        // Assert
        identityOptions.Should().NotBeNull();
        var options = identityOptions.Value;
        
        // Password options
        // Note: These should match the appsettings.json configuration
        options.Password.RequiredLength.Should().Be(6);
        options.Password.RequireDigit.Should().BeFalse();
        options.Password.RequireLowercase.Should().BeFalse();
        options.Password.RequireUppercase.Should().BeFalse();
        options.Password.RequireNonAlphanumeric.Should().BeFalse();
        
        // Lockout options
        options.Lockout.DefaultLockoutTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
        options.Lockout.MaxFailedAccessAttempts.Should().Be(5);
        options.Lockout.AllowedForNewUsers.Should().BeTrue();
        
        // User options
        options.User.RequireUniqueEmail.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Create_User_With_Identity_Stores()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        // Act
        var user = new AppUser
        {
            UserName = "testuser",
            Email = "test@identity.local",
            FirstName = "Test",
            LastName = "User"
        };
        var result = await userManager.CreateAsync(user, "TestPassword123!");
        
        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        
        // Verify user was created successfully
        var createdUser = await userManager.FindByNameAsync("testuser");
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be("test@identity.local");
        createdUser.FirstName.Should().Be("Test");
        createdUser.LastName.Should().Be("User");
        createdUser.IsActive.Should().BeTrue();
    }
}
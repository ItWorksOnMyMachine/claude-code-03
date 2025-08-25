using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text;

namespace PlatformBff.Tests.Authentication;

/// <summary>
/// Test implementation of IDataProtector that doesn't actually encrypt
/// This is used for unit testing to bypass encryption/decryption
/// </summary>
public class TestDataProtector : IDataProtector
{
    public IDataProtector CreateProtector(string purpose)
    {
        return this;
    }

    public byte[] Protect(byte[] plaintext)
    {
        // Just return the plaintext for testing
        return plaintext;
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        // Just return the data as-is for testing
        return protectedData;
    }
}

/// <summary>
/// Test implementation of IDataProtectionProvider
/// </summary>
public class TestDataProtectionProvider : IDataProtectionProvider
{
    public IDataProtector CreateProtector(string purpose)
    {
        return new TestDataProtector();
    }
}

/// <summary>
/// Extension methods to match the real DataProtection API
/// </summary>
public static class TestDataProtectorExtensions
{
    public static string Protect(this IDataProtector protector, string plaintext)
    {
        if (protector is TestDataProtector)
        {
            // For testing, just return the plaintext
            return plaintext;
        }
        
        // Fallback to actual implementation
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var protectedBytes = protector.Protect(plaintextBytes);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(this IDataProtector protector, string protectedData)
    {
        if (protector is TestDataProtector)
        {
            // For testing, just return the data as-is
            return protectedData;
        }
        
        // Fallback to actual implementation
        var protectedBytes = Convert.FromBase64String(protectedData);
        var plaintextBytes = protector.Unprotect(protectedBytes);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
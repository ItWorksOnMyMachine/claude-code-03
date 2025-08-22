using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace PlatformBff.Tests.Helpers;

public class TestResponseCookieCollection : IResponseCookies
{
    private readonly Dictionary<string, string> _cookies = new Dictionary<string, string>();

    public void Append(string key, string value)
    {
        _cookies[key] = value;
    }

    public void Append(string key, string value, CookieOptions options)
    {
        _cookies[key] = value;
    }

    public void Delete(string key)
    {
        _cookies.Remove(key);
    }

    public void Delete(string key, CookieOptions options)
    {
        _cookies.Remove(key);
    }

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public string? GetValue(string key) => _cookies.TryGetValue(key, out var value) ? value : null;
}
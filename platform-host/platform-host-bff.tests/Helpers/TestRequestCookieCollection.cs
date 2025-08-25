using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Collections.Generic;

namespace PlatformBff.Tests.Helpers;

public class TestRequestCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies;

    public TestRequestCookieCollection(Dictionary<string, string> cookies = null)
    {
        _cookies = cookies ?? new Dictionary<string, string>();
    }

    public string this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

    public int Count => _cookies.Count;

    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    public bool TryGetValue(string key, out string value) => _cookies.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public void Add(string key, string value) => _cookies[key] = value;
}
using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace AuthService.Tests.TestInfrastructure;

/// <summary>
/// AsyncLocal sink so background server threads can record log lines which are later flushed to the current test's ITestOutputHelper.
/// </summary>
internal static class XunitHostLogSink
{
    private static readonly AsyncLocal<ITestOutputHelper?> _current = new();
    private static readonly ConcurrentQueue<string> _buffer = new();

    public static void SetTestOutput(ITestOutputHelper helper) => _current.Value = helper;
    public static void Clear() => _current.Value = null;

    public static void Write(string line)
    {
        var ts = DateTime.UtcNow.ToString("O");
        var formatted = $"{ts} {line}";
        _buffer.Enqueue(formatted);
        var current = _current.Value;
        if (current != null)
        {
            try { current.WriteLine(formatted); } catch { }
        }
    }

    public static void FlushTo(ITestOutputHelper helper)
    {
        while (_buffer.TryDequeue(out var line))
        {
            try { helper.WriteLine(line); } catch { }
        }
    }
}

internal sealed class XunitHostLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new XunitHostLogger(categoryName);
    public void Dispose() { }

    private sealed class XunitHostLogger : ILogger
    {
        private readonly string _category;
        public XunitHostLogger(string category) => _category = category;
        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;
        bool ILogger.IsEnabled(LogLevel logLevel) => true;
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                var msg = formatter(state, exception);
                if (exception != null)
                {
                    XunitHostLogSink.Write($"[{logLevel}] {_category}: {msg}\n{exception}");
                }
                else
                {
                    XunitHostLogSink.Write($"[{logLevel}] {_category}: {msg}");
                }
            }
            catch { }
        }
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}

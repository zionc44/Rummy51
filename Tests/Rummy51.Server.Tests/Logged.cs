using Microsoft.Extensions.Logging;

namespace Rummy51.Server.Tests;

/// <summary>
/// What the host wrote down, kept instead of printed. Two reasons, and the
/// second is the one that matters: a run full of stack traces from a test
/// that threw on purpose cannot be told apart from a run that broke, and
/// an error nobody asked for goes unnoticed in the middle of it.
///
/// Held under a lock because the hosted clock logs from its own thread.
/// </summary>
public sealed class Logged : ILoggerProvider
{
    private readonly Lock gate = new();
    private readonly List<Entry> entries = [];

    public sealed record Entry(LogLevel Level, string Category, string Message, Exception? Thrown);

    /// <summary>Everything the host thought was an error, worst first.</summary>
    public IReadOnlyList<Entry> Errors
    {
        get
        {
            lock (gate)
            {
                return [.. entries.Where(entry => entry.Level >= LogLevel.Error)];
            }
        }
    }

    public void Forget()
    {
        lock (gate)
        {
            entries.Clear();
        }
    }

    public ILogger CreateLogger(string category) => new Writer(this, category);

    public void Dispose()
    {
    }

    private void Add(Entry entry)
    {
        lock (gate)
        {
            entries.Add(entry);
        }
    }

    private sealed class Writer(Logged kept, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel level) => true;

        public void Log<TState>(
            LogLevel level,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            kept.Add(new Entry(level, category, formatter(state, exception), exception));
    }
}

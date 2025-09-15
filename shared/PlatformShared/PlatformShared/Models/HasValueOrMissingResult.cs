namespace PlatformShared.Models;

public class HasValueOrMissingResult<T>
{
    public T Value { get; }
    public bool HasValue { get; }
    public bool IsMissing => !HasValue;

    protected HasValueOrMissingResult(T value, bool hasValue)
    {
        Value = value;
        HasValue = hasValue;
    }

    public static HasValueOrMissingResult<T> SetValue(T value)
    {
        return new HasValueOrMissingResult<T>(value, true);
    }

    public static HasValueOrMissingResult<T> SetMissing()
    {
        return new HasValueOrMissingResult<T>(default(T), false);
    }

    // Map function to transform the success value
    public HasValueOrMissingResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (IsMissing)
            return HasValueOrMissingResult<TNew>.SetMissing();

        return HasValueOrMissingResult<TNew>.SetValue(mapper(Value));
    }

    // Bind function for chaining operations that return Results
    public HasValueOrMissingResult<TNew> Bind<TNew>(Func<T, HasValueOrMissingResult<TNew>> binder)
    {
        if (IsMissing)
            return HasValueOrMissingResult<TNew>.SetMissing();

        return binder(Value);
    }

    // Match pattern for handling both success and failure cases
    public TResult Match<TResult>(
        Func<T, TResult> onValue,
        Func<TResult> onMissing)
    {
        return HasValue ? onValue(Value) : onMissing();
    }
}
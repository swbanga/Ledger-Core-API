namespace LedgerCore.Application.Contracts;

public interface ICachingService
{
    /// <summary>
    /// Atomically increment the value stored at the key by the given delta.
    /// Returns the new value after increment.
    /// </summary>
    Task<long> AtomicIncrementAsync(string key, long delta);

    /// <summary>
    /// Sets a key‑level expiry on the key.
    /// </summary>
    Task SetKeyExpiryAsync(string key, TimeSpan expiry);

    /// <summary>
    /// Acquires a lock by setting the key with <paramref name="value"/> only if it does not already exist.
    /// Returns true if the lock was acquired; false otherwise.
    /// </summary>
    Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry);

    /// <summary>
    /// Gets the string value stored at <paramref name="key"/>.
    /// Returns null when the key does not exist.
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Sets the string <paramref name="value"/> at <paramref name="key"/> with the given expiry.
    /// </summary>
    Task SetAsync(string key, string value, TimeSpan expiry);
}

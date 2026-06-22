using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Xunit;
using LedgerCore.Application.Behaviors;
using LedgerCore.Application.Contracts;

namespace LedgerCore.IntegrationTests;

public class IdempotencyBehaviorTests
{
    public class DummyRequest : IRequest<string>, IIdempotentCommand<string>
    {
        public Guid IdempotencyKey { get; init; }
        public string Payload { get; init; } = "";
    }

    public class FakeCacheService : ICachingService
    {
        private readonly object _guard = new object();
        private readonly Dictionary<string, (string value, DateTime? expiry)> _store = new();

        public Task<long> AtomicIncrementAsync(string key, long delta)
        {
            return Task.FromResult(0L);
        }

        public Task SetKeyExpiryAsync(string key, TimeSpan expiry)
        {
            return Task.CompletedTask;
        }

        public Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry)
        {
            lock (_guard)
            {
                if (_store.ContainsKey(key))
                {
                    return Task.FromResult(false);
                }
                _store[key] = (value, DateTime.UtcNow + expiry);
                return Task.FromResult(true);
            }
        }

        public Task<string?> GetAsync(string key)
        {
            lock (_guard)
            {
                if (_store.TryGetValue(key, out var entry))
                {
                    if (entry.expiry.HasValue && entry.expiry.Value < DateTime.UtcNow)
                    {
                        _store.Remove(key);
                        return Task.FromResult<string?>(null);
                    }
                    return Task.FromResult<string?>(entry.value);
                }
                return Task.FromResult<string?>(null);
            }
        }

        public Task SetAsync(string key, string value, TimeSpan expiry)
        {
            lock (_guard)
            {
                _store[key] = (value, DateTime.UtcNow + expiry);
                return Task.CompletedTask;
            }
        }
    }

    [Fact]
    public async Task Duplicate_Request_With_Cached_Response_Does_Not_Run_Handler_Again()
    {
        var key = Guid.NewGuid();
        var cache = new FakeCacheService();
        var behavior = new IdempotencyBehavior<DummyRequest, string>(cache);

        int runCount = 0;
        RequestHandlerDelegate<string> handler = () =>
        {
            Interlocked.Increment(ref runCount);
            return Task.FromResult("result-abc");
        };

        var request = new DummyRequest { IdempotencyKey = key };

        // First call
        var response1 = await behavior.Handle(request, handler, CancellationToken.None);
        Assert.Equal("result-abc", response1);
        Assert.Equal(1, runCount);

        // Second call – should return cached result without invoking handler
        var response2 = await behavior.Handle(request, handler, CancellationToken.None);
        Assert.Equal("result-abc", response2);
        Assert.Equal(1, runCount); // Handler still called once.
    }

    [Fact]
    public async Task Concurrent_Requests_With_Same_Key_Only_One_Succeeds_No_Duplicate()
    {
        var key = Guid.NewGuid();
        var cache = new FakeCacheService();
        var behavior = new IdempotencyBehavior<DummyRequest, string>(cache);

        int handlerInvocations = 0;
        int successCount = 0;
        int duplicateExceptionCount = 0;
        int otherExceptionCount = 0;

        var tasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                RequestHandlerDelegate<string> handler = () =>
                {
                    Interlocked.Increment(ref handlerInvocations);
                    return Task.FromResult("processed");
                };
                var req = new DummyRequest { IdempotencyKey = key };

                try
                {
                    var res = await behavior.Handle(req, handler, CancellationToken.None);
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Duplicate request detected"))
                {
                    Interlocked.Increment(ref duplicateExceptionCount);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref otherExceptionCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Exactly one success, the rest should be duplicate detection exceptions.
        Assert.Equal(1, successCount);
        Assert.True(duplicateExceptionCount >= 19, $"Expected at least 19 duplicates, got {duplicateExceptionCount}");
        Assert.Equal(0, otherExceptionCount);
        // Handler should have run exactly once (by the winner)
        Assert.Equal(1, handlerInvocations);
    }

    [Fact]
    public async Task Idempotent_Key_Reuse_Across_Non_Overlapping_Calls_Uses_Cache()
    {
        var key = Guid.NewGuid();
        var cache = new FakeCacheService();
        var behavior = new IdempotencyBehavior<DummyRequest, string>(cache);

        int handlerInvocations = 0;
        RequestHandlerDelegate<string> handler = () =>
        {
            Interlocked.Increment(ref handlerInvocations);
            return Task.FromResult("first-result");
        };

        var request = new DummyRequest { IdempotencyKey = key };

        // First call
        var firstResult = await behavior.Handle(request, handler, CancellationToken.None);
        Assert.Equal("first-result", firstResult);
        Assert.Equal(1, handlerInvocations);

        // Second call after first completes
        var secondResult = await behavior.Handle(request, handler, CancellationToken.None);
        Assert.Equal("first-result", secondResult);
        Assert.Equal(1, handlerInvocations);
    }
}

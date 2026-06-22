using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, (string value, DateTime? expiry)> _store = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _locks = new();

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
            return Task.FromResult(_locks.TryAdd(key, 1));
        }

        public Task<string?> GetAsync(string key)
        {
            if (_store.TryGetValue(key, out var entry))
            {
                if (entry.expiry.HasValue && entry.expiry.Value < DateTime.UtcNow)
                {
                    _store.TryRemove(key, out _);
                    return Task.FromResult<string?>(null);
                }
                return Task.FromResult<string?>(entry.value);
            }
            return Task.FromResult<string?>(null);
        }

        public Task SetAsync(string key, string value, TimeSpan expiry)
        {
            _store[key] = (value, DateTime.UtcNow + expiry);
            return Task.CompletedTask;
        }
    }

    private int _handlerRunCount;
    private int _handlerInvocations;
    private int _successCount;
    private int _duplicateExceptionCount;
    private int _otherExceptionCount;

    [Fact]
    public async Task Duplicate_Request_With_Cached_Response_Does_Not_Run_Handler_Again()
    {
        var key = Guid.NewGuid();
        var cache = new FakeCacheService();
        var behavior = new IdempotencyBehavior<DummyRequest, string>(cache);

        _handlerRunCount = 0;

        var request = new DummyRequest { IdempotencyKey = key };

        // First call
        RequestHandlerDelegate<string> next = (CancellationToken _) => { Interlocked.Increment(ref _handlerRunCount); return Task.FromResult("result-abc"); };
        var response1 = await behavior.Handle(request, next, CancellationToken.None);
        Assert.Equal("result-abc", response1);
        Assert.Equal(1, _handlerRunCount);

        // Second call – should return cached result without invoking handler
        var response2 = await behavior.Handle(request, next, CancellationToken.None);
        Assert.Equal("result-abc", response2);
        Assert.Equal(1, _handlerRunCount); // Handler still called once.
    }

    [Fact]
    public async Task Concurrent_Requests_With_Same_Key_Only_One_Succeeds_No_Duplicate()
    {
        var key = Guid.NewGuid();
        var cache = new FakeCacheService();
        var behavior = new IdempotencyBehavior<DummyRequest, string>(cache);

        _handlerInvocations = 0;
        _successCount = 0;
        _duplicateExceptionCount = 0;
        _otherExceptionCount = 0;

        var tasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var req = new DummyRequest { IdempotencyKey = key };

                try
                {
                    RequestHandlerDelegate<string> next = (CancellationToken _) => { Interlocked.Increment(ref _handlerInvocations); return Task.FromResult("processed"); };
                    var res = await behavior.Handle(req, next, CancellationToken.None);
                    Interlocked.Increment(ref _successCount);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Duplicate request detected"))
                {
                    Interlocked.Increment(ref _duplicateExceptionCount);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref _otherExceptionCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Exactly one success, the rest should be duplicate detection exceptions.
        Assert.Equal(1, _successCount);
        Assert.True(_duplicateExceptionCount >= 19, $"Expected at least 19 duplicates, got {_duplicateExceptionCount}");
        Assert.Equal(0, _otherExceptionCount);
        // Handler should have run exactly once (by the winner)
        Assert.Equal(1, _handlerInvocations);
    }

    [Fact]
    public async Task Idempotent_Key_Reuse_Across_Non_Overlapping_Calls_Uses_Cache()
    {
        var key = Guid.NewGuid();
        var cache = new FakeCacheService();
        var behavior = new IdempotencyBehavior<DummyRequest, string>(cache);

        _handlerInvocations = 0;

        var request = new DummyRequest { IdempotencyKey = key };

        // First call
        RequestHandlerDelegate<string> next = (CancellationToken _) => { Interlocked.Increment(ref _handlerInvocations); return Task.FromResult("first-result"); };
        var firstResult = await behavior.Handle(request, next, CancellationToken.None);
        Assert.Equal("first-result", firstResult);
        Assert.Equal(1, _handlerInvocations);

        // Second call after first completes
        var secondResult = await behavior.Handle(request, next, CancellationToken.None);
        Assert.Equal("first-result", secondResult);
        Assert.Equal(1, _handlerInvocations);
    }
}

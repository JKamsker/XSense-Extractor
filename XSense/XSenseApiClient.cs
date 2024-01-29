using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSense;

public class InMemoryStorage
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public async ValueTask<T> GetOrAddAsync<T>(string key, Func<ValueTask<T>> factory)
    {
        var entry = _cache.GetOrAdd(key, _ => new CacheEntry(null));
        if (entry.Value is not null)
        {
            return (T)entry.Value;
        }

        await entry.Semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (entry.Value is not null)
            {
                return (T)entry.Value;
            }

            var result = await factory().ConfigureAwait(false);
            entry.Value = result;
            return result;
        }
        finally
        {
            entry.Semaphore.Release();
        }
    }

    //private record CacheEntry(object Value, SemaphoreSlim Semaphore);

    private class CacheEntry
    {
        public object? Value { get; set; }
        public SemaphoreSlim Semaphore { get; }

        public CacheEntry(object? value, SemaphoreSlim? semaphore = null)
        {
            Value = value;
            Semaphore = semaphore ?? new SemaphoreSlim(1, 1);
        }
    }
}

internal class XSenseApiClient
{
    private readonly XSenseHttpClient _httpClient;
    private readonly InMemoryStorage _storage;

    public XSenseApiClient(XSenseHttpClient httpClient, InMemoryStorage storage)
    {
        _httpClient = httpClient;
        _storage = storage;
    }

    public async Task<bool> LoginAsync(string userName, string password)
    {
        // 1: Check if we have a valid refresh token
        // 2: If not, authenticate with SRP
        // 3: If so, refresh the token
    }

    private async ValueTask<ClientInfo> GetClientInfoAsync()
    {
        var info = await _storage.GetOrAddAsync("clientInfo", async () =>
        {
            var clientInfo = await _httpClient.QueryClientInfo().ConfigureAwait(false);
            return clientInfo;
        }).ConfigureAwait(false);

        return info;
    }
}
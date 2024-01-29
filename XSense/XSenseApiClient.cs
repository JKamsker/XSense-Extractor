using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSense;

public interface IExpirable
{
    bool IsExpired { get; }
}

public class InMemoryStorage
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    //public async ValueTask<T> GetOrAddAsync<T>(string key, Func<ValueTask<T>> factory)
    //{
    //    var entry = _cache.GetOrAdd(key, _ => new CacheEntry(null));
    //    if (entry.Value is not null)
    //    {
    //        return (T)entry.Value;
    //    }

    //    await entry.Semaphore.WaitAsync().ConfigureAwait(false);
    //    try
    //    {
    //        if (entry.Value is not null)
    //        {
    //            return (T)entry.Value;
    //        }

    //        var result = await factory().ConfigureAwait(false);
    //        entry.Value = result;
    //        return result;
    //    }
    //    finally
    //    {
    //        entry.Semaphore.Release();
    //    }
    //}

    //public async ValueTask<T> GetOrAddAsync<T>(string key, Func<ValueTask<T>> factory)
    //    where T : class
    //{
    //    var entry = _cache.GetOrAdd(key, _ => new CacheEntry(null));
    //    if (entry.Value is not null)
    //    {
    //        if (entry.Value is IExpirable expirable && expirable.IsExpired)
    //        {
    //            // If the value is expired, remove it from cache
    //            _cache.TryRemove(key, out _);
    //        }
    //        else
    //        {
    //            return (T)entry.Value;
    //        }
    //    }

    //    await entry.Semaphore.WaitAsync().ConfigureAwait(false);
    //    try
    //    {
    //        if (entry.Value is not null)
    //        {
    //            if (entry.Value is IExpirable expirable && expirable.IsExpired)
    //            {
    //                // If the value is expired, remove it from cache
    //                _cache.TryRemove(key, out _);
    //            }
    //            else
    //            {
    //                return (T)entry.Value;
    //            }
    //        }

    //        var result = await factory().ConfigureAwait(false);
    //        entry.Value = result;
    //        return result;
    //    }
    //    finally
    //    {
    //        entry.Semaphore.Release();
    //    }
    //}

    public async ValueTask<T> GetOrAddAsync<T>(string key, Func<T, ValueTask<T>> factory)
        where T : class
    {
        var entry = _cache.GetOrAdd(key, _ => new CacheEntry(null));
        T oldValue = entry.Value as T;

        if (oldValue is not null)
        {
            if (oldValue is IExpirable expirable && expirable.IsExpired)
            {
                // If the value is expired, remove it from cache
                _cache.TryRemove(key, out _);
            }
            else
            {
                return oldValue;
            }
        }

        await entry.Semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            oldValue = entry.Value as T;
            if (oldValue is not null)
            {
                if (oldValue is IExpirable expirable && expirable.IsExpired)
                {
                    // If the value is expired, remove it from cache
                    _cache.TryRemove(key, out _);
                }
                else
                {
                    return oldValue;
                }
            }

            var result = await factory(oldValue).ConfigureAwait(false);
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
    private Credentials _credentials;

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
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);

        _credentials = await _storage.GetOrAddAsync<Credentials>($"login_{userName}", async old =>
        {
            if (old is not null && !old.ShouldRefresh)
            {
                return old;
            }

            if (old is null)
            {
                return await _httpClient.AuthenticateWithSrpAsync(clientInfo, userName, password).ConfigureAwait(false);
            }

            return await _httpClient.RefreshTokenAsync(clientInfo, old.Username, old.RefreshToken).ConfigureAwait(false);
        });

        return _credentials is not null;
    }

    private async ValueTask<ClientInfo> GetClientInfoAsync()
    {
        return await _storage.GetOrAddAsync<ClientInfo>("clientInfo", async _ =>
        {
            var clientInfo = await _httpClient.QueryClientInfo().ConfigureAwait(false);
            return clientInfo;
        }).ConfigureAwait(false);
    }
}
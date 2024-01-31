using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

using XSense.Models.Init;
using XSense.Models.Sensoric;

namespace XSense;

public interface IExpirable
{
    bool IsExpired { get; }
}

public class InMemoryStorage
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public Func<InMemoryStorage, ValueTask> OnCacheUpdated { get; set; } = s => ValueTask.CompletedTask;

    public InMemoryStorage()
    {
    }

    private InMemoryStorage(ConcurrentDictionary<string, CacheEntry>? cache)
    {
        _cache = cache ?? new ConcurrentDictionary<string, CacheEntry>();
    }

    public async ValueTask<T> GetOrAddAsync<T>(string key, Func<T, ValueTask<T>> factory)
        where T : class
    {
        bool dirtyBit = false;
        try
        {
            var entry = _cache.GetOrAdd(key, _ =>
            {
                dirtyBit = true;
                return new CacheEntry(null);
            });

            T oldValue = default;
            if (TryTypeCast(entry, out oldValue))
            {
                if (oldValue is IExpirable expirable && expirable.IsExpired)
                {
                    // If the value is expired, remove it from cache
                    _cache.TryRemove(key, out _);
                    dirtyBit = true;
                }
                else
                {
                    return oldValue;
                }
            }

            await entry.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (TryTypeCast(entry, out oldValue))
                {
                    if (oldValue is IExpirable expirable && expirable.IsExpired)
                    {
                        // If the value is expired, remove it from cache
                        _cache.TryRemove(key, out _);
                        dirtyBit = true;
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
        finally
        {
            if (dirtyBit)
            {
                await OnCacheUpdated(this).ConfigureAwait(false);
            }
        }
    }

    private bool TryTypeCast<T>(CacheEntry value, out T result)
    {
        if (TryTypeCast(value.Value, out result))
        {
            if (value.Value is not T)
            {
                value.Value = result;
            }

            return true;
        }

        result = default!;
        return false;
    }

    private bool TryTypeCast<T>(object? value, out T result)
    {
        if (value is T t)
        {
            result = t;
            return true;
        }

        // JsonElement
        if (value is JsonElement jsonElement)
        {
            result = jsonElement.Deserialize<T>();
            return true;
        }

        result = default!;
        return false;
    }

    public async Task SaveToDiskAsync(string fileName)
    {
        var model = _cache.Select(x => new CacheEntryModel(x.Key, x.Value.Value)).ToList();

        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fileName, json).ConfigureAwait(false);
    }

    public static InMemoryStorage LoadFromDisk(string fileName)
    {
        if (!File.Exists(fileName))
        {
            return new InMemoryStorage();
        }

        var json = File.ReadAllText(fileName);

        var model = JsonSerializer.Deserialize<List<CacheEntryModel>>(json);
        var cache = new ConcurrentDictionary<string, CacheEntry>(model.Select(x => new KeyValuePair<string, CacheEntry>(x.Key, x.ToCacheEntry())));
        return new InMemoryStorage(cache);
    }

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

    private class CacheEntryModel
    {
        public string Key { get; set; }
        public object? Value { get; set; }

        public CacheEntryModel(string key, object? value)
        {
            Key = key;
            Value = value;
        }

        public CacheEntry ToCacheEntry()
        {
            return new(Value);
        }
    }
}

internal class XSenseApiClient
{
    private readonly XSenseHttpClient _httpClient;
    private readonly InMemoryStorage _storage;
    private Credentials _credentials;

    public XSenseApiClient(XSenseHttpClient httpClient, InMemoryStorage? storage = null)
    {
        _httpClient = httpClient;
        _storage = storage ?? new InMemoryStorage();
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

            return await _httpClient.RefreshTokenAsync(clientInfo, old).ConfigureAwait(false);
        });

        return _credentials is not null;
    }

    private async ValueTask<Credentials> GetCredentialsAsync()
    {
        var creds = _credentials ?? throw new InvalidOperationException("Not logged in");

        return await _storage.GetOrAddAsync<Credentials>($"login_{creds.UserId}", async old =>
        {
            _ = old ?? throw new InvalidOperationException("Not logged in");
            if (!old.ShouldRefresh)
            {
                return old;
            }

            var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
            return await _httpClient.RefreshTokenAsync(clientInfo, old).ConfigureAwait(false);
        });
    }

    private async ValueTask<ClientInfo> GetClientInfoAsync()
    {
        return await _storage.GetOrAddAsync<ClientInfo>("clientInfo", async _ =>
        {
            var clientInfo = await _httpClient.QueryClientInfo().ConfigureAwait(false);
            return clientInfo;
        }).ConfigureAwait(false);
    }

    public async Task<GetHousesResponseData[]> GetHousesAsync()
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetHouses(clientInfo, creds).ConfigureAwait(false);
    }

    // GetHouseDetails
    public async Task<GetHousesDetailResponseData> GetHouseDetailsAsync(string houseId)
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetHouseDetails(clientInfo, creds, houseId).ConfigureAwait(false);
    }

    public async Task<GetSensoricDataResponseData> GetSensoricDataAsync(GetSensoricDataRequest request)
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetSensoricData(clientInfo, creds, request).ConfigureAwait(false);
    }
}
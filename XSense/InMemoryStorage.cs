using System.Collections.Concurrent;
using System.Text.Json;

namespace XSense;

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
            //if (TryTypeCast(entry, out oldValue))
            //{
            //    if (oldValue is IExpirable expirable && expirable.IsExpired)
            //    {
            //        // If the value is expired, remove it from cache
            //        _cache.TryRemove(key, out _);
            //        dirtyBit = true;
            //    }
            //    else
            //    {
            //        return oldValue;
            //    }
            //}

            // Happy path: No converting, just cast
            if (entry.Value is T t && t is not null && !(t is IExpirable expirable && expirable.IsExpired))
            {
                return t;
            }

            await entry.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (TryTypeCast(entry, out oldValue))
                {
                    var isExpired = oldValue is IExpirable expirable1 && expirable1.IsExpired;

                    if (!isExpired)
                    {
                        return oldValue;
                    }

                    // If the value is expired, remove it from cache
                    //_cache.TryRemove(key, out _);
                    dirtyBit = true;
                }

                var result = await factory(oldValue).ConfigureAwait(false);
                if (result is not null)
                {
                    entry.Value = result;
                    return result;
                }
                _cache.TryRemove(key, out _);
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

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
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
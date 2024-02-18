using XSense.Models.Init;
using XSense.Models.Internal;

namespace XSense.Database;

public class XDao
{
    private readonly IStorage _storage;

    public XDao(IStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Get or add credentials to the cache
    /// </summary>
    /// <param name="userName">email</param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public async ValueTask<Credentials> GetOrAddCredentialsAsync(string userName, Func<Credentials, ValueTask<Credentials>> factory)
    {
        return await _storage.GetOrAddAsync($"login_{userName}", factory);
    }

    public void DeleteCredentials(string userName)
    {
        _storage.Remove($"login_{userName}");
    }

    // GetOrAddClientInfoAsync
    public async ValueTask<ClientInfo> GetOrAddClientInfoAsync(Func<ClientInfo, ValueTask<ClientInfo>> factory)
    {
        return await _storage.GetOrAddAsync("clientInfo", factory);
    }

    // GoC AwsIotCredentials
    public async ValueTask<AwsIotCredentials> GetOrAddIotCredsAsync(string userId, Func<AwsIotCredentials, ValueTask<AwsIotCredentials>> factory)
    {
        return await _storage.GetOrAddAsync($"iotCreds_{userId}", factory);
    }

    internal async ValueTask<Settings> GetSettingsAsync()
    {
        return await _storage.GetOrAddAsync<Settings>("settings", async _ => new Settings());
    }

    public async Task SaveChangesAsync()
    {
        await _storage.SaveChangesAsync();
    }
}
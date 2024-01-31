using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;

using System.Text.Json.Serialization;

namespace XSense;

//public record Credentials
//(
//    CognitoUser User,
//    AuthenticationResultType AuthenticationResult
//);

public class Credentials : IExpirable
{
    //public CognitoUser User { get; set; }
    //public AuthenticationResultType AuthenticationResult { get; set; }

    public Credentials(CognitoUser user, AuthenticationResultType authenticationResult)
    {
        //User = user;
        //AuthenticationResult = authenticationResult;

        Username = user.Username;
        UserId = user.UserID;

        IdToken = authenticationResult.IdToken;
        AccessToken = authenticationResult.AccessToken;
        RefreshToken = authenticationResult.RefreshToken;
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(authenticationResult.ExpiresIn);
    }

    public Credentials()
    {
    }

    /// <summary>
    /// UserId (EMAIL)
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// UserName (GUID)
    /// </summary>
    public string Username { get; set; }

    //public string Password { get; set; }

    public string IdToken { get; set; }
    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    [JsonIgnore]
    public bool ShouldRefresh => ExpiresAt < DateTimeOffset.UtcNow.AddMinutes(5);

    [JsonIgnore]
    public bool IsExpired => ShouldRefresh;
}
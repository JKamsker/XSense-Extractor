using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;

namespace XSense;

//public record Credentials
//(
//    CognitoUser User,
//    AuthenticationResultType AuthenticationResult
//);

public class Credentials
{
    public CognitoUser User { get; set; }
    public AuthenticationResultType AuthenticationResult { get; set; }

    public Credentials(CognitoUser user, AuthenticationResultType authenticationResult)
    {
        User = user;
        AuthenticationResult = authenticationResult;

        UserName = user.Username;
        UserId = user.UserID;

        IdToken = authenticationResult.IdToken;
        AccessToken = authenticationResult.AccessToken;
        RefreshToken = authenticationResult.RefreshToken;
    }

    /// <summary>
    /// UserId (EMAIL)
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// UserName (GUID)
    /// </summary>
    public string UserName { get; set; }

    //public string Password { get; set; }

    public string IdToken { get; set; }
    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }
}
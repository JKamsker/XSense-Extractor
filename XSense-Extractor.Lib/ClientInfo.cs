namespace XSenseExtractor;

public record ClientInfo
(
    string? Region,
    string PoolId,
    string ClientId,
    ClientSecret ClientSecret
)
{
    // $"{PoolID}#{ClientID}"
    public string UserPoolConfig => $"{PoolId}#{ClientId}";

    public static readonly ClientInfo Default = new(
        "us-east-1",
        "us-east-1_nX8mlT3dQ",
        "1jok6cf2o57ndalv2a6ol0ogqh",
        "MTE3MjFmcjBya3BkNGFtdnB1czI1cmQ2N284b2E1NHNnZXBlNmljbmVmOTA1Z2RtaWQ3ZmFhb3My"
    );
}
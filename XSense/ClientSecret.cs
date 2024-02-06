using System.Text;

namespace XSense;

public class ClientSecret
{
    public string Base64Value { get; }

    public string FullValue { get; }

    public string ShortValue { get; }

    public ClientSecret(string base64Value)
    {
        Base64Value = base64Value;
        FullValue = Encoding.UTF8.GetString(Convert.FromBase64String(base64Value));
        if (FullValue.Length >= 5)
        {
            ShortValue = FullValue.Substring(0, FullValue.Length - 1).Substring((1172).ToString().Length);
        }
    }

    public static implicit operator ClientSecret(string base64Value) => new(base64Value);
}
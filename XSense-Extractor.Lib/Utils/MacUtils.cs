using System.Security.Cryptography;
using System.Text;

namespace XSenseExtractor.Utils;

public static class MacUtils
{
    public static string GetRequestMac(Dictionary<string, object> map, ClientSecret clientSecret)
        => GetRequestMac(map, clientSecret.ShortValue);

    public static string GetRequestMac(Dictionary<string, object> map, string clientSecret)
    {
        StringBuilder sb2 = new StringBuilder();
        if (map != null && map.Any())
        {
            foreach (var entry in map)
            {
                if (entry.Value is List<string> list)
                {
                    if (list.Any())
                    {
                        sb2.Append(string.Join("", list));
                        Console.WriteLine($"appendListContent {sb2.ToString()}");
                    }
                }
                else if (entry.Value is Dictionary<string, object> map2)
                {
                    throw new NotImplementedException();
                    //if (map2 != null)
                    //{
                    //    sb2.Append(JsonConvert.SerializeObject(map2, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings
                    //    {
                    //        ContractResolver = new DefaultContractResolver
                    //        {
                    //            NamingStrategy = new CamelCaseNamingStrategy()
                    //        }
                    //    }));
                    //}
                }
                else
                {
                    sb2.Append(entry.Value);
                }
            }
        }

        //sb2.Append("1fr0rkpd4amvpus25rd67o8oa54sgepe6icnef905gdmid7faaos");
        sb2.Append(clientSecret);
        //Console.WriteLine($"mac加密数据: {sb2.ToString()}");
        return ToMD5(sb2.ToString());
    }

    public static string ToMD5(string str)
    {
        using (MD5 md5Hash = MD5.Create())
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));

            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
}
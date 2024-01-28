using System.Text;
using System.Text.Json;

namespace XSense;

public class JsonContent : StringContent
{
    public JsonContent(object obj) :
        base(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json")
    {
    }

    private JsonContent(string content, Encoding encoding, string mediaType) :
        base(content, encoding, mediaType)
    {
    }

    // Create<T>
    public static JsonContent Create<T>(T obj)
    {
        var serialized = JsonSerializer.Serialize(obj);
        return new JsonContent(serialized, Encoding.UTF8, "application/json");
    }
}



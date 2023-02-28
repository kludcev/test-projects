using System.Text.Json;

namespace Providers;

// without additional config for simplicity
public static class JsonExtensions
{
    public static string ToJson(this object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));
        
        return JsonSerializer.Serialize(obj);
    }

    public static T FromJson<T>(this string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException(nameof(json));
        
        return JsonSerializer.Deserialize<T>(json);
    }
}
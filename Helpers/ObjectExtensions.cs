using System.Text.Json.Serialization;
using System.Text.Json;

namespace IME.SpotDataApi.Helpers
{
    public static class ObjectExtensions
    {
        //public static T Clone<T>(this T source)
        //{
        //    var options = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };
        //    var json = JsonSerializer.Serialize(source, options);
        //    return JsonSerializer.Deserialize<T>(json, options);
        //}
        //public static bool Compare<T>(this T obj1, T obj2)
        //{
        //    var options = new JsonSerializerOptions
        //    {
        //        IgnoreNullValues = true,
        //        IgnoreReadOnlyProperties = true
        //    };
        //    var json1 = JsonSerializer.Serialize(obj1, options);
        //    var json2 = JsonSerializer.Serialize(obj2, options);
        //    return json1 == json2;
        //}
    }
}

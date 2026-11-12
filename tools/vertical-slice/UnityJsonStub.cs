// Test adapter ONLY; never imported into Assets or used as a backend.
namespace UnityEngine
{
    public static class JsonUtility
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
        public static string ToJson(object value,bool pretty=false)=>System.Text.Json.JsonSerializer.Serialize(value,Options);
        public static T FromJson<T>(string value)=>System.Text.Json.JsonSerializer.Deserialize<T>(value,Options);
    }
}

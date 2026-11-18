// Test adapter ONLY; never imported into Assets or used as a backend.
namespace UnityEngine
{
    public static class JsonUtility
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
        public static string ToJson(object value,bool pretty=false)=>System.Text.Json.JsonSerializer.Serialize(value,Options);
        public static T FromJson<T>(string value)=>System.Text.Json.JsonSerializer.Deserialize<T>(value,Options);
    }
    // Activity 9: collects developer log lines so tests can check that raw errors stay out of player-facing text.
    public static class Debug
    {
        public static readonly System.Collections.Generic.List<string> Lines=new System.Collections.Generic.List<string>();
        public static void Log(object message)=>Lines.Add(message?.ToString());
        public static void LogWarning(object message)=>Lines.Add(message?.ToString());
        public static void LogError(object message)=>Lines.Add(message?.ToString());
    }
}

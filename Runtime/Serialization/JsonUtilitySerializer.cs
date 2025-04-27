using UnityEngine;

namespace AsyncNetClient.Serialization
{
    public class JsonUtilitySerializer : ISerializer
    {
        public string ContentType => "application/json";

        public string SerializeObject<T>(T value)
        {
            return JsonUtility.ToJson(value);
        }

        public T DeserializeObject<T>(string value)
        {
            return JsonUtility.FromJson<T>(value);
        }
    }

}
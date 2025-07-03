#if WITH_NEWTONSOFT_JSON
using Newtonsoft.Json;

namespace AsyncNetClient.Serialization
{
    public class NewtonsoftSerializer : ISerializer
    {
        public string ContentType => "application/json";
        private readonly JsonSerializerSettings _serializerSettings;

        public NewtonsoftSerializer(JsonSerializerSettings settings = null)
        {
            _serializerSettings = settings ?? new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
        }


        public string SerializeObject<T>(T value)
        {
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }

        public T DeserializeObject<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, _serializerSettings);
        }
    }
}
#endif
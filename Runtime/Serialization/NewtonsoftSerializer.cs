#if WITH_NEWTONSOFT_JSON
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AsyncNetClient.Serialization
{
    public class NewtonsoftSerializer : ISerializer
    {
        public string ContentType => "application/json";
        private readonly JsonSerializer _serializer;

        public NewtonsoftSerializer(JsonSerializerSettings settings = null)
            : this(JsonSerializer.Create(settings)) {}

        private NewtonsoftSerializer(JsonSerializer serializer)
            => _serializer = serializer;

        public string SerializeObject<T>(T value)
        {
            var builder = new StringBuilder(256);
            using var writer = new StringWriter(builder, CultureInfo.InvariantCulture);
            using var jsonWriter = new JsonTextWriter(writer);
            jsonWriter.Formatting = _serializer.Formatting;
            _serializer.Serialize(jsonWriter, value, typeof(T));

            return writer.ToString();
        }

        public T DeserializeObject<T>(string value)
        {
            using var reader = new JsonTextReader(new StringReader(value));
            return (T)_serializer.Deserialize(reader, typeof(T));
        }
    }
}
#endif
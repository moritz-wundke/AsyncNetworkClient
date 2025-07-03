#if WITH_NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace AsyncNetClient.Serialization
{
    public static class SerializationFactory
    {
#if WITH_NEWTONSOFT_JSON
        public static ISerializer Create(JsonSerializerSettings settings = null)
#else
        public static ISerializer Create()
#endif
        {
#if WITH_NEWTONSOFT_JSON
            return new NewtonsoftSerializer(settings);
#else
            return new JsonUtilitySerializer();
#endif
        }
    }
}
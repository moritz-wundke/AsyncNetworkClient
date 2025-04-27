namespace AsyncNetClient.Serialization
{
    public static class SerializationFactory
    {
        public static ISerializer Create()
        {
#if WITH_NEWTONSOFT_JSON
            return new NewtonsoftSerializer();
#else
            return new JsonUtilitySerializer();
#endif
        }
    }
}
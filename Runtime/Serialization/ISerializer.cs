namespace AsyncNetClient.Serialization
{
    public interface ISerializer
    {
        string ContentType { get; }
        string SerializeObject<T>(T value);

        T DeserializeObject<T>(string value);
    }
}
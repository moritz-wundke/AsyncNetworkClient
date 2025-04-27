using System.Threading;
using System.Threading.Tasks;
using AsyncNetClient.Serialization;

namespace AsyncNetClient
{
    public interface IAsyncNetworkClient
    {
        ISerializer Serializer { get; }
        Task<ResponseContext> SendAsync(HttpMethod method, string path, object request = null, CancellationToken cancellationToken = default);
    }
}